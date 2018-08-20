using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public class UnitHero : Unit
    {
        private string name;

        public UnitHero(string name) => this.name = name;

        public override string GetDisplayName()
        {
            return "Lowenbrau";
        }
    }
}

