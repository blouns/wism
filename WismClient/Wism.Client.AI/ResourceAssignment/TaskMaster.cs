using System;
using System.Collections.Generic;
using Wism.Client.AI.ResourceAssignment;
using Wism.Client.Core;

namespace Wism.Client.AI.ResourceAssignment
{
    public class TaskMaster
    {
        List<TaskableObject> objectsVisible;
        List<TaskableObject> assets;

        public World World { get; }
        public Player Player { get; }

        public TaskMaster(World world, Player player)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void GatherTasks()
        {
            // Detect objects
            var detector = new ObjectDetector(World);
            var taskableObjects = detector.FindTaskableObjects(Player);

            // Generate tasks for objects
            var tasks = new List<Task>();

        }

        public void GeneratePossibleAssignments()
        {
            // Create possible assignments
            // Eliminate impossible combinations            
            //for (n = 0; n < listTask.size(); n++)
            //{
            //    for (f = 0; f < listAsset.size(); f++)
            //    {
            //        if (listAsset[f].isTaskSuitable(listTask[n]))
            //        {
            //            listPossAssignment.add(new PossibleAssignment(listTaskn]));
            //        }
            //    }
            //}

            throw new NotImplementedException();
        }

        public void SortAssignmentsByScore()
        {
            throw new NotImplementedException();
        }

        public void AssignTasks()
        {
            throw new NotImplementedException();
        }
    }
}
