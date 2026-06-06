using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Managers;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Wism.Client.Data.Entities;
using Wism.Client.Modules;
using Wism.Client.Modules.Profiles;

namespace Assets.Scripts.UnityGame.ModKit
{
    public static class UnityModKitSelection
    {
        public const string PluginModRoot = @"Assets\Plugins\WismClient\Mods";

        public static UnityModKitSelectionReport Inspect(
            string profileId,
            string[] packIds,
            string worldOverride,
            string modRootOverride)
        {
            var report = CreateBaseReport(profileId, packIds, worldOverride, modRootOverride);
            if (!report.hasExplicitSelection)
            {
                report.status = "Default";
                report.outcome = "No explicit Mod Kit selection was provided; Unity default mod settings are unchanged.";
                report.modRoot = ResolveDefaultModRoot(modRootOverride, false);
                report.worldName = string.IsNullOrWhiteSpace(worldOverride)
                    ? GameManager.DefaultWorld
                    : worldOverride;
                report.activePackIds = new string[0];
                report.sceneModDrift = BuildDriftSummary(report.modRoot, report.worldName);
                return report;
            }

            ResolveSelection(report);
            return report;
        }

        public static UnityModKitSelectionReport Apply(
            UnityManager unityManager,
            string profileId,
            string[] packIds,
            string worldOverride,
            string modRootOverride)
        {
            var report = Inspect(profileId, packIds, worldOverride, modRootOverride);
            if (!report.isLoadable && report.status == "Failed")
            {
                throw new InvalidOperationException(report.outcome);
            }

            ModFactory.ModPath = report.modRoot;
            ModFactory.WorldPath = report.worldName;
            ModFactory.ActiveFeaturePackIds = report.activePackIds.ToList();
            ModFactory.ResetCache();

            if (unityManager != null && unityManager.GameManager != null)
            {
                unityManager.GameManager.ModPath = report.modRoot;
                unityManager.GameManager.WorldName = report.worldName;
            }

            report.applied = true;
            report.outcome = report.hasExplicitSelection
                ? "Applied explicit Mod Kit selection to ModFactory and GameManager."
                : "Applied Unity default mod settings to ModFactory and GameManager.";
            return report;
        }

        public static UnityModKitSelectionReport ApplySavedSelection(
            UnityManager unityManager,
            ModKitSelectionEntity savedSelection,
            string modRootOverride)
        {
            var modRoot = ResolveDefaultModRoot(modRootOverride, true);
            var report = CreateBaseReport(
                savedSelection == null ? ModularGameProfileCatalog.DefaultProfileId : savedSelection.ProfileId,
                savedSelection == null ? Array.Empty<string>() : savedSelection.PackIds,
                savedSelection == null ? string.Empty : savedSelection.World,
                modRoot);
            var compatibility = ModKitSelectionService.VerifySavedSelection(modRoot, savedSelection, ResolveWismVersion());
            ApplyCompatibility(report, compatibility);
            if (!compatibility.IsLoadable)
            {
                throw new InvalidOperationException(report.outcome);
            }

            ModFactory.ModPath = report.modRoot;
            ModFactory.WorldPath = report.worldName;
            ModFactory.ActiveFeaturePackIds = report.activePackIds.ToList();
            ModFactory.ResetCache();

            if (unityManager != null && unityManager.GameManager != null)
            {
                unityManager.GameManager.ModPath = report.modRoot;
                unityManager.GameManager.WorldName = report.worldName;
            }

            report.applied = true;
            report.outcome = savedSelection == null
                ? "Loaded legacy save with default no-pack Mod Kit settings."
                : "Applied saved Mod Kit selection to ModFactory and GameManager.";
            return report;
        }

        static UnityModKitSelectionReport CreateBaseReport(
            string profileId,
            string[] packIds,
            string worldOverride,
            string modRootOverride)
        {
            var packs = packIds ?? new string[0];
            var hasExplicitSelection =
                !string.IsNullOrWhiteSpace(profileId) ||
                packs.Any(id => !string.IsNullOrWhiteSpace(id)) ||
                !string.IsNullOrWhiteSpace(modRootOverride);

            return new UnityModKitSelectionReport
            {
                schemaVersion = 1,
                profileId = string.IsNullOrWhiteSpace(profileId)
                    ? ModularGameProfileCatalog.DefaultProfileId
                    : profileId,
                requestedPackIds = packs.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray(),
                worldOverride = worldOverride ?? string.Empty,
                modRootOverride = modRootOverride ?? string.Empty,
                hasExplicitSelection = hasExplicitSelection,
                timestampUtc = DateTime.UtcNow.ToString("O")
            };
        }

