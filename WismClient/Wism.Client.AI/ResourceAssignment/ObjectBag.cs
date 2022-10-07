using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.ResourceAssignment
{
    public class TaskObjectBag
    {
        public List<TaskableObject> OpposingArmies { get; set; }

        public List<TaskableObject> AllCities { get; set; }

        public List<TaskableObject> UnsearchedLocations { get; set; }

        public List<TaskableObject> LooseItems { get; set; }
    }
 }
