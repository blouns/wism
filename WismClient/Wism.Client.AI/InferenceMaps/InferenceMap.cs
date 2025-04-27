namespace Wism.Client.AI.InferenceMaps
{
    public class InferenceMap
    {
        private const int DefaultForwardPropagateCount = 10;

        public int[,] Map { get; set; }

        private InferenceMap() { }

        public InferenceMap(int m, int n)
        {
            Map = new int[m, n];
            Map.Initialize();            
        }

        /// <summary>
        /// Forward propagate influence of each map location to its neighbors based on its value.
        /// </summary>
        /// <param name="count"></param>
        public void ForwardPropogate(int count = DefaultForwardPropagateCount)
        {
            int xMax = Map.GetUpperBound(0);
            int yMax = Map.GetUpperBound(1);

            int[,] mapBuffer = CopyMap(Map, xMax, yMax);

            for (int i = 0; i < count; i++)
            {
                // Increment positive/negative influence
                for (int y = 0; y < yMax; y++)
                {
                    for (int x = 0; x < xMax; x++)
                    {
                        InfluenceNeighbors(mapBuffer, Map[y, x], y, x, xMax, yMax);
                    }
                }

                Map = CopyMap(mapBuffer, xMax, yMax);
            }
        }

        /// <summary>
        /// Influence the value of each of the 8 2D-array neighbors based on value.
        /// </summary>
        /// <param name="mapBuffer">Map to set the new values for neighbors</param>
        /// <param name="value">Current position value from which to influence</param>
        /// <param name="x">X of current value</param>
        /// <param name="y">Y of current value</param>
        /// <param name="xMax">X max</param>
        /// <param name="yMax">Y max</param>
        /// <remarks>
        /// In a 2D-grid, let v be value of the current position. Numbers 1 - 8 are
        /// the neighbors of v. Influence each neighbor n.   
        /// +---+---+---+
        /// | 1 | 2 | 3 |
        /// +---+---+---+
        /// | 4 | v | 5 |
        /// +---+---+---+
        /// | 6 | 7 | 8 |
        /// +---+---+---+
        /// </remarks>
        private void InfluenceNeighbors(int[,] mapBuffer, int value, int x, int y, int xMax, int yMax)
        {
            InfluenceNeighbor(mapBuffer, value, x - 1, y - 1, xMax, yMax); // 1
            InfluenceNeighbor(mapBuffer, value, x + 0, y - 1, xMax, yMax); // 2
            InfluenceNeighbor(mapBuffer, value, x + 1, y - 1, xMax, yMax); // 3
            InfluenceNeighbor(mapBuffer, value, x - 1, y + 0, xMax, yMax); // 4
            InfluenceNeighbor(mapBuffer, value, x + 1, y + 0, xMax, yMax); // 5
            InfluenceNeighbor(mapBuffer, value, x - 1, y + 1, xMax, yMax); // 6
            InfluenceNeighbor(mapBuffer, value, x + 0, y + 1, xMax, yMax); // 7
            InfluenceNeighbor(mapBuffer, value, x + 1, y + 1, xMax, yMax); // 8
        }

        /// <summary>
        /// Influence a value based on given value
        /// </summary>
        /// <param name="mapBuffer">Map to set the new values for neighbors</param>
        /// <param name="value">Current position value from which to influence</param>
        /// <param name="x">X of current value</param>
        /// <param name="y">Y of current value</param>
        /// <param name="xMax">X max</param>
        /// <param name="yMax">Y max</param>
        private void InfluenceNeighbor(int[,] mapBuffer, int value, int x, int y, int xMax, int yMax)
        {
            if (x < 0 || x > xMax)
                return;
            if (y < 0 || y > yMax)
                return;

            mapBuffer[x, y] = value > 0 ? value + 1 : value - 1;
        }

        private int[,] CopyMap(int[,] map, int xMax, int yMax)
        {
            int[,] mapBuffer = new int[xMax, yMax];
            for (int i = 0; i < yMax; i++)
            {
                for (int j = 0; j < xMax; j++)
                {
                    mapBuffer[i, j] = Map[i, j];
                }
            }

            return mapBuffer;
        }
    }
}
