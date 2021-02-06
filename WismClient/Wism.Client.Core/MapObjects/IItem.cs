using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.MapObjects
{
    public interface IItem
    {
        void Take(Hero hero);

        void Drop(Hero hero);
    }
}
