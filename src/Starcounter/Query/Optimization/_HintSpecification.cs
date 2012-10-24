// ***********************************************************************
// <copyright file="_HintSpecification.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

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
}
}

