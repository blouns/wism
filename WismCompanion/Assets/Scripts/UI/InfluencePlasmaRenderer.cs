using UnityEngine;

namespace WismCompanion.UI
{
    /// <summary>
    /// V2 "Plasma" GPU renderer for the influence overlay. Packs the morphed field into a small
    /// float texture, blits it through the <c>Wism/InfluencePlasma</c> material into a supersampled
    /// <see cref="RenderTexture"/> (flow + palette + glow), and hands the result back to
    /// <see cref="MapView"/> to draw as textured quads.
    /// </summary>
    /// <remarks>
    /// Fully self-healing: if the shader is missing or a GPU resource fails to allocate it flags
    /// itself unavailable and the overlay falls back to the Painter2D Aurora path. Resources are
    /// lazily (re)created when the field dimensions change.
    /// </remarks>
    public sealed class InfluencePlasmaRenderer
    {
        private const int TargetResolution = 256; // supersample the field up to ~this many texels/side
        private const int MaxScale = 8;

        private Material material;
        private Texture2D fieldTex;
        private RenderTexture heatRt;
        private Color[] scratch;
        private int width, height;
        private bool failed;

        public bool Ready => material != null && heatRt != null && fieldTex != null && !failed;

        private bool TryInitialize()
        {
            if (failed) return false;
            if (material != null) return true;

            var shader = Shader.Find("Wism/InfluencePlasma");
            if (shader == null) { failed = true; return false; }

            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return true;
        }

        private bool EnsureResources(int w, int h)
        {
            if (!TryInitialize()) return false;
            if (fieldTex != null && heatRt != null && w == width && h == height) return true;

            width = w;
            height = h;

            if (fieldTex != null) Object.Destroy(fieldTex);
            fieldTex = new Texture2D(w, h, TextureFormat.RGBAFloat, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            if (heatRt != null) heatRt.Release();
            var scale = Mathf.Clamp(TargetResolution / Mathf.Max(1, Mathf.Max(w, h)), 1, MaxScale);
            heatRt = new RenderTexture(w * scale, h * scale, 0, RenderTextureFormat.ARGBHalf)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!heatRt.Create()) { failed = true; return false; }

            scratch = new Color[w * h];
            return true;
        }

        /// <summary>Upload the field, blit through the plasma material, and return the heat texture (or null).</summary>
        public RenderTexture Render(float[] tension, float[] friendly, float[] enemy, int w, int h, float opacity, int channel)
        {
            if (w <= 0 || h <= 0 || tension == null) return null;
            if (!EnsureResources(w, h)) return null;

            var n = w * h;
            for (var i = 0; i < n; i++)
            {
                var ten = i < tension.Length ? tension[i] : 0f;
                var fr = friendly != null && i < friendly.Length ? friendly[i] : 0f;
                var en = enemy != null && i < enemy.Length ? enemy[i] : 0f;
                scratch[i] = new Color((ten + 1f) * 0.5f, fr, en, 1f);
            }

            fieldTex.SetPixels(scratch);
            fieldTex.Apply(false);

            material.SetFloat("_Opacity", opacity);
            material.SetFloat("_Channel", channel);

            Graphics.Blit(fieldTex, heatRt, material);
            return heatRt;
        }

        public void Dispose()
        {
            if (heatRt != null) { heatRt.Release(); heatRt = null; }
            if (fieldTex != null) { Object.Destroy(fieldTex); fieldTex = null; }
            if (material != null) { Object.Destroy(material); material = null; }
        }
    }
}
