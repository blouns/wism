using System.Collections.Generic;

namespace Assets.Scripts.UnityGame.Persistance.Entities
{
    public class UnityNewGameEntity
    {
        public string WorldName { get; set; }

        public bool RandomStartLocations { get; set; }

        public bool InteractiveUI { get; set; }

        public int  RandomSeed { get; set; }

        public UnityPlayerEntity[] Players { get; set; }

        public bool IsNewGame { get; set; }
    }
}
