using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Searchables
{
    public interface ISearchable
    {
        bool CanSearchKind(string kind);

        bool Search(List<Army> armies, Location location, out object result);
    }
}