using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using Wism.Client.Data.Entities;
using Wism.Client.Modules;

namespace Assets.Scripts.UnityGame.Factories
{
    public class UnityPlayerFactory
    {
        public PlayerEntity[] CreatePlayers(UnityPlayerEntity[] unityPlayers)
        {
            if (unityPlayers is null)
            {
                throw new ArgumentNullException(nameof(unityPlayers));
            }
            if (unityPlayers.Length == 0)
            {
                throw new ArgumentException("Must have at least one player", nameof(unityPlayers));
            }
            PlayerEntity[] playerEntities = new PlayerEntity[unityPlayers.Length];
            for (int i = 0; i < unityPlayers.Length; i++)
            {
                playerEntities[i] = new PlayerEntity();

                // Verify the clan exists
                if (ModFactory.FindClanInfo(unityPlayers[i].ClanName) == null)
                {
                    throw new ArgumentException("Clan not found: " + playerEntities[i].ClanShortName);
                }
                playerEntities[i].ClanShortName = unityPlayers[i].ClanName;
                playerEntities[i].IsHuman = unityPlayers[i].IsHuman;
            }

            return playerEntities;
        }
    }
}
