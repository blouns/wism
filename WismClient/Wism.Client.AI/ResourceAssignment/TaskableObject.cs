using System;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.ResourceAssignment
{
    public class TaskableObject
    {
        public MapObject MapObject { get; set; }

        public TaskableObject(MapObject mapObject)
        {
            MapObject = mapObject ?? throw new ArgumentNullException(nameof(mapObject));
        }            

        public void Assign(PossibleAssignment assignment)
        {
            throw new NotImplementedException();
        }

        public bool IsTaskSuitable(Task task)
        {
            return false;
        }
    }
}
