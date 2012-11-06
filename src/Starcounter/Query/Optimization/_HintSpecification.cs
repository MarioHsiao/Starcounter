// ***********************************************************************
// <copyright file="_HintSpecification.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
/// <summary>
/// Class that holds information about user specified hints to a query.
/// </summary>
internal class HintSpecification
{
    List<IndexHint> indexHintList;
    List<Int32> hintedJoinOrder;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal HintSpecification()
    {
        indexHintList = new List<IndexHint>();
        hintedJoinOrder = null;
    }

    internal Boolean JoinOrderFixed
    {
        get
        {
            // "hintedJoinOrder.Count = 0" represents "JOIN ORDER FIXED".
            return (hintedJoinOrder != null && hintedJoinOrder.Count == 0);
        }
    }

    internal List<Int32> HintedJoinOrder
    {
        get
        {
            return hintedJoinOrder;
        }
    }

    internal List<IndexHint> IndexHintList
    {
        get
        {
            return indexHintList;
        }
    }

    internal void Add(IHint hint)
    {
        // Only the first join order hint will be considered.
        if (hint is JoinOrderHint && hintedJoinOrder == null)
            hintedJoinOrder = (hint as JoinOrderHint).ExtentNumList;

        if (hint is IndexHint)
            indexHintList.Add(hint as IndexHint);
    }

    /// <summary>
    /// Investigates if the input extent order satisfies the hinted join order.
    /// For example the extent order [1,2,3,4] satisfies the hinted join orders 
    /// [1,2,3,4], [1,2,4] and [3,4], 
    /// but it does not satisfy the hinted join orders [1,2,4,3] or [4,3]. 
    /// </summary>
    /// <param name="extentOrder">An input extent order.</param>
    /// <returns>True, if the input extent order satisfies the hinted join order, 
    /// otherwise false.</returns>
    internal Boolean SatisfiesHintedJoinOrder(List<Int32> extentOrder)
    {
        if (extentOrder == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentOrder.");

        if (hintedJoinOrder == null)
            return true;

        Int32 i = 0;
        Int32 j = 0;
        while (i < hintedJoinOrder.Count)
        {
            while (j < extentOrder.Count && extentOrder[j] != hintedJoinOrder[i])
                j++;
            if (j >= extentOrder.Count)
                return false;
            i++;
        }
        return true;
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    internal bool AssertEquals(HintSpecification other) {
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
        // Check cardinalities of collections
        Debug.Assert(this.indexHintList.Count == other.indexHintList.Count);
        if (this.indexHintList.Count != other.indexHintList.Count)
            return false;
        // Check collections of basic types
        if (this.hintedJoinOrder == null) {
            Debug.Assert(other.hintedJoinOrder == null);
            if (other.hintedJoinOrder != null)
                return false;
        } else {
            Debug.Assert(this.hintedJoinOrder.Count == other.hintedJoinOrder.Count);
            if (this.hintedJoinOrder.Count != other.hintedJoinOrder.Count)
                return false;
            for (int i = 0; i < this.hintedJoinOrder.Count; i++) {
                Debug.Assert(this.hintedJoinOrder[i] == other.hintedJoinOrder[i]);
                if (this.hintedJoinOrder[i] != other.hintedJoinOrder[i])
                    return false;
            }
        }
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        // Check collections of objects
        for (int i = 0; i < this.indexHintList.Count && areEquals; i++)
            areEquals = this.indexHintList[i].AssertEquals(other.indexHintList[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}

