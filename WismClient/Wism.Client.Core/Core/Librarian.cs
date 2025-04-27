using System;
using System.Collections.Generic;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;

namespace Wism.Client.Core
{
    public class Librarian
    {
        // Library knowledge formats
        private readonly string artifactBookKnowledgeHero = "The {0} is owned by a hero!";
        private readonly string artifactBookKnowledgeLocation = "The {0} can be found at {1}!";

        private readonly string artifactBookKnowledgeTile =
            "The {0} can be found in the vicinity of the {1} city of {2}!";

        private readonly string artifactSeerKnowledgeHero = "...near {0} with a hero of the {1}";

        // Sage knowledge formats
        private readonly string artifactSeerKnowledgeIntro = "The {0} can be found...";
        private readonly string artifactSeerKnowledgeLocation = "...at {0}!";
        private readonly string artifactSeerKnowledgeTile = "...in the vicinity of the {0} city of {1}";
        private readonly string locationBookKnowledgeInhabited = "{0} is inhabited by a {1}!";
        private readonly string locationBookKnowledgeUninhabited = "{0} is uninhabited!";
        private readonly string locationSeerKnowledgeAllies = "Friendship is possible!";
        private readonly string locationSeerKnowledgeArtifact = "The {0}!";
        private readonly string locationSeerKnowledgeArtifactIntro = "An ancient artifact lives there!";
        private readonly string locationSeerKnowledgeExplored = "It has already been explored!";
        private readonly string locationSeerKnowledgeGold = "An valuable gold treasure is there!";
        private readonly string locationSeerKnowledgeInhabited = "...it is inhabited by a {0}";
        private readonly string locationSeerKnowledgeLibrary = "It is a truly magnificent collection of books!";
        private readonly string locationSeerKnowledgeSage = "Ahhh grasshopper! A place to seek wisdom!";
        private readonly string locationSeerKnowledgeTemple = "Blessed is he who visits this place!";
        private readonly string locationSeerKnowledgeThrone = "Hope the gods will favor you!";
        private readonly string locationSeerKnowledgeUninhabited = "Nothing evil lives here!";

        private List<Location> allLocations;
        private Dictionary<Artifact, object> artifactDictionary;

        /// <summary>
        ///     Get the names of all locations in the world
        /// </summary>
        /// <returns>List of names of locations</returns>
        public string[] GetAllLocationNames()
        {
            this.BuildLocationKnowledge();

            var names = this.allLocations.ConvertAll(
                MapObjectToName);

            return names.ToArray();
        }

        /// <summary>
        ///     Gets the names of all artifacts in the world (locations, tiles, heros)
        /// </summary>
        /// <returns>List of names of artifacts</returns>
        public string[] GetAllArtifactNames()
        {
            this.BuildArtifactKnowledge();

            // Create the list of names
            var allArtifacts = new List<Artifact>(this.artifactDictionary.Keys);
            var names = allArtifacts.ConvertAll(MapObjectToName);

            return names.ToArray();
        }

        /// <summary>
        ///     Gets a random piece of knowledge from the great libraries
        /// </summary>
        /// <param name="player">Player seeking knowledge</param>
        /// <returns>Knoweldge</returns>
        /// <remarks>
        ///     Knowledge consists of information about a location, artifact,
        ///     or other "useful" things. Will not provide knoweldge about
        ///     the players owned items.
        /// </remarks>
        public string GetRandomKnowledge(Player player)
        {
            string knowledge;

            this.BuildLocationKnowledge();
            this.BuildArtifactKnowledge();
            var otherKnowledge = this.BuildOtherKnowledge();
            var locationsWithBoons = this.allLocations.FindAll(loc => loc.HasBoon());
            var allArtifacts = new List<Artifact>(this.artifactDictionary.Keys);

            var totalKnowledge = allArtifacts.Count + locationsWithBoons.Count + otherKnowledge.Length;
            var index = Game.Current.Random.Next(totalKnowledge);
            if (index < allArtifacts.Count)
            {
                // Give Artifact knowledge
                knowledge = this.GetArtifactLibraryKnowledge(
                    allArtifacts[index], // Artifact
                    this.artifactDictionary[allArtifacts[index]]); // Location, hero, or tile
            }
            else if (index < allArtifacts.Count + locationsWithBoons.Count)
            {
                // Give Location knowledge
                var locIndex = index - allArtifacts.Count;
                knowledge = this.GetLocationLibraryKnowledge(locationsWithBoons[locIndex]);
            }
            else
            {
                // Give other knowledge
                var otherIndex = index - (allArtifacts.Count + locationsWithBoons.Count);
                knowledge = FormatOtherKnowledge(otherKnowledge[otherIndex], player.Clan.DisplayName);
            }

            return knowledge;
        }

