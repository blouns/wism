// File: Wism.Client.AI/Tactical/SimpleBid.cs

using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Tactical
{
    public class SimpleBid : IBid
    {
        public List<Army> Armies { get; private set; }
        public double Utility { get; private set; }
        public ITacticalModule Module { get; private set; }

        public SimpleBid(List<Army> armies, ITacticalModule module, double utility)
        {
            this.Armies = armies;
            this.Module = module;
            this.Utility = utility;
        }
    }
}