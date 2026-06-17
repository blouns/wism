using NUnit.Framework;
using UnityEngine;
using WismCompanion.UI;

namespace WismCompanion.Tests
{
    public sealed class InfluencePlasmaRendererTests
    {
        [Test]
        public void RenderProducesVisiblePlasmaPixels()
        {
            var renderer = new InfluencePlasmaRenderer();
            RenderTexture rt = null;
            RenderTexture previous = RenderTexture.active;

            try
            {
                var width = 8;
                var height = 8;
                var tension = new float[width * height];
                var friendly = new float[width * height];
                var enemy = new float[width * height];

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var i = (y * width) + x;
                        tension[i] = x < width / 2 ? 0.85f : -0.85f;
                        friendly[i] = Mathf.InverseLerp(0, width - 1, x);
                        enemy[i] = Mathf.InverseLerp(width - 1, 0, x);
                    }
                }

                rt = renderer.Render(
                    tension,
                    friendly,
                    enemy,
                    width,
                    height,
                    0.8f,
                    (int)InfluenceChannel.Tension,
                    true,
                    new Color(0.15f, 0.85f, 1f),
                    new Color(0.25f, 0.45f, 1f),
                    new Color(1f, 0.45f, 0.1f),
                    new Color(1f, 0.12f, 0.12f));

                Assert.That(rt, Is.Not.Null, "Plasma renderer should find and execute the influence shader.");

                var sample = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;
                sample.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                sample.Apply();

                var pixels = sample.GetPixels();
                Object.DestroyImmediate(sample);

                var visible = 0;
                var colorful = 0;
                foreach (var pixel in pixels)
                {
                    if (pixel.a > 0.05f)
                    {
                        visible++;
                    }

                    var channelSpread = Mathf.Max(pixel.r, pixel.g, pixel.b) - Mathf.Min(pixel.r, pixel.g, pixel.b);
                    if (pixel.a > 0.05f && channelSpread > 0.08f)
                    {
                        colorful++;
                    }
                }

                Assert.That(visible, Is.GreaterThan(pixels.Length / 4), "Plasma output should not be blank or fully transparent.");
                Assert.That(colorful, Is.GreaterThan(pixels.Length / 5), "Plasma output should contain colored heat, not a flat mask.");
            }
            finally
            {
                RenderTexture.active = previous;
                renderer.Dispose();
            }
        }
    }
}
