using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wism.Companion.Shared.Models;

namespace WismCompanion.UI
{
    /// <summary>Which influence channel the heat layer paints.</summary>
    public enum InfluenceChannel
    {
        Tension,
        Friendly,
        Enemy
    }

    /// <summary>Bright diverging colour schemes for the heat field.</summary>
    public enum InfluencePalette
    {
        Aurora,
        Inferno,
        Viridis
    }

    /// <summary>
    /// The "Aurora" spatial overlay (V1): a translucent, animated heat layer painted over the map
    /// with <see cref="Painter2D"/>. The field arrives once per AI turn, so liveliness is animated
    /// rather than streamed — the displayed field lerps toward each new snapshot (so the front
    /// <em>slides</em> instead of popping), a flowing shimmer modulates alpha, and cells whose
    /// tension flips sign emit an expanding ripple ("the front just moved here").
    /// </summary>
    /// <remarks>
    /// V1 paints per-tile translucent rectangles — cheap and architecture-friendly, but blocky.
    /// V2 (Plasma) swaps the fill for a GPU shader sampling the same field; the data, morph, toggle,
    /// and ripple logic here are reused unchanged.
    /// </remarks>
    public sealed class InfluenceOverlay
    {
        private const float LerpPerSecond = 3.0f;   // field morph speed (≈0.6s to converge)
        private const float ShimmerSpeed = 2.2f;    // flow shimmer rate
        private const float RippleSpeed = 2.4f;     // ring expansion, tiles/second (slow = it lingers)
        private const float RippleLife = 2.6f;      // seconds — rings hang out and crackle
        private const float SignFlipEpsilon = 0.06f; // |Δtension| needed to count as a real flip
        private const float PulseInterval = 1.3f;   // heartbeat: re-emit rings from hot cells
        private const float PulseThreshold = 0.5f;  // |tension| a cell needs to pulse
        private const int MaxRipples = 96;

        public bool Enabled { get; set; }
        public bool ShowFront { get; set; } = true;
        public bool ShowSparkle { get; set; } = true;
        public InfluenceChannel Channel { get; set; } = InfluenceChannel.Tension;
        public float Opacity { get; set; } = 0.75f;

        /// <summary>When true, render the GPU "Plasma" path; falls back to Painter2D if it fails.</summary>
        public bool UseGpu { get; set; } = true;

        /// <summary>When true, draw flow chevrons drifting along the tension gradient.</summary>
        public bool ShowGradient { get; set; }

        public InfluencePalette Palette { get; set; } = InfluencePalette.Aurora;

        /// <summary>When true, stream glowing ember particles along the gradient (stretch).</summary>
        public bool ShowEmbers { get; set; }

        public bool HasField => width > 0 && height > 0;
        public bool Animating => Enabled && HasField;

        private readonly InfluencePlasmaRenderer plasma = new InfluencePlasmaRenderer();
        private readonly EmberField ember = new EmberField();
        private RenderTexture heatRt;
        private int width, height;
        private float[] displayT, displayF, displayE;   // smoothed (what we draw)
        private float[] targetT, targetF, targetE;       // latest snapshot
        private float lastNow = -1f;
        private float clock;
        private float pulseTimer;
        private bool morphing;

        private readonly List<Ripple> ripples = new();
        private struct Ripple { public int X, Y; public float Age; public float Sign; }

        /// <summary>Ingest the latest field: set morph targets and spawn ripples where the front moved.</summary>
        public void SetField(InfluenceFieldDto field)
        {
            if (field == null || field.Width <= 0 || field.Height <= 0 || field.Tension == null)
            {
                width = height = 0;
                return;
            }

            var resized = field.Width != width || field.Height != height;
            width = field.Width;
            height = field.Height;
            var n = width * height;

            var newT = field.Tension;
            var newF = field.Friendly != null && field.Friendly.Length == n ? field.Friendly : new float[n];
            var newE = field.Enemy != null && field.Enemy.Length == n ? field.Enemy : new float[n];

            if (resized || displayT == null || displayT.Length != n)
            {
                displayT = (float[])newT.Clone();
                displayF = (float[])newF.Clone();
                displayE = (float[])newE.Clone();
                ripples.Clear();
            }
            else
            {
                SpawnRipplesForSignFlips(newT);
            }

            targetT = newT;
            targetF = newF;
            targetE = newE;
            ember.SetField(displayT, width, height);
        }

