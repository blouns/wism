using Assets.Scripts.Managers;
using System;
using Wism.Client.Entities;

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
                throw new System.ArgumentException($"'{nameof(gameDisplayName)}' cannot be null or whitespace", nameof(gameDisplayName));
            }

            if (unityGame is null)
            {
                throw new System.ArgumentNullException(nameof(unityGame));
            }

            Initialize(gameDisplayName, unityGame);
        }

        private void Initialize(string gameDisplayName, UnityManager unityGame)
        {
            this.DisplayName = gameDisplayName;
            this.WorldName = unityGame.GetComponent<GameFactory>().WorldName;   // TODO: Resolve dupe world name with GameManager
            this.LastCommandId = unityGame.LastCommandId;
            var cameraPosition = unityGame.GetMainCamera().transform.position;
            this.CameraPosition = new float[3]
            {
                cameraPosition.x,
                cameraPosition.y,
                cameraPosition.z
            };
        }

        /// <summary>
        /// Save file friendly name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// World name
        /// </summary>
        public string WorldName { get; set; }

        /// <summary>
        /// Last execution command run
        /// </summary>
        public int LastCommandId { get; set; }

        /// <summary>
        /// Camera position
        /// </summary>
        public float[] CameraPosition { get; set; }

        public GameEntity WismGameEntity { get; set; }
    }
}