        public string[] GetSageArtifactKnowledge(Artifact artifact, bool buildKnowledge = true)
        {
            if (artifact is null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            if (buildKnowledge)
            {
                this.BuildArtifactKnowledge();
            }

            var knowledge = new string[2];
            knowledge[0] = string.Format(this.artifactSeerKnowledgeIntro, artifact.DisplayName);

            var place = this.artifactDictionary[artifact];
            if (place is Location)
            {
                knowledge[1] = string.Format(this.artifactSeerKnowledgeLocation, ((Location)place).DisplayName);
            }
            else if (place is Hero)
            {
                var hero = (Hero)place;
                var city = this.GetClosestCity(hero.Tile);

                knowledge[1] = string.Format(this.artifactSeerKnowledgeHero, city, hero.Clan);
            }
            else if (place is Tile)
            {
                var tile = (Tile)place;
                var city = this.GetClosestCity(tile);

                knowledge[1] = string.Format(this.artifactSeerKnowledgeTile, city.Clan, city.DisplayName);
            }
            else
            {
                knowledge[1] = "...an undisclosed location!";
            }

            return knowledge;
        }

        public string[] GetSageLocationKnowledge(Location location)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var knowledge = new List<string>();
            switch (location.Kind)
            {
                case "Library":
                    knowledge.Add(this.locationSeerKnowledgeLibrary);
                    break;
                case "Ruins":
                case "Tomb":
                    this.AddRuinsKnowledge(location, knowledge);
                    break;
                case "Sage":
                    knowledge.Add(this.locationSeerKnowledgeSage);
                    break;
                case "Temple":
                    knowledge.Add(this.locationSeerKnowledgeTemple);
                    break;
                default:
                    knowledge.Add("Its contents are a mystery even to me!");
                    break;
            }

            return knowledge.ToArray();
        }

        private void AddRuinsKnowledge(Location location, List<string> knowledge)
        {
            if (location.Searched)
            {
                knowledge.Add(this.locationSeerKnowledgeExplored);
            }
            else
            {
                if (location.HasMonster())
                {
                    knowledge.Add(string.Format(this.locationSeerKnowledgeInhabited, location.Monster));
                }
                else
                {
                    knowledge.Add(this.locationSeerKnowledgeUninhabited);
                }

                if (location.HasBoon())
                {
                    this.AddBoonKnowledge(location, knowledge);
                }
                else
                {
                    knowledge.Add("There is nothing of interest here!");
                }
            }
        }

        private void AddBoonKnowledge(Location location, List<string> knowledge)
        {
            // TODO: Make this mod-able (knowledge lives in JSON)
            if (location.HasBoon())
            {
                if (location.Boon is AlliesBoon)
                {
                    knowledge.Add(this.locationSeerKnowledgeAllies);
                }
                else if (location.Boon is ThroneBoon)
                {
                    knowledge.Add(this.locationSeerKnowledgeThrone);
                }
                else if (location.Boon is ArtifactBoon)
                {
                    var artifact = ((ArtifactBoon)location.Boon).Artifact;
                    knowledge.Add(this.locationSeerKnowledgeArtifactIntro);
                    knowledge.Add(string.Format(this.locationSeerKnowledgeArtifact, artifact.DisplayName));
                }
                else if (location.Boon is GoldBoon)
                {
                    knowledge.Add(this.locationSeerKnowledgeGold);
                }
                else
                {
                    knowledge.Add("Its contents are a mystery to me!");
                }
            }
        }

        private string GetArtifactLibraryKnowledge(Artifact artifact, object container)
        {
            string knowledge;

            if (container is Location)
            {
                knowledge = string.Format(this.artifactBookKnowledgeLocation,
                    artifact.DisplayName,
                    ((Location)container).DisplayName);
            }
            else if (container is Hero)
            {
                knowledge = string.Format(this.artifactBookKnowledgeHero,
                    artifact.DisplayName,
                    ((Hero)container).DisplayName);
            }
            else if (container is Tile)
            {
                var city = this.GetClosestCity((Tile)container);
                knowledge = string.Format(this.artifactBookKnowledgeTile,
                    artifact.DisplayName,
                    city.Clan.DisplayName,
                    city.DisplayName);
            }
            else
            {
                knowledge = $"The {artifact.DisplayName} has gone missing!";
            }

            return knowledge;
        }

        public Location GetLocation(string shortName)
        {
            this.BuildLocationKnowledge();

            return this.allLocations.Find(loc => loc.ShortName == shortName);
        }

        public Location GetLocation(int index)
        {
            this.BuildLocationKnowledge();

            return this.allLocations[index];
        }

        public Artifact GetArtifact(string shortName, bool buildKnowledge = true)
        {
            if (buildKnowledge)
            {
                this.BuildArtifactKnowledge();
            }

            var allArtifacts = new List<Artifact>(this.artifactDictionary.Keys);
            return allArtifacts.Find(art => art.ShortName == shortName);
        }