        private void SpawnRipplesForSignFlips(float[] newT)
        {
            if (targetT == null || targetT.Length != newT.Length)
            {
                return;
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = (y * width) + x;
                    var before = targetT[i];
                    var after = newT[i];
                    if (Mathf.Sign(before) != Mathf.Sign(after) && Mathf.Abs(after - before) > SignFlipEpsilon)
                    {
                        if (ripples.Count >= MaxRipples) return;
                        ripples.Add(new Ripple { X = x, Y = y, Age = 0f, Sign = Mathf.Sign(after) });
                    }
                }
            }
        }

        // Periodically re-emit rings from the strongest cells so hotspots keep crackling.
        private void EmitHeartbeatRipples()
        {
            if (displayT == null) return;
            var added = 0;
            for (var y = 1; y < height - 1 && added < 8; y += 2)
            {
                for (var x = 1; x < width - 1 && added < 8; x += 2)
                {
                    var v = displayT[(y * width) + x];
                    if (Mathf.Abs(v) < PulseThreshold || !IsTensionLocalMax(x, y, Mathf.Abs(v))) continue;
                    if (ripples.Count >= MaxRipples) return;
                    ripples.Add(new Ripple { X = x, Y = y, Age = 0f, Sign = Mathf.Sign(v) });
                    added++;
                }
            }
        }

