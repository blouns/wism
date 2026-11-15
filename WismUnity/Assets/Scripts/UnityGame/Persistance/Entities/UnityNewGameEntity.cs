using Wism.Client.Data.Entities;
using Assets.Scripts.Persistance.Entities;

namespace Assets.Scripts.UnityGame.Persistance.Entities
{
    public class UnityNewGameEntity
    {
        public string WorldName { get; set; }

        public bool RandomStartLocations { get; set; }

        public bool InteractiveUI { get; set; }

        public bool ShowAiCombat { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public bool ObserveAiMovement { get; set; } = true;

        public int RandomSeed { get; set; }

        public UnityPlayerEntity[] Players { get; set; }

        public bool IsNewGame { get; set; }
        public string LoadFilename { get; set; }
        internal UnityGameEntity PreparedLoadSnapshot { get; set; }

        public ModKitSelectionEntity ModKitSelection { get; set; }
    }
}
