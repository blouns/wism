using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BranallyGames.Wism.API.Model;
using BranallyGames.Wism.Client;

namespace Wism.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpClient httpClient = new HttpClient();
            var wismProxy = new BranallyGames.Wism.Client.Client("http://localhost:51045/", httpClient);

            Console.WriteLine("Getting all available worlds...");
            var worlds = new List<WorldModel>();
            try
            {
                worlds.AddRange(wismProxy.GetWorldsAsync().Result);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

            if (worlds.Count == 0)
            {
                Console.WriteLine("No worlds found.");
                return;
            }
            else
            {
                foreach (WorldModel world in worlds)
                {
                    Console.WriteLine("World: {0}", world.DisplayName);
                }
            }

            Console.WriteLine("Getting Branally from Etheria...");
            WorldModel etheria = worlds.Find(w => w.ShortName == "Etheria");
            var players = new List<PlayerModel>();
            players.AddRange(wismProxy.GetPlayersForWorldAsync(etheria.Id).Result);
            var branally = players.Find(p => p.ShortName == "Brian");
            
            Console.WriteLine("Found {0} from {1}!", branally.DisplayName, etheria.DisplayName);
            Console.ReadLine();

            return;
        }
    }
}
