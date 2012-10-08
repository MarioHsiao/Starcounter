
using Starcounter.Binding;
using Starcounter.Query.Execution;
using System;

namespace Starcounter.Query.Optimization
{
    // Holds informaton about which index to use and which sort-ordering to use on the index.
    internal class IndexUseInfo
    {
        internal IndexInfo IndexInfo;
        internal SortOrder SortOrdering;

        internal IndexUseInfo(IndexInfo indexInfo, SortOrder sortOrdering)
        {
            IndexInfo = indexInfo;
            SortOrdering = sortOrdering;
        }
    }
}
