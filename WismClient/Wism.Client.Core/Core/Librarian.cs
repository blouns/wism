using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;

namespace Wism.Client.Core
{
    public class Librarian
    {
        // Library knowledge formats
        private readonly string artifactBookKnowledgeHero = "The {0} is owned by a hero!";
        private readonly string artifactBookKnowledgeTile = "The {0} can be found in the vicinity of the {1} city of {2}!";
        private readonly string artifactBookKnowledgeLocation = "The {0} can be found at {1}!";
        private readonly string locationBookKnowledgeUninhabited = "{0} is uninhabited!";
        private readonly string locationBookKnowledgeInhabited = "{0} is inhabited by a {1}!";

        // Sage knowledge formats
        private readonly string artifactSeerKnowledgeIntro = "The {0} can be found...";
        private readonly string artifactSeerKnowledgeHero = "...near {0} with a hero of the {1}";
        private readonly string artifactSeerKnowledgeTile = "...in the vicinity of the {0} city of {1}";
        private readonly string artifactSeerKnowledgeLocation = "...at {0}!";
        private readonly string locationSeerKnowledgeUninhabited = "Nothing evil lives here!";
        private readonly string locationSeerKnowledgeInhabited = "...it is inhabited by a {0}";
        private readonly string locationSeerKnowledgeAllies = "Friendship is possible!";
        private readonly string locationSeerKnowledgeAltar = "Hope the gods will favor you!";
        private readonly string locationSeerKnowledgeArtifactIntro = "An ancient artifact lives there!";
        private readonly string locationSeerKnowledgeArtifact = "The {0}!";
        private readonly string locationSeerKnowledgeGold = "An valuable gold treasure is there!";
        private readonly string locationSeerKnowledgeLibrary = "It is a truly magnificent collection of books!";
        private readonly string locationSeerKnowledgeTemple = "Blessed is he who visits this place!";
        private readonly string locationSeerKnowledgeSage = "Ahhh grasshopper! A place to seek wisdom!";        
        private readonly string locationSeerKnowledgeExplored = "It has already been explored!";

        private List<Location> allLocations;
        private Dictionary<Artifact, object> artifactDictionary;

        /// <summary>
        /// Get the names of all locations in the world
        /// </summary>
        /// <returns>List of names of locations</returns>
        public string[] GetAllLocationNames()
        {
            BuildLocationKnowledge();

            var names = allLocations.ConvertAll<string>(
                new Converter<Location, string>(MapObjectToName));

            return names.ToArray();
        }

        /// <summary>
        /// Gets the names of all artifacts in the world (locations, tiles, heros)
        /// </summary>
        /// <returns>List of names of artifacts</returns>
        public string[] GetAllArtifactNames()
        {
            BuildArtifactKnowledge();

            // Create the list of names
            var allArtifacts = new List<Artifact>(artifactDictionary.Keys);
            var names = allArtifacts.ConvertAll<string>(new Converter<Artifact, string>(MapObjectToName));

            return names.ToArray();
        }

