using System;
using System.Collections.Generic;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta
{
    public class Bid
    {
        public Bid(TacticalModule parent)
        {
            this.Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public TacticalModule Parent { get; set; }

        public List<Army> Assets { get; set; }

        public MapObject Target { get; set; }

        public int UtilityValue { get; set; }

        public int TurnsToComplete { get; internal set; }

        public List<Tile> PathToTarget { get; set; }
    }
}