using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WismCompanion.EditorTools
{
    /// <summary>
    /// Ensures the UI Toolkit assets the runtime bootstrap expects exist: a default runtime theme and
    /// a PanelSettings (wired to that theme) under Resources. Runs automatically on editor load so the
    /// project is press-Play ready; also exposed as a menu item for an explicit re-run.
    ///
    /// The default font is applied at runtime by CompanionBootstrap.ApplyDefaultFont (which sets a
    /// concrete FontAsset on the root) — so no PanelTextSettings asset is needed, and we avoid baking a
    /// HideFlags.DontSave font asset into the build.
    /// </summary>
    [InitializeOnLoad]
    public static class CompanionSetup
    {
        private const string ThemeDir = "Assets/UI Toolkit/UnityThemes";
        private const string ThemePath = ThemeDir + "/UnityDefaultRuntimeTheme.tss";
        private const string ResourcesDir = "Assets/Resources";
        private const string PanelSettingsPath = ResourcesDir + "/CompanionPanelSettings.asset";

        static CompanionSetup()
        {
            // In batchmode the build invokes EnsureAssets() synchronously; auto-running here would race
            // the build's asset pipeline and trip an "m_LockCount == 0" assertion in BuildPlayer.
            if (Application.isBatchMode)
            {
                return;
            }

            // Asset mutations are not safe directly inside the static constructor during domain reload.
            EditorApplication.delayCall += () => EnsureAssets(false);
        }

        [MenuItem("WISM/Create Companion Assets")]
        public static void CreateAssetsMenu()
        {
            EnsureAssets(true);
        }

        internal static void EnsureAssets(bool verbose)
        {
            var theme = EnsureRuntimeTheme();
            var changed = EnsurePanelSettings(theme);
            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (verbose)
            {
                Debug.Log("[WismCompanion] Companion assets ready. Press Play to launch the companion.");
            }
        }

        private static ThemeStyleSheet EnsureRuntimeTheme()
        {
            Directory.CreateDirectory(ThemeDir);
            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
            }

            AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        }

        private static bool EnsurePanelSettings(ThemeStyleSheet theme)
        {
            if (!AssetDatabase.IsValidFolder(ResourcesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.themeStyleSheet = theme;
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
                return true;
            }

            if (theme != null && settings.themeStyleSheet == null)
            {
                settings.themeStyleSheet = theme;
                EditorUtility.SetDirty(settings);
                return true;
            }

            return false;
        }
    }
}