        /// <summary>
        /// Gets a random piece of knowledge from the great libraries
        /// </summary>
        /// <param name="player">Player seeking knowledge</param>
        /// <returns>Knoweldge</returns>
        /// <remarks>
        /// Knowledge consists of information about a location, artifact, 
        /// or other "useful" things. Will not provide knoweldge about
        /// the players owned items.
        /// </remarks>
        public string GetRandomKnowledge(Player player)
        {
            string knowledge;

            BuildLocationKnowledge();
            BuildArtifactKnowledge();
            var otherKnowledge = BuildOtherKnowledge();
            var locationsWithBoons = allLocations.FindAll(loc => loc.HasBoon());
            var allArtifacts = new List<Artifact>(artifactDictionary.Keys);

            int totalKnowledge = allArtifacts.Count + locationsWithBoons.Count + otherKnowledge.Length;
            int index = Game.Current.Random.Next(totalKnowledge);
            if (index < allArtifacts.Count)
            {
                // Give Artifact knowledge
                knowledge = GetArtifactLibraryKnowledge(
                    allArtifacts[index],                        // Artifact
                    artifactDictionary[allArtifacts[index]]);   // Location, hero, or tile
            }
            else if (index < (allArtifacts.Count + locationsWithBoons.Count))
            {
                // Give Location knowledge
                int locIndex = index - allArtifacts.Count;
                knowledge = GetLocationLibraryKnowledge(locationsWithBoons[locIndex]);
            }
            else
            {
                // Give other knowledge
                int otherIndex = (index - (allArtifacts.Count + locationsWithBoons.Count));
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
                BuildArtifactKnowledge();
            }

            var knowledge = new string[2];
            knowledge[0] = String.Format(artifactSeerKnowledgeIntro, artifact.DisplayName);

            var place = this.artifactDictionary[artifact];
            if (place is Location)
            {
                knowledge[1] = String.Format(artifactSeerKnowledgeLocation, ((Location)place).DisplayName);
            }
            else if (place is Hero)
            {
                Hero hero = (Hero)place;
                City city = GetClosestCity(hero.Tile);

                knowledge[1] = String.Format(artifactSeerKnowledgeHero, city, hero.Clan);
            }
            else if (place is Tile)
            {
                Tile tile = (Tile)place;
                City city = GetClosestCity(tile);

                knowledge[1] = String.Format(artifactSeerKnowledgeTile, city.Clan, city.DisplayName);
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
                    knowledge.Add(locationSeerKnowledgeLibrary);
                    break;
                case "Ruins":
                case "Tomb":
                    AddRuinsKnowledge(location, knowledge);
                    break;
                case "Sage":
                    knowledge.Add(locationSeerKnowledgeSage);
                    break;
                case "Temple":
                    knowledge.Add(locationSeerKnowledgeTemple);
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
                knowledge.Add(locationSeerKnowledgeExplored);
            }
            else
            {
                if (location.HasMonster())
                {
                    knowledge.Add(String.Format(locationSeerKnowledgeInhabited, location.Monster));
                }
                else
                {
                    knowledge.Add(locationSeerKnowledgeUninhabited);
                }

                if (location.HasBoon())
                {
                    AddBoonKnowledge(location, knowledge);
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
                    knowledge.Add(locationSeerKnowledgeAllies);
                }
                else if (location.Boon is AltarBoon)
                {
                    knowledge.Add(locationSeerKnowledgeAltar);
                }
                else if (location.Boon is ArtifactBoon)
                {
                    var artifact = ((ArtifactBoon)location.Boon).Artifact;
                    knowledge.Add(locationSeerKnowledgeArtifactIntro);
                    knowledge.Add(String.Format(locationSeerKnowledgeArtifact, artifact.DisplayName));
                }
                else if (location.Boon is GoldBoon)
                {
                    knowledge.Add(locationSeerKnowledgeGold);
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
                knowledge = String.Format(artifactBookKnowledgeLocation,
                    artifact.DisplayName,
                    ((Location)container).DisplayName);
            }
            else if (container is Hero)
            {
                knowledge = String.Format(artifactBookKnowledgeHero,
                    artifact.DisplayName,
                    ((Hero)container).DisplayName);
            }
            else if (container is Tile)
            {
                City city = GetClosestCity((Tile)container);
                knowledge = String.Format(artifactBookKnowledgeTile,
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
            BuildLocationKnowledge();

            return this.allLocations.Find(loc => loc.ShortName == shortName);
        }

        public Location GetLocation(int index)
        {
            BuildLocationKnowledge();

            return this.allLocations[index];
        }

        public Artifact GetArtifact(string shortName, bool buildKnowledge = true)
        {
            if (buildKnowledge)
            {
                BuildArtifactKnowledge();
            }

            var allArtifacts = new List<Artifact>(artifactDictionary.Keys);
            return allArtifacts.Find(art => art.ShortName == shortName);
        }

        public Artifact GetArtifact(int index, bool buildKnowledge = true)
        {
            if (buildKnowledge)
            {
                BuildArtifactKnowledge();
            }

            var allArtifacts = new List<Artifact>(artifactDictionary.Keys);
            return allArtifacts[index];
        }

        public object GetArtifactPlace(string shortName, bool buildKnowledge = true)
        {
            if (buildKnowledge)
            {
                BuildArtifactKnowledge();
            }

            var artifact = GetArtifact(shortName, false);
            return this.artifactDictionary[artifact];
        }

        private static string MapObjectToName(MapObject mapObject)
        {
            return mapObject.DisplayName;
        }

        private List<Tile> GetTilesWithArtifacts()
        {
            List<Tile> tiles = new List<Tile>();
            var map = World.Current.Map;
            for (int i = 0; i <= map.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= map.GetUpperBound(1); j++)
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
            var artifactTiles = GetTilesWithArtifacts();

            // Join all into master dictionary of artifacts
            this.artifactDictionary = MergeArtifactObjects(artifactHeros, artifactLocations, artifactTiles);
        }

        private Dictionary<Artifact, object> MergeArtifactObjects(
            List<Army> artifactHeros, List<Location> artifactLocations, List<Tile> artifactTiles)
        {
            var artifactDictionary = new Dictionary<Artifact, object>();

            // Add hero items
            foreach (var army in artifactHeros)
            {
                Hero hero = (Hero)army;
                foreach (var item in hero.Items)
                {
                    artifactDictionary.Add((Artifact)item, hero);
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
                    artifactDictionary.Add((Artifact)item, tile);
                }
            }

            return artifactDictionary;
        }

        private void BuildLocationKnowledge()
        {
            if (allLocations == null)
            {
                allLocations = World.Current.GetLocations();
            }
        }

        private string[] BuildOtherKnowledge()
        {
            // TODO: Pull from a mod file
            var otherKnowledge = new string[]
            {
                "Never give a {0} an even break!",
                "Lord Lowenbrau will return!",
                "Branally the Fist is the great creator!"
            };

            return otherKnowledge;
        }

        /// <summary>
        /// Basic formatting for English nuances
        /// </summary>
        /// <param name="knowledgeFormat">Format string</param>
        /// <param name="clanName">Clan name</param>
        /// <returns></returns>
        private static string FormatOtherKnowledge(string knowledgeFormat, string clanName)
        {
            string name = clanName;
            if (clanName.StartsWith("The "))
            {
                name = name.Substring(4);
            }
            if (clanName.EndsWith("s"))
            {
                name = name.Remove(name.LastIndexOf("s"));
            }

            return String.Format(knowledgeFormat, name);
        }

        /// <summary>
        /// Calculate the distance to the closest city from this tile
        /// </summary>
        /// <param name="tile">Tile to use as the source</param>
        /// <returns>Closest city to tile</returns>
        private City GetClosestCity(Tile tile)
        {
            City closestCity = null;
            double closestDistance = Double.MaxValue;
            foreach (var city in World.Current.GetCities())
            {
                double distance = Distance(
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
                knowledge = String.Format(locationBookKnowledgeInhabited, location.DisplayName, location.Monster);
            }
            else
            {
                knowledge = String.Format(locationBookKnowledgeUninhabited, location.DisplayName);
            }

            return knowledge;
        }

        private static double Distance(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        }

    }
}