        static void ResolveSelection(UnityModKitSelectionReport report)
        {
            report.modRoot = ResolveDefaultModRoot(report.modRootOverride, true);
            var validation = ModKitValidator.ValidateModRoot(report.modRoot);
            report.validation = new UnityModKitValidationSummary
            {
                isValid = validation.IsValid,
                issueCount = validation.Issues.Count,
                issues = validation.Issues.Select(issue => new UnityModKitValidationIssueSummary
                {
                    severity = issue.Severity.ToString(),
                    code = issue.Code,
                    message = issue.Message,
                    path = issue.Path
                }).ToArray()
            };

            try
            {
                var selection = ModularGameProfileCatalog.ResolveFromModRoot(
                    report.modRoot,
                    report.profileId,
                    report.requestedPackIds);
                report.activePackIds = selection.PackIds.ToArray();
                report.baseWorld = selection.BaseWorld;
                report.worldName = !string.IsNullOrWhiteSpace(report.worldOverride)
                    ? report.worldOverride
                    : !string.IsNullOrWhiteSpace(selection.Launch.World)
                        ? selection.Launch.World
                        : selection.BaseWorld;
                report.seed = selection.Launch.Seed ?? GameManager.DefaultRandom;
                report.packCount = selection.Packs.Count;
                report.availablePacks = ModularGameProfileCatalog.DiscoverPacksFromModRoot(report.modRoot)
                    .Select(pack => pack.Id)
                    .ToArray();
                report.sceneModDrift = BuildDriftSummary(report.modRoot, report.worldName);
                ApplyCompatibility(report, ModKitSelectionService.VerifySelection(
                    report.modRoot,
                    report.profileId,
                    report.requestedPackIds,
                    report.worldName,
                    ResolveWismVersion()));
            }
            catch (Exception ex)
            {
                report.status = "Failed";
                report.outcome = ex.Message;
                report.activePackIds = new string[0];
                report.sceneModDrift = BuildDriftSummary(report.modRoot, report.worldName);
            }
        }

        static void ApplyCompatibility(UnityModKitSelectionReport report, ModKitCompatibilityReport compatibility)
        {
            report.compatibilityStatus = compatibility.Status.ToString();
            report.isGreen = compatibility.IsGreen;
            report.isLoadable = compatibility.IsLoadable;
            report.selectionEntity = compatibility.Selection;
            report.contentFingerprint = compatibility.Selection == null
                ? string.Empty
                : compatibility.Selection.ContentFingerprint;
            report.compatibilityIssues = compatibility.Issues.Select(issue => new UnityModKitValidationIssueSummary
            {
                severity = issue.Severity.ToString(),
                code = issue.Code,
                message = issue.Message,
                path = issue.Path
            }).ToArray();
            report.status = compatibility.IsGreen
                ? "Passed"
                : compatibility.IsLoadable
                    ? "Warning"
                    : "Failed";
            report.outcome = compatibility.IsGreen
                ? "Resolved verified Mod Kit selection."
                : $"Mod Kit selection is {compatibility.Status}.";
            if (compatibility.Selection != null)
            {
                report.activePackIds = compatibility.Selection.PackIds ?? Array.Empty<string>();
                report.worldName = compatibility.Selection.World;
            }
        }

        static string ResolveWismVersion()
        {
            return string.IsNullOrWhiteSpace(Application.version)
                ? ModKitSelectionService.CurrentWismVersion
                : Application.version;
        }

        static string ResolveDefaultModRoot(string modRootOverride, bool preferPluginModRoot)
        {
            if (!string.IsNullOrWhiteSpace(modRootOverride))
            {
                return modRootOverride;
            }

            if (preferPluginModRoot && Directory.Exists(PluginModRoot))
            {
                return PluginModRoot;
            }

            return GameManager.DefaultModPath;
        }

        static UnityModKitDriftSummary BuildDriftSummary(string modRoot, string worldName)
        {
            var worldRoot = Path.Combine(modRoot ?? string.Empty, ModFactory.WorldsPath, worldName ?? string.Empty);
            var cityPath = Path.Combine(worldRoot, "City.json");
            var locationPath = Path.Combine(worldRoot, "Location.json");
            return new UnityModKitDriftSummary
            {
                available = Directory.Exists(worldRoot),
                worldName = worldName ?? string.Empty,
                worldRoot = worldRoot,
                cityJsonExists = File.Exists(cityPath),
                locationJsonExists = File.Exists(locationPath),
                cityJsonCount = CountJsonArrayItems(cityPath),
                locationJsonCount = CountJsonArrayItems(locationPath),
                note = "Read-only MOD data count summary. Scene object comparison is reserved for the next drift-report slice."
            };
        }

        static int CountJsonArrayItems(string path)
        {
            try
            {
                return File.Exists(path) ? JArray.Parse(File.ReadAllText(path)).Count : 0;
            }
            catch
            {
                return -1;
            }
        }
    }

    [Serializable]
    public sealed class UnityModKitSelectionReport
    {
        public int schemaVersion;
        public string status;
        public string outcome;
        public bool applied;
        public bool hasExplicitSelection;
        public string profileId;
        public string[] requestedPackIds = new string[0];
        public string[] activePackIds = new string[0];
        public string[] availablePacks = new string[0];
        public string modRoot;
        public string modRootOverride;
        public string baseWorld;
        public string worldName;
        public string worldOverride;
        public int seed;
        public int packCount;
        public bool isGreen;
        public bool isLoadable;
        public string compatibilityStatus;
        public string contentFingerprint;
        public ModKitSelectionEntity selectionEntity;
        public UnityModKitValidationSummary validation;
        public UnityModKitValidationIssueSummary[] compatibilityIssues = new UnityModKitValidationIssueSummary[0];
        public UnityModKitDriftSummary sceneModDrift;
        public string timestampUtc;
    }

    [Serializable]
    public sealed class UnityModKitValidationSummary
    {
        public bool isValid;
        public int issueCount;
        public UnityModKitValidationIssueSummary[] issues = new UnityModKitValidationIssueSummary[0];
    }

    [Serializable]
    public sealed class UnityModKitValidationIssueSummary
    {
        public string severity;
        public string code;
        public string message;
        public string path;
    }

    [Serializable]
    public sealed class UnityModKitDriftSummary
    {
        public bool available;
        public string worldName;
        public string worldRoot;
        public bool cityJsonExists;
        public bool locationJsonExists;
        public int cityJsonCount;
        public int locationJsonCount;
        public string note;
    }
}
