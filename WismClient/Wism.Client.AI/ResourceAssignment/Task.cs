using System;

namespace Wism.Client.AI.ResourceAssignment
{
    public class Task
    {
        public int Priority { get; set; }

        public int PriorityModifier { get; set; }

        public TaskableObject Objective { get; set; }

        public TaskableObject TaskDoer { get; set; }

        public void Assign(TaskableObject taskDoer)
        {
            throw new NotImplementedException();
        }

        /*
         * Objects:
         *  - Army
         *  - City
         *  - Location
         *  - Item
         * 
         * Tasks:
         *  - Defend City
         *  - Attack City
         *  - Attack Army
         *  - Search Location
         *  - Produce Army
         *  - Take Item
         *  - Build
         *  - Raze
         */
    }
}
