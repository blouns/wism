namespace Wism.Client.Model
{
    public abstract class MapObjectBase
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public bool TryMove(int newX, int newY)
        {
            X = newX;
            Y = newY;

            return true;
        }
    }
}
