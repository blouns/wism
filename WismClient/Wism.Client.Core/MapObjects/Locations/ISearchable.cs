using System.Collections.Generic;

namespace Wism.Client.MapObjects
{
    public interface ISearchable
    {
        bool CanSearchKind(string kind);

        bool Search(List<Army> armies, out object result);

        SearchStatus GetStatus();
    }
}
