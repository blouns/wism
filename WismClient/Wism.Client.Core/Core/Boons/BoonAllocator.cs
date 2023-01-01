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
        private const float DefaultThronePercent = 0.05f;
        private const float DefaultGoldPercent = 0.05f;
        private readonly List<IBoon> boonLibrary = new List<IBoon>();
        private int[] boonOrder;

        public BoonAllocator()
        {
            // Default percentages must add up to 100%
            this.AlliesPercent = DefaultAlliesPercent;
            this.ArtifactPercent = DefaultArtifactPercent;
            this.ThronePercent = DefaultThronePercent;
            this.GoldPercent = DefaultGoldPercent;
        }

        public float AlliesPercent { get; set; }
        public float ArtifactPercent { get; set; }
        public float ThronePercent { get; set; }
        public float GoldPercent { get; set; }

        /// <summary>
        ///     Allocate boons to all locations that support boons.
        /// </summary>
        /// <remarks>
        ///     Allocations based off percentages defined, but may be scaled up or down
        ///     to fit the number of locations in need of boons.
        /// </remarks>
        public void Allocate(List<Location> worldLocations)
        {
            //ValidateAllocationPercentages();

            var locations = GetLocationsToAllocate(worldLocations);
            this.BuildBoonLibrary(ModFactory.ModPath, locations.Count);
            this.RandomizeBoons();
            this.AllocateInternal(locations);
        }

        /// <summary>
        ///     Filter the locations to those that support boons
        /// </summary>
        /// <param name="worldPath">World path to artifacts</param>
        /// <returns></returns>
        private static List<Location> GetLocationsToAllocate(List<Location> allLocations)
        {
            var locationsToAllocate = allLocations.FindAll(l => l.Kind == "Tomb" || l.Kind == "Ruins");
            return locationsToAllocate;
        }

        /// <summary>
        ///     Validate that the allocations add up to 100%
        /// </summary>
        private void ValidateAllocationPercentages()
        {
            if (this.AlliesPercent + this.ArtifactPercent + this.ThronePercent + this.GoldPercent > 1f)
            {
                throw new InvalidOperationException(string.Format(
                    "Allocations must equal 100%: Allies: {0}%, Artifact: {1}%, Throne: {2}%, Gold: {3}%",
                    this.AlliesPercent * 100f,
                    this.ArtifactPercent * 100f,
                    this.ThronePercent * 100f,
                    this.GoldPercent * 100f));
            }
        }

        /// <summary>
        ///     Randomize the order for boon selection
        /// </summary>
        private void RandomizeBoons()
        {
            var boonPool = new List<IBoon>(this.boonLibrary);
            var random = Game.Current.Random;
            this.boonOrder = new int[this.boonLibrary.Count];
            for (var i = 0; i < this.boonLibrary.Count; i++)
            {
                var index = random.Next(0, boonPool.Count);
                this.boonOrder[i] = this.boonLibrary.IndexOf(boonPool[index]);
                boonPool.RemoveAt(index);
            }
        }

        /// <summary>
        ///     Build the library of boons to allocate
        /// </summary>
        /// <param name="modPath">Mode path</param>
        /// <param name="boonCount">Total boons to create</param>
        private void BuildBoonLibrary(string modPath, int boonCount)
        {
            var artifacts = ModFactory.LoadArtifacts(modPath);
            float artifactActual, alliesActual, throneActual, goldActual;
            this.CalculateActualAllocationPercent(boonCount, artifacts, out artifactActual, out alliesActual,
                out throneActual, out goldActual);

            this.AddArtifacts(artifacts, boonCount, artifactActual);
            this.AddAllies(boonCount, alliesActual);
            this.AddGold(boonCount, goldActual);
            this.AddThrone(boonCount, throneActual);
        }

        /// <summary>
        ///     Calculate needed allocations for non-artifacts based on number of artifacts.
        ///     If too few artifacts then scale up other boons accordingly.
        /// </summary>
        /// <param name="boonCount">Total boons needed</param>
        /// <param name="artifacts">Artifacts available</param>
        /// <param name="artifactActual">Actual % of artifact boons</param>
        /// <param name="alliesActual">Actual % of allies boons</param>
        /// <param name="throneActual">Actual % of throne boons</param>
        /// <param name="goldActual">Actual % of gold boons</param>
        private void CalculateActualAllocationPercent(int boonCount, IList<Artifact> artifacts,
            out float artifactActual, out float alliesActual, out float throneActual, out float goldActual)
        {
            var artifactsPerBoon = artifacts.Count / (float)boonCount;
            artifactActual = artifactsPerBoon > this.ArtifactPercent ? this.ArtifactPercent : artifactsPerBoon;

            if (artifactActual < this.ArtifactPercent)
            {
                var artifactPercentDelta = (this.ArtifactPercent - artifactActual) / artifactActual;
                alliesActual = artifactPercentDelta * this.AlliesPercent + this.AlliesPercent;
                throneActual = artifactPercentDelta * this.ThronePercent + this.ThronePercent;
                goldActual = artifactPercentDelta * this.GoldPercent + this.GoldPercent;
            }
            else
            {
                alliesActual = this.AlliesPercent;
                throneActual = this.ThronePercent;
                goldActual = this.GoldPercent;
                goldActual += 1f - (artifactActual + alliesActual + goldActual); // Remainder goes to gold
            }
        }

        /// <summary>
        ///     Add % of Throne boons
        /// </summary>
        /// <param name="boonCount">Total boons</param>
        /// <param name="throneActual">% of Throne boons desired</param>
        private void AddThrone(int boonCount, float throneActual)
        {
            for (var i = 0; i < boonCount * throneActual; i++)
            {
                this.boonLibrary.Add(new ThroneBoon());
            }
        }

        /// <summary>
        ///     Add % of Gold boons
        /// </summary>
        /// <param name="boonCount">Total boons</param>
        /// <param name="goldActual">% of Gold boons desired</param>
        private void AddGold(int boonCount, float goldActual)
        {
            for (var i = 0; i < boonCount * goldActual; i++)
            {
                this.boonLibrary.Add(new GoldBoon());
            }
        }

        /// <summary>
        ///     Add % of Allies across random set of special armies
        /// </summary>
        /// <param name="boonCount">Total boons</param>
        /// <param name="alliesActual">% of Allies boons desired</param>
        private void AddAllies(int boonCount, float alliesActual)
        {
            var allies = GetSpecialAllies();
            for (var i = 0; i < boonCount * alliesActual; i++)
            {
                var armyInfo = allies[Game.Current.Random.Next(0, allies.Count)];
                this.boonLibrary.Add(new AlliesBoon(armyInfo));
            }
        }

        /// <summary>
        ///     Add % of artifact boons. Scale down if too many.
        /// </summary>
        /// <param name="artifacts">List of Artifacts to add</param>
        /// <param name="artifactActual">% of Artifacts desired</param>
        private void AddArtifacts(IList<Artifact> artifacts, int boonCount, float artifactActual)
        {
            if (artifactActual >= this.ArtifactPercent)
            {
                // If too many artifacts then pick a random set that fits
                var artifactPool = new List<Artifact>(artifacts);
                var artifactFitCount = (int)(boonCount * this.ArtifactPercent);
                var artifactOrder = new int[artifactFitCount];
                for (var i = 0; i < artifactFitCount; i++)
                {
                    var index = Game.Current.Random.Next(0, artifactPool.Count);
                    artifactOrder[i] = artifacts.IndexOf(artifactPool[index]);
                    artifactPool.RemoveAt(index);
                }

                // Add to the library in random order
                for (var i = 0; i < artifactFitCount; i++)
                {
                    var randomArtifact = artifacts[artifactOrder[i]];
                    this.boonLibrary.Add(new ArtifactBoon(randomArtifact));
                }
            }
            else
            {
                // Too few artifacts, so just add all of them
                foreach (var artifact in artifacts)
                {
                    this.boonLibrary.Add(new ArtifactBoon(artifact));
                }
            }
        }

        /// <summary>
        ///     Get a list of the ArmyInfos to include in the AlliesBoon
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
        ///     Allocate the boons across the locations
        /// </summary>
        /// <param name="locations">Locations to allocate boons into</param>
        private void AllocateInternal(IList<Location> locations)
        {
            for (var i = 0; i < locations.Count; i++)
            {
                // Assign a random boon
                locations[i].Boon = this.boonLibrary[this.boonOrder[i]];

                if (locations[i].Boon.IsDefended)
                {
                    locations[i].Monster = this.GetRandomMonster();
                }
            }
        }

        /// <summary>
        ///     Select a random monster
        /// </summary>
        /// <returns>Monster name</returns>
        private string GetRandomMonster()
        {
            var monsters = new[]
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