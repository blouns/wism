﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public abstract class MapObject
    {
        public abstract string GetDisplayName();

        public override string ToString()
        {
            return this.GetDisplayName();
        }
    }
}
