// ***********************************************************************
// <copyright file="_IndexUseInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Query.Execution;
using System;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
    // Holds informaton about which index to use and which sort-ordering to use on the index.
    internal class IndexUseInfo
    {
        internal IndexInfo2 IndexInfo;
        internal SortOrder SortOrdering;

        internal IndexUseInfo(IndexInfo2 indexInfo, SortOrder sortOrdering)
        {
            IndexInfo = indexInfo;
            SortOrdering = sortOrdering;
        }

#if DEBUG
        private bool AssertEqualsVisited = false;
        internal bool AssertEquals(IndexUseInfo other) {
            Debug.Assert(other != null);
            if (other == null)
                return false;
            // Check if there are not cyclic references
            Debug.Assert(!this.AssertEqualsVisited);
            if (this.AssertEqualsVisited)
                return false;
            Debug.Assert(!other.AssertEqualsVisited);
            if (other.AssertEqualsVisited)
                return false;
            // Check basic types
            Debug.Assert(this.SortOrdering == other.SortOrdering);
            if (this.SortOrdering != other.SortOrdering)
                return false;
            // Check references. This should be checked if there is cyclic reference.
            AssertEqualsVisited = true;
            bool areEquals = true;
            if (this.IndexInfo == null) {
                Debug.Assert(other.IndexInfo == null);
                areEquals = other.IndexInfo == null;
            } else
                areEquals = this.IndexInfo.AssertEquals(other.IndexInfo);
            AssertEqualsVisited = false;
            return areEquals;
        }
#endif
    }
}