        private bool IsTensionLocalMax(int x, int y, float mag)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var nx = x + dx; var ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (Mathf.Abs(displayT[(ny * width) + nx]) > mag) return false;
                }
            }
            return true;
        }

        /// <summary>Advance morph + ripples. <paramref name="now"/> is realtime seconds.</summary>
        public void Tick(float now)
        {
            if (lastNow < 0f) lastNow = now;
            var dt = Mathf.Clamp(now - lastNow, 0f, 0.1f);
            lastNow = now;
            clock += dt;

            if (!HasField || displayT == null || targetT == null) return;

            var k = Mathf.Clamp01(dt * LerpPerSecond);
            var n = Math.Min(displayT.Length, targetT.Length);
            var moving = false;
            for (var i = 0; i < n; i++)
            {
                if (!moving && Mathf.Abs(targetT[i] - displayT[i]) > 0.002f) moving = true;
                displayT[i] += (targetT[i] - displayT[i]) * k;
                displayF[i] += (targetF[i] - displayF[i]) * k;
                displayE[i] += (targetE[i] - displayE[i]) * k;
            }
            morphing = moving;

            for (var r = ripples.Count - 1; r >= 0; r--)
            {
                var rip = ripples[r];
                rip.Age += dt;
                if (rip.Age >= RippleLife) ripples.RemoveAt(r);
                else ripples[r] = rip;
            }

            // Heartbeat: keep emitting rings from the hottest cells so the field stays alive
            // between turns ("hangs out and crackles") instead of pulsing once on a sign flip.
            pulseTimer += dt;
            if (Enabled && pulseTimer >= PulseInterval)
            {
                pulseTimer = 0f;
                EmitHeartbeatRipples();
            }

            ember.Enabled = Enabled && ShowEmbers;
            ember.Tick(dt);

            // GPU path: re-blit the morphed field through the plasma material this frame. The field
            // texture only re-uploads while morphing; the flow animates purely in the shader.
            var pal = ResolvePalette(Palette);
            heatRt = Enabled && UseGpu && HasField && displayT != null
                ? plasma.Render(displayT, displayF, displayE, width, height, Opacity, (int)Channel,
                    morphing, pal.FriendlyNear, pal.FriendlyFar, pal.EnemyNear, pal.EnemyFar)
                : null;
        }

        // ---- Drawing -----------------------------------------------------------------

        /// <summary>Paint the heat field. Call after terrain, before units, so stacks stay readable.</summary>
        public void DrawHeat(MeshGenerationContext mgc, Painter2D p, Rect tileRegion,
            int originX, int originY, int tilesW, int tilesH, bool invertY,
            Func<int, int, Vector2> mapToViewport, float tile)
        {
            if (!Enabled || !HasField || displayT == null) return;

            // Plasma: one smooth textured quad over the visible window, UV-tracked to pan/zoom.
            if (heatRt != null)
            {
                var uMin = originX / (float)width;
                var uMax = (originX + tilesW) / (float)width;
                float vTop, vBottom;
                if (invertY) { vTop = (originY + tilesH) / (float)height; vBottom = originY / (float)height; }
                else { vTop = originY / (float)height; vBottom = (originY + tilesH) / (float)height; }

                var mesh = mgc.Allocate(4, 6, heatRt);
                var col = (Color32)Color.white;
                var z = Vertex.nearZ;
                mesh.SetNextVertex(new Vertex { position = new Vector3(tileRegion.xMin, tileRegion.yMin, z), tint = col, uv = new Vector2(uMin, vTop) });
                mesh.SetNextVertex(new Vertex { position = new Vector3(tileRegion.xMax, tileRegion.yMin, z), tint = col, uv = new Vector2(uMax, vTop) });
                mesh.SetNextVertex(new Vertex { position = new Vector3(tileRegion.xMax, tileRegion.yMax, z), tint = col, uv = new Vector2(uMax, vBottom) });
                mesh.SetNextVertex(new Vertex { position = new Vector3(tileRegion.xMin, tileRegion.yMax, z), tint = col, uv = new Vector2(uMin, vBottom) });
                mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
                mesh.SetNextIndex(0); mesh.SetNextIndex(2); mesh.SetNextIndex(3);
                return;
            }

            // Aurora fallback: per-tile translucent fills.
            for (var y = originY; y < originY + tilesH; y++)
            {
                for (var x = originX; x < originX + tilesW; x++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    var i = (y * width) + x;

                    var color = HeatColor(displayT[i], displayF[i], displayE[i], x, y);
                    if (color.a <= 0.004f) continue;

                    var pos = mapToViewport(x, y);
                    FillRect(p, new Rect(pos.x, pos.y, tile, tile), color);
                }
            }
        }

        /// <summary>Release GPU resources; call when the owning element detaches.</summary>
        public void Dispose() => plasma.Dispose();

        /// <summary>Paint the front-line seam, ripples, and sparkle. Call after units, on top.</summary>
        public void DrawEffects(Painter2D p, int originX, int originY, int tilesW, int tilesH,
            bool invertY, Func<int, int, Vector2> mapToViewport, Func<float, float, Vector2> mapToViewportF, float tile)
        {
            if (!Enabled || !HasField || displayT == null) return;

            var pal = ResolvePalette(Palette);

            if (ShowGradient)
            {
                DrawGradientChevrons(p, originX, originY, tilesW, tilesH, invertY, mapToViewport, tile);
            }

            if (ShowFront)
            {
                DrawFrontSeam(p, originX, originY, tilesW, tilesH, mapToViewport, tile);
            }

            // Ripples: expanding rings emanating from where the front moved.
            foreach (var rip in ripples)
            {
                if (rip.X < originX || rip.Y < originY || rip.X >= originX + tilesW || rip.Y >= originY + tilesH) continue;
                var t = rip.Age / RippleLife;
                var pos = mapToViewport(rip.X, rip.Y);
                var center = new Vector2(pos.x + tile * 0.5f, pos.y + tile * 0.5f);
                var radius = tile * (0.3f + RippleSpeed * rip.Age);
                var ringColor = rip.Sign >= 0f ? pal.FriendlyNear : pal.EnemyNear;
                var crackle = 0.6f + 0.4f * Mathf.Sin((clock * 22f) + (rip.X * 1.3f) + rip.Y);
                ringColor.a = (1f - t) * 0.85f * Opacity * crackle;
                StrokeCircle(p, center, radius, ringColor, Mathf.Lerp(3f, 0.5f, t));
            }

            if (ShowSparkle)
            {
                DrawSparkle(p, originX, originY, tilesW, tilesH, mapToViewport, tile);
            }

            if (ShowEmbers)
            {
                ember.Draw(p, originX, originY, tilesW, tilesH, mapToViewportF, tile, pal.FriendlyNear, pal.EnemyNear, Opacity);
            }
        }

        private void DrawFrontSeam(Painter2D p, int originX, int originY, int tilesW, int tilesH,
            Func<int, int, Vector2> mapToViewport, float tile)
        {
            // A cell sits on the front when a 4-neighbour disagrees in tension sign.
            for (var y = originY; y < originY + tilesH; y++)
            {
                for (var x = originX; x < originX + tilesW; x++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    var here = displayT[(y * width) + x];
                    if (!IsFront(x, y, here)) continue;

                    var pos = mapToViewport(x, y);
                    // Higher-frequency flicker so the front line crackles rather than gently pulses.
                    var pulse = 0.5f + 0.5f * Mathf.Sin(clock * 9f + (x * 1.7f + y * 2.3f));
                    var seam = new Color(1f, 1f, 0.78f, Mathf.Clamp01(pulse) * 0.9f * Opacity);
                    var c = new Vector2(pos.x + tile * 0.5f, pos.y + tile * 0.5f);
                    StrokeCircle(p, c, tile * 0.18f, seam, Mathf.Max(1.5f, tile * 0.08f));
                }
            }
        }

        private bool IsFront(int x, int y, float here)
        {
            var hp = here >= 0f;
            return Disagrees(x - 1, y, hp) || Disagrees(x + 1, y, hp) ||
                   Disagrees(x, y - 1, hp) || Disagrees(x, y + 1, hp);
        }

        private bool Disagrees(int x, int y, bool herePositive)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            var t = displayT[(y * width) + x];
            return (t >= 0f) != herePositive && Mathf.Max(Mathf.Abs(t), 0f) > FrontMagnitude(t, herePositive);
        }

        private static float FrontMagnitude(float t, bool herePositive) => 0.02f;

        private void DrawSparkle(Painter2D p, int originX, int originY, int tilesW, int tilesH,
            Func<int, int, Vector2> mapToViewport, float tile)
        {
            // Twinkle on local maxima of the active channel — muster points / strong stacks.
            for (var y = originY; y < originY + tilesH; y++)
            {
                for (var x = originX; x < originX + tilesW; x++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    var mag = ChannelMagnitude(x, y);
                    if (mag < 0.55f || !IsLocalMax(x, y, mag)) continue;

                    var twinkle = Mathf.Pow(Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(clock * 6f + (x * 12.9f + y * 78.2f))), 3f);
                    if (twinkle < 0.15f) continue;

                    var pos = mapToViewport(x, y);
                    var c = new Vector2(pos.x + tile * 0.5f, pos.y + tile * 0.5f);
                    var spark = new Color(1f, 1f, 1f, twinkle * 0.9f * Opacity);
                    var r = tile * (0.06f + 0.10f * twinkle);
                    FillCircle(p, c, r, spark);
                }
            }
        }

        private void DrawGradientChevrons(Painter2D p, int originX, int originY, int tilesW, int tilesH,
            bool invertY, Func<int, int, Vector2> mapToViewport, float tile)
        {
            var step = tile < 22f ? 3 : 2; // sparser when zoomed out
            for (var y = originY; y < originY + tilesH; y += step)
            {
                for (var x = originX; x < originX + tilesW; x += step)
                {
                    if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1) continue;
                    var row = y * width;
                    var gx = displayT[row + x + 1] - displayT[row + x - 1];
                    var gy = displayT[((y + 1) * width) + x] - displayT[((y - 1) * width) + x];
                    var mag = Mathf.Sqrt(gx * gx + gy * gy);
                    if (mag < 0.05f) continue;

                    // Flow toward the enemy = descending tension = -gradient. Screen Y may invert.
                    var dir = new Vector2(-gx, invertY ? gy : -gy) / mag;
                    var perp = new Vector2(-dir.y, dir.x);

                    var pos = mapToViewport(x, y);
                    var basePt = new Vector2(pos.x + tile * 0.5f, pos.y + tile * 0.5f);
                    var phase = Mathf.Repeat((clock * 0.6f) + (x * 0.13f + y * 0.21f), 1f);
                    var center = basePt + dir * ((phase - 0.5f) * tile * 0.8f);
                    var a = Mathf.Sin(phase * Mathf.PI) * 0.5f * Opacity * Mathf.Clamp01(mag * 4f);
                    if (a < 0.02f) continue;

                    var tip = center + dir * (tile * 0.18f);
                    var bl = center - dir * (tile * 0.06f) + perp * (tile * 0.12f);
                    var br = center - dir * (tile * 0.06f) - perp * (tile * 0.12f);
                    var col = new Color(0.85f, 0.95f, 1f, a);
                    var lw = Mathf.Max(1f, tile * 0.05f);
                    StrokeLine(p, tip, bl, col, lw);
                    StrokeLine(p, tip, br, col, lw);
                }
            }
        }

        private float ChannelMagnitude(int x, int y)
        {
            var i = (y * width) + x;
            switch (Channel)
            {
                case InfluenceChannel.Friendly: return Mathf.Clamp01(displayF[i]);
                case InfluenceChannel.Enemy: return Mathf.Clamp01(displayE[i]);
                default: return Mathf.Clamp01(Mathf.Abs(displayT[i]));
            }
        }

        private bool IsLocalMax(int x, int y, float mag)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var nx = x + dx; var ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    if (ChannelMagnitude(nx, ny) > mag) return false;
                }
            }
            return true;
        }

        // ---- Palette -----------------------------------------------------------------

        private readonly struct Palette4
        {
            public Palette4(Color friendlyNear, Color friendlyFar, Color enemyNear, Color enemyFar)
            {
                FriendlyNear = friendlyNear;
                FriendlyFar = friendlyFar;
                EnemyNear = enemyNear;
                EnemyFar = enemyFar;
            }

            public Color FriendlyNear { get; }
            public Color FriendlyFar { get; }
            public Color EnemyNear { get; }
            public Color EnemyFar { get; }
        }

        private static Palette4 ResolvePalette(InfluencePalette palette)
        {
            switch (palette)
            {
                case InfluencePalette.Inferno:
                    return new Palette4(new Color(1f, 0.95f, 0.5f), new Color(1f, 0.5f, 0f),
                        new Color(0.95f, 0.2f, 0.7f), new Color(0.4f, 0f, 0.5f));
                case InfluencePalette.Viridis:
                    return new Palette4(new Color(0.13f, 0.57f, 0.55f), new Color(0.99f, 0.91f, 0.14f),
                        new Color(0.27f, 0f, 0.33f), new Color(0.19f, 0.41f, 0.56f));
                default: // Aurora — electric cyan/blue friendly, orange/red enemy.
                    return new Palette4(new Color(0.15f, 0.85f, 1f), new Color(0.25f, 0.45f, 1f),
                        new Color(1f, 0.45f, 0.1f), new Color(1f, 0.12f, 0.12f));
            }
        }

        private Color HeatColor(float tension, float friendly, float enemy, int x, int y)
        {
            var pal = ResolvePalette(Palette);
            float mag;
            Color baseColor;
            switch (Channel)
            {
                case InfluenceChannel.Friendly:
                    mag = Mathf.Clamp01(friendly);
                    baseColor = Color.Lerp(pal.FriendlyNear, pal.FriendlyFar, mag);
                    break;
                case InfluenceChannel.Enemy:
                    mag = Mathf.Clamp01(enemy);
                    baseColor = Color.Lerp(pal.EnemyNear, pal.EnemyFar, mag);
                    break;
                default: // Tension: sign picks the ramp, magnitude its intensity.
                    mag = Mathf.Clamp01(Mathf.Abs(tension));
                    baseColor = tension >= 0f
                        ? Color.Lerp(pal.FriendlyNear, pal.FriendlyFar, mag)
                        : Color.Lerp(pal.EnemyNear, pal.EnemyFar, mag);
                    break;
            }

            // Flowing shimmer so the field "breathes" between snapshots.
            var shimmer = 0.82f + 0.18f * Mathf.Sin(clock * ShimmerSpeed + (x * 0.7f + y * 1.3f));
            var alpha = Smooth(mag) * Opacity * shimmer;
            baseColor.a = Mathf.Clamp01(alpha);
            return baseColor;
        }

        private static float Smooth(float m)
        {
            // Gamma lift so weak influence still reads — density is visible across a wide area
            // instead of vanishing a few tiles out from each source.
            return Mathf.Pow(Mathf.Clamp01(m), 0.55f);
        }

        // ---- Painter2D primitives ----------------------------------------------------

        private static void FillRect(Painter2D p, Rect r, Color color)
        {
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMax));
            p.LineTo(new Vector2(r.xMin, r.yMax));
            p.ClosePath();
            p.Fill();
        }

        private static void StrokeLine(Painter2D p, Vector2 a, Vector2 b, Color color, float lineWidth)
        {
            p.strokeColor = color;
            p.lineWidth = lineWidth;
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.Stroke();
        }

        private static void StrokeCircle(Painter2D p, Vector2 center, float radius, Color color, float lineWidth)
        {
            if (radius <= 0.1f) return;
            p.strokeColor = color;
            p.lineWidth = lineWidth;
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();
        }

        private static void FillCircle(Painter2D p, Vector2 center, float radius, Color color)
        {
            if (radius <= 0.1f) return;
            p.fillColor = color;
            p.BeginPath();
            p.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            p.ClosePath();
            p.Fill();
        }
    }
}
