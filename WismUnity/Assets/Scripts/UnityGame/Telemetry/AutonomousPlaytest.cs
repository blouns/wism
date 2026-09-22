using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.Persistance.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wism.Client.Core;

namespace Assets.Scripts.Telemetry
{
    public sealed class AutonomousPlaytest : MonoBehaviour
    {
        private double deadline;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Environment.GetCommandLineArgs().Contains("-wism-autoplay"))
                new GameObject("Autonomous playtest").AddComponent<AutonomousPlaytest>();
        }

        public static UnityNewGameEntity Settings()
        {
            return new UnityNewGameEntity {
                WorldName = "Illuria", IsNewGame = true, InteractiveUI = true,
                RandomSeed = 1990, RandomStartLocations = false,
                ShowAiCombat = false, ObserveAiMovement = true,
                Players = new[] { "Sirians", "StormGiants", "GreyDwarves", "OrcsOfKor",
                    "Elvallie", "HorseLords", "Selentines", "LordBane" }
                    .Select(clan => new UnityPlayerEntity { ClanName = clan, IsHuman = false,
                        AiDifficulty = AiDifficultyTier.Lord }).ToArray()
            };
        }

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            AudioListener.pause = true;
            AudioListener.volume = 0f;
            // Unload the splash coroutine before it can navigate back to setup.
            yield return null;
            UnityManager.SetNewGameSettings(Settings());
            SceneManager.LoadScene("Illuria");
            deadline = Time.realtimeSinceStartupAsDouble + 7200;
            Debug.Log("Autonomous playtest started: Illuria, eight AI clans, Lord difficulty, seed 1990, two-hour limit.");
        }

        private void Update()
        {
            if (deadline > 0 && Time.realtimeSinceStartupAsDouble >= deadline)
            {
                Debug.Log("Autonomous playtest deadline reached. Exiting normally.");
                deadline = 0;
                Application.Quit();
            }
        }
    }
}
