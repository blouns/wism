using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Assets.Scripts.UI;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WismUnity.EditorBridge
{
    public static class WismUiPipelineCommands
    {
        private const string CaptureDirectory = "Library/WismUiCaptures";

        [CliCommand("wism_ui_inventory", "Inventory WISM UI semantics, states, geometry, fonts, overflow, and raycast order.", Tags = new[] { "wism/ui" })]
        public static object Inventory()
        {
            var surfaces = FindSceneObjects<WismUiSurface>()
                .OrderBy(surface => surface.SurfaceId, StringComparer.Ordinal)
                .Select(surface => new
                {
                    surfaceId = surface.SurfaceId,
                    requiredStates = surface.RequiredStates.Select(state => state.ToString()).ToArray(),
                    controls = surface.GetComponentsInChildren<WismUiControl>(true)
                        .OrderBy(control => control.SemanticId, StringComparer.Ordinal)
                        .Select(ControlSnapshot)
                        .ToArray()
                })
                .ToArray();

            var undeclared = FindSceneObjects<Selectable>()
                .Where(selectable => selectable.GetComponent<WismUiControl>() == null)
                .Select(selectable => HierarchyPath(selectable.transform))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            return Success("WISM UI inventory completed.", new
            {
                surfaces,
                declaredSurfaceCount = surfaces.Length,
                declaredControlCount = surfaces.Sum(surface => surface.controls.Length),
                undeclaredSelectableCount = undeclared.Length,
                undeclaredSelectables = undeclared,
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [CliCommand("wism_ui_exercise", "Build semantic mouse, keyboard, and simulated-touch traces for declared WISM UI workflows.", Tags = new[] { "wism/ui" })]
        public static object Exercise()
        {
            var modalities = Enum.GetNames(typeof(WismUiInputModality));
            var traces = FindSceneObjects<WismUiSurface>()
                .OrderBy(surface => surface.SurfaceId, StringComparer.Ordinal)
                .SelectMany(surface => modalities.Select(modality => new
                {
                    workflowId = WorkflowId(surface.SurfaceId),
                    surfaceId = surface.SurfaceId,
                    modality,
                    actions = surface.GetComponentsInChildren<WismUiControl>(true)
                        .Where(control => !string.IsNullOrWhiteSpace(control.ActionId))
                        .OrderBy(control => control.SemanticId, StringComparer.Ordinal)
                        .Select(control => new
                        {
                            controlId = control.SemanticId,
                            actionId = control.ActionId,
                            role = control.Role.ToString(),
                            state = control.State.ToString(),
                            accepted = control.IsEnabled
                        })
                        .ToArray()
                }))
                .ToArray();

            return Success("WISM UI semantic exercise traces completed.", new
            {
                traces,
                traceCount = traces.Length,
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [CliCommand("wism_ui_capture", "Capture the current WISM Game view and a semantic geometry manifest under the ignored Unity Library tree.", Tags = new[] { "wism/ui/visual" })]
        public static object Capture()
        {
            if (!EditorApplication.isPlaying)
            {
                return Error("PLAY_MODE_REQUIRED", "Enter Play Mode before capturing a WISM UI surface.");
            }

            Directory.CreateDirectory(CaptureDirectory);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var imagePath = Path.GetFullPath(Path.Combine(CaptureDirectory, "wism-ui-" + timestamp + ".png"));
            var manifestPath = Path.ChangeExtension(imagePath, ".geometry.json");
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
            {
                return Error("CAPTURE_FAILED", "Unity did not return a Game view texture.");
            }

            try
            {
                var bytes = texture.EncodeToPNG();
                File.WriteAllBytes(imagePath, bytes);
                var controls = FindSceneObjects<WismUiControl>()
                    .OrderBy(control => control.SemanticId, StringComparer.Ordinal)
                    .Select(SerializableControlSnapshot)
                    .ToArray();
                var manifest = new CaptureManifest
                {
                    schemaVersion = 1,
                    timestampUtc = DateTime.UtcNow.ToString("O"),
                    width = texture.width,
                    height = texture.height,
                    imageSha256 = Sha256(bytes),
                    surfaces = FindSceneObjects<WismUiSurface>()
                        .Select(surface => surface.SurfaceId)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray(),
                    controls = controls,
                    lockedRegions = controls
                        .Where(control => control.role != WismUiControlRole.Status.ToString())
                        .Select(control => control.visual)
                        .ToArray()
                };
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
                return Success("WISM UI capture completed.", new
                {
                    imagePath,
                    manifestPath,
                    manifest.imageSha256,
                    manifest.width,
                    manifest.height,
                    controlCount = manifest.controls.Length
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [CliCommand("wism_ui_compare", "Compare the two latest WISM UI captures for pixel and geometry drift.", Tags = new[] { "wism/ui/visual" })]
        public static object Compare()
        {
            if (!Directory.Exists(CaptureDirectory))
            {
                return Error("CAPTURES_MISSING", "No WISM UI capture directory exists.");
            }

            var paths = Directory.GetFiles(CaptureDirectory, "*.png")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(2)
                .ToArray();
            if (paths.Length < 2)
            {
                return Error("CAPTURES_MISSING", "Two WISM UI captures are required.");
            }

            var candidate = LoadTexture(paths[0]);
            var baseline = LoadTexture(paths[1]);
            var candidateManifest = LoadManifest(Path.ChangeExtension(paths[0], ".geometry.json"));
            var baselineManifest = LoadManifest(Path.ChangeExtension(paths[1], ".geometry.json"));
            try
            {
                if (candidate.width != baseline.width || candidate.height != baseline.height)
                {
                    return Success("WISM UI capture dimensions differ.", new
                    {
                        baselinePath = paths[1],
                        candidatePath = paths[0],
                        dimensionsMatch = false,
                        baselineSize = new[] { baseline.width, baseline.height },
                        candidateSize = new[] { candidate.width, candidate.height }
                    });
                }

                var baselinePixels = baseline.GetPixels32();
                var candidatePixels = candidate.GetPixels32();
                long absoluteDelta = 0;
                var changed = 0;
                var lockedPixels = 0;
                var lockedChanged = 0;
                var perceptualPixels = 0;
                var perceptualChanged = 0;
                double perceptualDelta = 0d;
                for (var i = 0; i < baselinePixels.Length; i++)
                {
                    var delta = ChannelDelta(baselinePixels[i], candidatePixels[i]);
                    absoluteDelta += delta;
                    if (delta > 8)
                    {
                        changed++;
                    }

                    var point = new Vector2(i % baseline.width, i / baseline.width);
                    if (baselineManifest.lockedRegions.Any(region => region.Contains(point)))
                    {
                        lockedPixels++;
                        if (delta > 0)
                        {
                            lockedChanged++;
                        }
                    }
                    else
                    {
                        var difference = PerceptualDelta(baselinePixels[i], candidatePixels[i]);
                        perceptualPixels++;
                        perceptualDelta += difference;
                        if (difference > 0.035d)
                        {
                            perceptualChanged++;
                        }
                    }
                }

                var geometry = CompareGeometry(baselineManifest, candidateManifest);

                return Success("WISM UI capture comparison completed.", new
                {
                    baselinePath = paths[1],
                    candidatePath = paths[0],
                    dimensionsMatch = true,
                    changedPixelRatio = baselinePixels.Length == 0 ? 0d : (double)changed / baselinePixels.Length,
                    meanChannelDelta = baselinePixels.Length == 0 ? 0d : (double)absoluteDelta / (baselinePixels.Length * 4d),
                    lockedRegionChangedPixelRatio = lockedPixels == 0 ? 0d : (double)lockedChanged / lockedPixels,
                    maskedPerceptualChangedPixelRatio = perceptualPixels == 0 ? 0d : (double)perceptualChanged / perceptualPixels,
                    maskedMeanPerceptualDelta = perceptualPixels == 0 ? 0d : perceptualDelta / perceptualPixels,
                    geometry,
                    baselineGeometryPath = Path.ChangeExtension(paths[1], ".geometry.json"),
                    candidateGeometryPath = Path.ChangeExtension(paths[0], ".geometry.json")
                });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(candidate);
                UnityEngine.Object.DestroyImmediate(baseline);
            }
        }

        private static object ControlSnapshot(WismUiControl control)
        {
            var hitArea = control.GetComponent<WismHitArea>();
            var text = control.GetComponentInChildren<Text>(true);
            var rect = control.GetComponent<RectTransform>();
            var visual = hitArea == null ? default : hitArea.GetVisualScreenBounds();
            var effective = hitArea == null ? visual : hitArea.GetEffectiveScreenBounds(WismUiInputModality.SimulatedTouch);
            return new
            {
                semanticId = control.SemanticId,
                actionId = control.ActionId,
                role = control.Role.ToString(),
                state = control.State.ToString(),
                overlapPriority = control.OverlapPriority,
                hierarchy = HierarchyPath(control.transform),
                visualBounds = RectSnapshot(visual),
                effectiveBounds = RectSnapshot(effective),
                font = text != null && text.font != null ? text.font.name : string.Empty,
                fontSize = text == null ? 0 : text.fontSize,
                textOverflow = text != null && rect != null && (text.preferredWidth > rect.rect.width + 0.5f || text.preferredHeight > rect.rect.height + 0.5f),
                raycastTargets = control.GetComponentsInChildren<Graphic>(true).Count(graphic => graphic.raycastTarget)
            };
        }

        private static SerializableControl SerializableControlSnapshot(WismUiControl control)
        {
            var hitArea = control.GetComponent<WismHitArea>();
            var visual = hitArea == null ? default : hitArea.GetVisualScreenBounds();
            var effective = hitArea == null ? visual : hitArea.GetEffectiveScreenBounds(WismUiInputModality.SimulatedTouch);
            return new SerializableControl
            {
                semanticId = control.SemanticId,
                actionId = control.ActionId,
                role = control.Role.ToString(),
                state = control.State.ToString(),
                visual = SerializableRect.From(visual),
                effective = SerializableRect.From(effective)
            };
        }

        private static object RectSnapshot(Rect rect) => new { rect.x, rect.y, rect.width, rect.height };

        private static T[] FindSceneObjects<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component.gameObject.scene.IsValid())
                .ToArray();
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }

            return string.Join("/", names);
        }

        private static Texture2D LoadTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not decode UI capture: " + path);
            }

            return texture;
        }

        private static int ChannelDelta(Color32 left, Color32 right)
        {
            return Math.Abs(left.r - right.r) + Math.Abs(left.g - right.g) + Math.Abs(left.b - right.b) + Math.Abs(left.a - right.a);
        }

        private static double PerceptualDelta(Color32 left, Color32 right)
        {
            var red = (left.r - right.r) / 255d;
            var green = (left.g - right.g) / 255d;
            var blue = (left.b - right.b) / 255d;
            return Math.Sqrt((0.2126d * red * red) + (0.7152d * green * green) + (0.0722d * blue * blue));
        }

        private static object CompareGeometry(CaptureManifest baseline, CaptureManifest candidate)
        {
            var baselineControls = baseline.controls
                .GroupBy(control => control.semanticId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var candidateControls = candidate.controls
                .GroupBy(control => control.semanticId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var removed = baselineControls.Keys.Except(candidateControls.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var added = candidateControls.Keys.Except(baselineControls.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var shared = baselineControls.Keys.Intersect(candidateControls.Keys, StringComparer.Ordinal).ToArray();
            var stateChanges = shared
                .Where(id => baselineControls[id].state != candidateControls[id].state)
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => new { semanticId = id, baseline = baselineControls[id].state, candidate = candidateControls[id].state })
                .ToArray();
            var drift = shared
                .Select(id => new
                {
                    semanticId = id,
                    maximumPixels = MaximumDifference(baselineControls[id].visual, candidateControls[id].visual)
                })
                .Where(item => item.maximumPixels > 0.5f)
                .OrderByDescending(item => item.maximumPixels)
                .ToArray();
            return new
            {
                added,
                removed,
                stateChanges,
                drift,
                maximumDriftPixels = drift.Length == 0 ? 0f : drift[0].maximumPixels
            };
        }

        private static float MaximumDifference(SerializableRect left, SerializableRect right)
        {
            return Mathf.Max(
                Mathf.Abs(left.x - right.x),
                Mathf.Abs(left.y - right.y),
                Mathf.Abs(left.width - right.width),
                Mathf.Abs(left.height - right.height));
        }

        private static CaptureManifest LoadManifest(string path)
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Missing UI geometry manifest: " + path);
            }

            return JsonUtility.FromJson<CaptureManifest>(File.ReadAllText(path)) ?? new CaptureManifest();
        }

        private static string WorkflowId(string surfaceId)
        {
            return surfaceId switch
            {
                "army-selection" => "select-or-attack-army",
                "owned-cities-production" => "manage-owned-city-production",
                "single-city-production" => "manage-single-city-production",
                "game-setup" => "configure-and-start-game",
                _ => "inspect-" + surfaceId
            };
        }

        private static string Sha256(byte[] bytes)
        {
            using var algorithm = SHA256.Create();
            return string.Concat(algorithm.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static object Success(string message, object data) => new { success = true, message, data };
        private static object Error(string code, object details) => new { success = false, error = new { code, details } };

        [Serializable]
        private sealed class CaptureManifest
        {
            public int schemaVersion;
            public string timestampUtc;
            public int width;
            public int height;
            public string imageSha256;
            public string[] surfaces = Array.Empty<string>();
            public SerializableControl[] controls;
            public SerializableRect[] lockedRegions = Array.Empty<SerializableRect>();
        }

        [Serializable]
        private sealed class SerializableControl
        {
            public string semanticId;
            public string actionId;
            public string role;
            public string state;
            public SerializableRect visual;
            public SerializableRect effective;
        }

        [Serializable]
        private struct SerializableRect
        {
            public float x;
            public float y;
            public float width;
            public float height;

            public bool Contains(Vector2 point)
            {
                return point.x >= this.x && point.x <= this.x + this.width &&
                    point.y >= this.y && point.y <= this.y + this.height;
            }

            public static SerializableRect From(Rect value) => new SerializableRect
            {
                x = value.x,
                y = value.y,
                width = value.width,
                height = value.height
            };
        }
    }
}
