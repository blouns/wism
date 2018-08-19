using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public class World
    {
        private enum mapObject : int
        {
            None = 0,
            Mountain = 1,
            Meadow = 2,
            Castle = 3
        }

        private static int[,] map =
        {
            { 1, 1, 1, 1, 1 },
            { 1, 2, 2, 2, 1 },
            { 1, 2, 3, 2, 1 },
            { 1, 2, 2, 2, 1 },
            { 1, 1, 1, 1, 1 }
        };
    }
}
