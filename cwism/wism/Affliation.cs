using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public abstract class Affliation
    {
        private int identity = 0;

        private string displayName = "Unaffiliated";

        private ControlKind control = ControlKind.Neutral;

        public string DisplayName { get => displayName; set => displayName = value; }
        public ControlKind Control { get => control; set => control = value; }

        public enum ControlKind : int
        {
            Neutral = 0,
            Human = 1
        }       
        
        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
