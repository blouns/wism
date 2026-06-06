using Assets.Scripts.Managers;
using System;
using Wism.Client.Core;
using Wism.Client.Data.Entities;

namespace Assets.Scripts.Persistance.Entities
{
    [Serializable]
    public class UnityGameEntity
    {
        public UnityGameEntity()
        {
        }

        public UnityGameEntity(string gameDisplayName, UnityManager unityGame)
        {
            if (string.IsNullOrWhiteSpace(gameDisplayName))
            {
                throw new ArgumentException($"'{nameof(gameDisplayName)}' cannot be null or whitespace", nameof(gameDisplayName));
            }

            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }

            Initialize(gameDisplayName, unityGame);
        }

        private void Initialize(string gameDisplayName, UnityManager unityGame)
        {
            DisplayName = gameDisplayName;
            WorldName = unityGame.GetComponent<UnityGameFactory>().WorldName;
            ModKitSelection = Game.IsInitialized() ? Game.Current.ModKitSelection : null;
            LastCommandId = unityGame.LastCommandId;
            var cameraPosition = unityGame.GetMainCamera().transform.position;
            CameraPosition = new[]
            {
                cameraPosition.x,
                cameraPosition.y,
                cameraPosition.z
            };
        }

        public string DisplayName { get; set; }
        public string WorldName { get; set; }
        public ModKitSelectionEntity ModKitSelection { get; set; }
        public int LastCommandId { get; set; }
        public float[] CameraPosition { get; set; }
        public GameEntity WismGameEntity { get; set; }
    }
}
