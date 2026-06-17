using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WismCompanion.UI
{
    /// <summary>
    /// "Embers" (stretch): a CPU particle field of glowing sparks that stream along the tension
    /// gradient — toward the enemy, with curl turbulence — and twinkle as they fade. Drawn in
    /// Painter2D so it lives inside the UI Toolkit map and tracks pan/zoom exactly like the rest of
    /// the overlay.
    /// </summary>
    /// <remarks>
    /// A true GPU VFX-Graph version would need the companion re-architected around a world-space
    /// board; this delivers the living-sparks look within the existing app, off by default.
    /// </remarks>
    public sealed class EmberField
    {
        private const int Budget = 260;
        private const float Speed = 2.2f;        // tiles/second along the gradient
        private const float Damping = 4.0f;
        private const float MinMagnitude = 0.12f; // sparks only live where the field is meaningful

        public bool Enabled { get; set; }

        private struct Ember { public float X, Y, Vx, Vy, Life, MaxLife, Seed; public bool Alive; }

        private Ember[] embers;
        private float[] field;     // reference to the overlay's smoothed tension
        private int width, height;
        private readonly System.Random rng = new System.Random(20260616);
        private float clock;

        /// <summary>Bind the (smoothed) tension array the sparks read. Cheap to call each snapshot.</summary>
        public void SetField(float[] tension, int w, int h)
        {
            field = tension;
            width = w;
            height = h;
            if (embers == null || embers.Length != Budget) embers = new Ember[Budget];
        }

        public void Tick(float dt)
        {
            if (!Enabled || field == null || width <= 2 || height <= 2 || embers == null) return;
            clock += dt;

            for (var i = 0; i < embers.Length; i++)
            {
                ref var e = ref embers[i];
                if (!e.Alive)
                {
                    if (rng.NextDouble() < 0.5) Spawn(ref e);
                    continue;
                }

                var ix = Mathf.Clamp((int)e.X, 1, width - 2);
                var iy = Mathf.Clamp((int)e.Y, 1, height - 2);
                var gx = field[(iy * width) + ix + 1] - field[(iy * width) + ix - 1];
                var gy = field[((iy + 1) * width) + ix] - field[((iy - 1) * width) + ix];
                var mag = Mathf.Sqrt((gx * gx) + (gy * gy));

                // Flow down-gradient (toward the enemy) plus a perpendicular curl for turbulence.
                var dirx = mag > 1e-4f ? -gx / mag : 0f;
                var diry = mag > 1e-4f ? -gy / mag : 0f;
                var curl = Mathf.Sin((e.X + e.Y + clock) * 1.7f + e.Seed) * 0.5f;
                var ax = (dirx * Speed) + (-diry * curl);
                var ay = (diry * Speed) + (dirx * curl);

                var blend = Mathf.Clamp01(dt * Damping);
                e.Vx += (ax - e.Vx) * blend;
                e.Vy += (ay - e.Vy) * blend;
                e.X += e.Vx * dt;
                e.Y += e.Vy * dt;
                e.Life -= dt;

                if (e.Life <= 0f || e.X < 0f || e.Y < 0f || e.X >= width || e.Y >= height ||
                    Mathf.Abs(SampleClamped(e.X, e.Y)) < MinMagnitude)
                {
                    e.Alive = false;
                }
            }
        }

        private void Spawn(ref Ember e)
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var x = rng.Next(1, width - 1);
                var y = rng.Next(1, height - 1);
                var m = Mathf.Abs(field[(y * width) + x]);
                if (m > MinMagnitude && rng.NextDouble() < m)
                {
                    e.X = x + 0.5f;
                    e.Y = y + 0.5f;
                    e.Vx = 0f;
                    e.Vy = 0f;
                    e.MaxLife = 1.2f + ((float)rng.NextDouble() * 1.6f);
                    e.Life = e.MaxLife;
                    e.Seed = (float)rng.NextDouble() * 6.28f;
                    e.Alive = true;
                    return;
                }
            }
        }

        private float SampleClamped(float fx, float fy)
        {
            var ix = Mathf.Clamp((int)fx, 0, width - 1);
            var iy = Mathf.Clamp((int)fy, 0, height - 1);
            return field[(iy * width) + ix];
        }

        public void Draw(Painter2D p, int originX, int originY, int tilesW, int tilesH,
            Func<float, float, Vector2> mapToViewportF, float tile, Color friendly, Color enemy, float opacity)
        {
            if (!Enabled || embers == null || field == null) return;

            for (var i = 0; i < embers.Length; i++)
            {
                var e = embers[i];
                if (!e.Alive) continue;
                if (e.X < originX || e.Y < originY || e.X >= originX + tilesW || e.Y >= originY + tilesH) continue;

                var head = mapToViewportF(e.X, e.Y);
                var tail = mapToViewportF(e.X - (e.Vx * 0.15f), e.Y - (e.Vy * 0.15f));

                var lifeT = Mathf.Clamp01(e.Life / Mathf.Max(0.01f, e.MaxLife));
                var env = Mathf.Sin(lifeT * Mathf.PI); // fade in then out
                var twinkle = 0.6f + (0.4f * Mathf.Sin((clock * 7f) + e.Seed));
                var a = env * twinkle * opacity;
                if (a < 0.03f) continue;

                var col = SampleClamped(e.X, e.Y) >= 0f ? friendly : enemy;
                var tailCol = col; tailCol.a = a * 0.4f;
                StrokeLine(p, tail, head, tailCol, Mathf.Max(1f, tile * 0.05f));

                var headCol = Color.Lerp(col, Color.white, 0.6f); headCol.a = a;
                FillCircle(p, head, Mathf.Max(1.2f, tile * 0.09f), headCol);
            }
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
