using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class BoonAllocator
    {
        private const float DefaultAlliesPercent = 0.3f;
        private const float DefaultArtifactPercent = 0.6f;
        private const float DefaultAltarPercent = 0.05f;
        private const float DefaultGoldPercent = 0.05f;
        
        List<IBoon> boonLibrary = new List<IBoon>();
        int[] boonOrder;

        public float AlliesPercent { get; set; }
        public float ArtifactPercent { get; set; }
        public float AltarPercent { get; set; }
        public float GoldPercent { get; set; }

        public BoonAllocator()
        {
            // Default percentages must add up to 100%
            AlliesPercent = DefaultAlliesPercent;
            ArtifactPercent = DefaultArtifactPercent;
            AltarPercent = DefaultAltarPercent;
            GoldPercent = DefaultGoldPercent;            
        }

        /// <summary>
        /// Allocate boons to all locations that support boons.
        /// </summary>
        /// <remarks>
        /// Allocations based off percentages defined, but may be scaled up or down 
        /// to fit the number of locations in need of boons.
        /// </remarks>
        public void Allocate(List<Location> worldLocations)
        {
            ValidateAllocationPercentages();

            List<Location> locations = GetLocationsToAllocate(worldLocations);
            BuildBoonLibrary(ModFactory.ModPath, locations.Count);
            RandomizeBoons();
            AllocateInternal(locations);
        }

        /// <summary>
        /// Filter the locations to those that support boons
        /// </summary>
        /// <param name="worldPath">World path to artifacts</param>
        /// <returns></returns>
        private static List<Location> GetLocationsToAllocate(List<Location> allLocations)
        {
            var locationsToAllocate = allLocations.FindAll(l => l.Kind == "Tomb" || l.Kind == "Ruins");
            return locationsToAllocate;
        }

        /// <summary>
        /// Validate that the allocations add up to 100%
        /// </summary>
        private void ValidateAllocationPercentages()
        {
            if ((AlliesPercent + ArtifactPercent + AltarPercent + GoldPercent) > 1f)
            {
                throw new InvalidOperationException(String.Format(
                    "Allocations must equal 100%: Allies: {0}%, Artifact: {1}%, Altar: {2}%, Gold: {3}%",
                    AltarPercent * 100f,
                    ArtifactPercent * 100f,
                    AltarPercent * 100f,
                    GoldPercent * 100f));
            }
        }

        /// <summary>
        /// Randomize the order for boon selection
        /// </summary>
        private void RandomizeBoons()
        {
            var boonPool = new List<IBoon>(boonLibrary);            
            var random = Game.Current.Random;           
            boonOrder = new int[boonLibrary.Count];
            for (int i = 0; i < boonLibrary.Count; i++)
            {
                int index = random.Next(0, boonPool.Count);
                boonOrder[i] = boonLibrary.IndexOf(boonPool[index]);
                boonPool.RemoveAt(index);
            }
        }

        /// <summary>
        /// Build the library of boons to allocate
        /// </summary>
        /// <param name="modPath">Mode path</param>
        /// <param name="boonCount">Total boons to create</param>
        private void BuildBoonLibrary(string modPath, int boonCount)
        {
            var artifacts = ModFactory.LoadArtifacts(modPath);            
            float artifactActual, alliesActual, altarActual, goldActual;
            CalculateActualAllocationPercent(boonCount, artifacts, out artifactActual, out alliesActual, out altarActual, out goldActual);

            AddArtifacts(artifacts, boonCount, artifactActual);
            AddAllies(boonCount, alliesActual);
            AddGold(boonCount, goldActual);
            AddAltar(boonCount, altarActual);
        }

        /// <summary>
        /// Calculate needed allocations for non-artifacts based on number of artifacts.
        /// If too few artifacts then scale up other boons accordingly.
        /// </summary>
        /// <param name="boonCount">Total boons needed</param>
        /// <param name="artifacts">Artifacts available</param>
        /// <param name="artifactActual">Actual % of artifact boons</param>
        /// <param name="alliesActual">Actual % of allies boons</param>
        /// <param name="altarActual">Actual % of altar boons</param>
        /// <param name="goldActual">Actual % of gold boons</param>
        private void CalculateActualAllocationPercent(int boonCount, IList<Artifact> artifacts, out float artifactActual, out float alliesActual, out float altarActual, out float goldActual)
        {
            float artifactsPerBoon = (artifacts.Count / boonCount);
            artifactActual = (artifactsPerBoon > ArtifactPercent) ? ArtifactPercent : artifactsPerBoon;
            alliesActual = (artifactActual < ArtifactPercent) ? AlliesPercent / (1 - artifactActual) : AlliesPercent;
            altarActual = (artifactActual < ArtifactPercent) ? AltarPercent / (1 - artifactActual) : AltarPercent;
            goldActual = (artifactActual < ArtifactPercent) ? GoldPercent / (1 - artifactActual) : GoldPercent;
        }

        /// <summary>
        /// Add % of Altar boons
        /// </summary>
        /// <param name="boonCount">Total boons</param>
        /// <param name="altarActual">% of Altar boons desired</param>
        private void AddAltar(int boonCount, float altarActual)
        {
            for (int i = 0; i < boonCount * altarActual; i++)
            {
                boonLibrary.Add(new AltarBoon());
            }
        }

        /// <summary>
        /// Add % of Gold boons
        /// </summary>
        /// <param name="boonCount">Total boons</param>
        /// <param name="goldActual">% of Gold boons desired</param>
        private void AddGold(int boonCount, float goldActual)
        {
            for (int i = 0; i < boonCount * goldActual; i++)
            {
                boonLibrary.Add(new GoldBoon());
            }
        }

        /// <summary>
        /// Add % of Allies across random set of special armies
        /// </summary>
        /// <param name="boonCount">Total boons</param>
        /// <param name="alliesActual">% of Allies boons desired</param>
        private void AddAllies(int boonCount, float alliesActual)
        {
            List<ArmyInfo> allies = GetSpecialAllies();
            for (int i = 0; i < boonCount * alliesActual; i++)
            {
                var armyInfo = allies[Game.Current.Random.Next(0, allies.Count)];
                boonLibrary.Add(new AlliesBoon(armyInfo));
            }
        }

        /// <summary>
        /// Add % of artifact boons. Scale down if too many.
        /// </summary>
        /// <param name="artifacts">List of Artifacts to add</param>
        /// <param name="artifactActual">% of Artifacts desired</param>
        private void AddArtifacts(IList<Artifact> artifacts, int boonCount, float artifactActual)
        {
            if (artifactActual >= ArtifactPercent)
            {
                // If too many artifacts then pick a random set that fits
                var artifactPool = new List<Artifact>(artifacts);
                int artifactFitCount = (int)(boonCount * ArtifactPercent);
                var artifactOrder = new int[artifactFitCount];
                for (int i = 0; i < artifactFitCount; i++)
                {
                    int index = Game.Current.Random.Next(0, artifactPool.Count);
                    artifactOrder[i] = artifacts.IndexOf(artifactPool[index]);
                    artifactPool.RemoveAt(index);
                }

                // Add to the library in random order
                for (int i = 0; i < artifactFitCount; i++)
                {
                    var randomArtifact = artifacts[artifactOrder[i]];
                    boonLibrary.Add(new ArtifactBoon(randomArtifact));
                }
            }
            else
            {
                // Too few artifacts, so just add all of them
                foreach (var artifact in artifacts)
                {
                    boonLibrary.Add(new ArtifactBoon(artifact));
                }
            }
        }

        /// <summary>
        /// Get a list of the ArmyInfos to include in the AlliesBoon
        /// </summary>
        /// <returns>Ally list</returns>
        private static List<ArmyInfo> GetSpecialAllies()
        {
            var armyInfos = ModFactory.LoadArmyInfos(ModFactory.ModPath);
            var allies = new List<ArmyInfo>();
            foreach (var armyInfo in armyInfos)
            {
                if (armyInfo.IsSpecial)
                {
                    allies.Add(armyInfo);
                }
            }

            return allies;
        }

        /// <summary>
        /// Allocate the boons across the locations
        /// </summary>
        /// <param name="locations">Locations to allocate boons into</param>
        private void AllocateInternal(IList<Location> locations)
        {
            for (int i = 0; i < locations.Count; i++)
            {
                // Assign a random boon
                locations[i].Boon = boonLibrary[boonOrder[i]];

                if (locations[i].Boon.IsDefended)
                {
                    locations[i].Monster = GetRandomMonster();
                }
            }
        }

        /// <summary>
        /// Select a random monster
        /// </summary>
        /// <returns>Monster name</returns>
        private string GetRandomMonster()
        {
            var monsters = new string[]
            {
                "Ghost",
                "Dragon",
                "Troll",
                "Demon"
            };

            return monsters[Game.Current.Random.Next(0, monsters.Length)];
        }
    }
}
