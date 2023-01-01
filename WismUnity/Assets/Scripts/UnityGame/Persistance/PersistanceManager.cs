using Assets.Scripts.Persistance.Entities;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.Data;

namespace Assets.Scripts.Managers
{
    public static class PersistanceManager
    {
        private static UnityGameEntity snapshot;

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

            // Persist Unity game state
            var snapshot = new UnityGameEntity(saveGameName, unityGame);

            // Persist WISM game state
            snapshot.WismGameEntity = Game.Current.Snapshot();
            snapshot.WismGameEntity.World.Name = snapshot.WorldName;

            // Write to disk
            string path = Application.persistentDataPath + "/" + filename;
            var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
            var json = JsonConvert.SerializeObject(snapshot, settings);
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.Write(json);
            }

            Debug.Log($"Saved game successfully to '{path}'.");
        }

        /// <summary>
        /// Loads the last snapshot.
        /// </summary>
        /// <param name="unityGame">Current unity manager</param>
        internal static void LoadLastSnapshot(UnityManager unityGame)
        {
            var snapshot = GetLastSnapshot();
            unityGame.GetMainCamera().transform.position = new Vector3(
                snapshot.CameraPosition[0],
                snapshot.CameraPosition[1],
                snapshot.CameraPosition[2]);
            unityGame.LastCommandId = snapshot.LastCommandId;
            unityGame.GetComponent<UnityGameFactory>().WorldName = snapshot.WorldName;
        }

        public static UnityGameEntity LoadEntities(string path, UnityManager unityGame)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace", nameof(path));
            }

            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException("File could not be loaded because the file was not found: " + path);
            }

            var json = File.ReadAllText(path);

            var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };
            snapshot = JsonConvert.DeserializeObject<UnityGameEntity>(json, settings);
            return snapshot;
        }

        internal static void SetLastSnapshot(UnityGameEntity unityGameEntity)
        {
            snapshot = unityGameEntity;
        }

        public static UnityGameEntity GetLastSnapshot()
        {
            return snapshot;
        }
    }
}