        public Artifact GetArtifact(int index, bool buildKnowledge = true)
        {
            if (buildKnowledge)
            {
                this.BuildArtifactKnowledge();
            }

            var allArtifacts = new List<Artifact>(this.artifactDictionary.Keys);
            return allArtifacts[index];
        }

        public object GetArtifactPlace(string shortName, bool buildKnowledge = true)
        {
            if (buildKnowledge)
            {
                this.BuildArtifactKnowledge();
            }

            var artifact = this.GetArtifact(shortName, false);
            return this.artifactDictionary[artifact];
        }

        private static string MapObjectToName(MapObject mapObject)
        {
            return mapObject.DisplayName;
        }

        private List<Tile> GetTilesWithArtifacts()
        {
            var tiles = new List<Tile>();
            var map = World.Current.Map;
            for (var i = 0; i <= map.GetUpperBound(0); i++)
            {
                for (var j = 0; j <= map.GetUpperBound(1); j++)
                {
                    if (map[i, j].HasItems())
                    {
                        tiles.Add(map[i, j]);
                    }
                }
            }

            return tiles;
        }

        private void BuildArtifactKnowledge()
        {
            // TODO: Consider moving to a database for performance on larger worlds.
            //       This operation is rare, so it is not essential for game perf.
            //       However, it the knowledge must be rebuilt each time as objects
            //       will move throughout the game.

            // Find all artifacts in the world
            var artifactLocations = World.Current.GetLocations()
                .FindAll(a => a.Boon is ArtifactBoon);
            var artifactHeros = Game.Current.GetAllArmies()
                .FindAll(a => a is Hero && ((Hero)a).HasItems());
            var artifactTiles = this.GetTilesWithArtifacts();

            // Join all into master dictionary of artifacts
            this.artifactDictionary = this.MergeArtifactObjects(artifactHeros, artifactLocations, artifactTiles);
        }

        private Dictionary<Artifact, object> MergeArtifactObjects(
            List<Army> artifactHeros, List<Location> artifactLocations, List<Tile> artifactTiles)
        {
            var artifactDictionary = new Dictionary<Artifact, object>();

            // Add hero items
            foreach (var army in artifactHeros)
            {
                var hero = (Hero)army;
                foreach (var item in hero.Items)
                {
                    artifactDictionary.Add(item, hero);
                }
            }

            // Add location items
            foreach (var location in artifactLocations)
            {
                artifactDictionary.Add(((ArtifactBoon)location.Boon).Artifact, location);
            }

            // Add world items
            foreach (var tile in artifactTiles)
            {
                foreach (var item in tile.Items)
                {
                    artifactDictionary.Add(item, tile);
                }
            }

            return artifactDictionary;
        }

        private void BuildLocationKnowledge()
        {
            if (this.allLocations == null)
            {
                this.allLocations = World.Current.GetLocations();
            }
        }

        private string[] BuildOtherKnowledge()
        {
            // TODO: Pull from a mod file
            var otherKnowledge = new[]
            {
                "Never give a {0} an even break!",
                "Lord Lowenbrau will return!",
                "Branally the Fist is the great creator!"
            };

            return otherKnowledge;
        }

        /// <summary>
        ///     Basic formatting for English nuances
        /// </summary>
        /// <param name="knowledgeFormat">Format string</param>
        /// <param name="clanName">Clan name</param>
        /// <returns></returns>
        private static string FormatOtherKnowledge(string knowledgeFormat, string clanName)
        {
            var name = clanName;
            if (clanName.StartsWith("The "))
            {
                name = name.Substring(4);
            }

            if (clanName.EndsWith("s"))
            {
                name = name.Remove(name.LastIndexOf("s"));
            }

            return string.Format(knowledgeFormat, name);
        }

        /// <summary>
        ///     Calculate the distance to the closest city from this tile
        /// </summary>
        /// <param name="tile">Tile to use as the source</param>
        /// <returns>Closest city to tile</returns>
        private City GetClosestCity(Tile tile)
        {
            City closestCity = null;
            var closestDistance = double.MaxValue;
            foreach (var city in World.Current.GetCities())
            {
                var distance = Distance(
                    tile.X, tile.Y,
                    city.Tile.X, city.Tile.Y);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCity = city;
                }
            }

            return closestCity;
        }

        private string GetLocationLibraryKnowledge(Location location)
        {
            string knowledge;

            if (location.HasMonster())
            {
                knowledge = string.Format(this.locationBookKnowledgeInhabited, location.DisplayName, location.Monster);
            }
            else
            {
                knowledge = string.Format(this.locationBookKnowledgeUninhabited, location.DisplayName);
            }

            return knowledge;
        }

        private static double Distance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }
    }
}