using Assets.Scripts.Persistance.Entities;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Wism.Client.Core;

namespace Assets.Scripts.Managers
{
    public static class PersistanceManager
    {
        public static void Save(string filename, string saveGameName, UnityManager unityGame)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"'{nameof(filename)}' cannot be null or whitespace", nameof(filename));
            }

            if (string.IsNullOrWhiteSpace(saveGameName))
            {
                throw new ArgumentException($"'{nameof(saveGameName)}' cannot be null or whitespace", nameof(saveGameName));
            }

            // Persist WISM game state
            var snapshot = new UnityGameEntity(saveGameName, unityGame);
            var snapshot.WismGameEntity = Game.Current.Snapshot();

            BinaryFormatter binaryFormatter = new BinaryFormatter();            
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                binaryFormatter.Serialize(stream, snapshot);                
            }

           
        }

        public static void Load(string filename, UnityManager unityGame)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"'{nameof(filename)}' cannot be null or whitespace", nameof(filename));
            }

            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }

            if (!File.Exists(filename))
            {
                throw new ArgumentException("File could not be loaded because the file was not found: " + filename);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                var unityGameEntity = binaryFormatter.Deserialize(stream) as UnityGameEntity;
                if (unityGameEntity == null)
                {
                    throw new ArgumentException("File could not be loaded because the file was not found: " + filename);
                }

                // Load Unity game state
                unityGameEntity.WorldName = unityGame.GetComponent<GameFactory>().WorldName;
                unityGameEntity.LastCommandId = unityGame.LastCommandId;
                var positionArray = unityGameEntity.CameraPosition;
                unityGame.GetMainCamera().transform.position = new Vector3(
                    positionArray[0], positionArray[1], positionArray[2]);

                // Load WISM game state


                // TODO: Need a Load Game CommandProcessor to handle calling this function at the right time
                // Implies adding a new Command for Load since it will change game state for remote players.
            }
        }
    }
}
