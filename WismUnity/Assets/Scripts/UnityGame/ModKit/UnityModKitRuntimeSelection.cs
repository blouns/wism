using Assets.Scripts.UnityGame.Persistance.Entities;
using Wism.Client.Data.Entities;

namespace Assets.Scripts.UnityGame.ModKit
{
    public static class UnityModKitRuntimeSelection
    {
        public static UnityModKitSelectionReport LastReport { get; private set; }
        public static ModKitSelectionEntity CurrentSelection { get; private set; }
        public static bool HasSelection => CurrentSelection != null;

        public static void Set(UnityModKitSelectionReport report)
        {
            LastReport = report;
            CurrentSelection = report == null ? null : report.selectionEntity;
        }

        public static void Clear()
        {
            LastReport = null;
            CurrentSelection = null;
        }

        public static void ApplyTo(UnityNewGameEntity settings)
        {
            if (settings == null || CurrentSelection == null)
            {
                return;
            }

            settings.ModKitSelection = CurrentSelection;
            settings.WorldName = CurrentSelection.World;
            // A zero seed means Unity should generate a fresh seed when creating the game.
            // Explicit scenario/test seeds remain deterministic.
        }
    }
}
