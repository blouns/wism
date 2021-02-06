using System.Collections.Generic;
using Wism.Client.Core;

namespace Wism.Client.MapObjects
{
    public interface ISearchable
    {
        bool CanSearchKind(string kind);

        bool Search(List<Army> armies, Location location, out object result);       
    }
}
