using System;
using System.Collections.Generic;

namespace Wism.Client.Common
{
    public static class WismHeroNamer
    {
        public static string GetRandomHeroName()
        {
            List<string> names = new List<string>()
            {
                "Aethelstan",
                "Angwyshaunce",
                "Alex the Black",
                "Aelfnoth",
                "Aelfwine",
                "Baron Roger",
                "Baroness Lucy",
                "Brandegoris",
                "Brus avec Petie",
                "Byrtnoth",
                "Clarivaunce",
                "Countess Phil",
                "Duke Tintagel",
                "Eadric",
                "Ealhelm",
                "Earl Steven",
                "Ecglaf",
                "Friar Tuck",
                "Joan d'Arc",
                "Lady Donna",
                "Lady Janeen",
                "Leofsunu",
                "Lord Graeme",
                "Manx Andrew",
                "Robin Hood",
                "Rowan the Red",
                "Sir Bors",
                "Sir Kay",
                "Sir Falkner",
                "Sir Gregor",
                "Sir Lamrock",
                "Sir Lancelot",
                "Sir Palomides",
                "Sir Percivale",
                "Sir Tristram",
                "Viscount Vick",
                "Will Scarlet",
                "Wulfstan"
            };

            Random random = new Random(100);

            return names[random.Next(names.Count)];
        }
    }
}
