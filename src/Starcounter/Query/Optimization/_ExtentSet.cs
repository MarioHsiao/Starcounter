// ***********************************************************************
// <copyright file="_ExtentSet.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
/// <summary>
/// Class that represents a set of extent numbers as a single UInt64.
/// For example, a set of extent numbers {0, 2} will be represented by the integer value 5 (2^0 + 2^2).
/// </summary>
class ExtentSet
{
    // TODO: Remove the limitation of 64 extents by replacing the UInt64 representation with a BinaryArray.
    UInt64 extentRepr;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal ExtentSet()
    {
        extentRepr = 0; // Value 0 represents the empty set.
    }

    internal UInt64 Value
    {
        get
        {
            return extentRepr;
        }
    }

    /// <summary>
    /// Empties this extent set.
    /// </summary>
    internal void Empty()
    {
        extentRepr = 0;
    }

    /// <summary>
    /// Returns true if this extent-set is empty, otherwise false.
    /// </summary>
    internal Boolean IsEmpty()
    {
        return (extentRepr == 0);
    }

    internal ExtentSet Union(ExtentSet extentSet)
    {
        // TODO: A more efficient implementation.

        if (extentSet == null || extentSet.IsEmpty())
            return this;

        ExtentSet result = new ExtentSet();
        for (Int32 i = 0; i < 64; i++)
        {
            if (this.IncludesExtentNumber(i) || extentSet.IncludesExtentNumber(i))
                result.AddExtentNumber(i);
        }
        return result;
    }

    private ExtentSet Clone()
    {
        ExtentSet extentSet = new ExtentSet();
        extentSet.extentRepr = this.extentRepr;
        return extentSet;
    }

    /// <summary>
    /// Returns true if this extent set includes the specified extent number.
    /// </summary>
    /// <param name="extentNum">The extent number to be looked for.</param>
    /// <returns>True, if the extent number was included, otherwise false.</returns>
    internal Boolean IncludesExtentNumber(Int32 extentNum)
    {
        if (extentNum < 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentNum.");
        }
        UInt64 temp = extentRepr;
        for (Int32 i = 0; i < extentNum; i++)
        {
            temp = temp / 2;
        }
        temp = temp % 2;
        return (temp == 1);
    }

    /// <summary>
    /// Adds the input extent number to this extent set.
    /// </summary>
    /// <param name="extentNum">An extent number to be added.</param>
    internal void AddExtentNumber(Int32 extentNum)
    {
        if (extentNum < 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentNum.");
        }
        if (!IncludesExtentNumber(extentNum))
        {
            UInt64 temp = 1;
            for (Int32 i = 0; i < extentNum; i++)
            {
                temp = temp * 2;
            }
            extentRepr = extentRepr + temp;
        }
    }

    /// <summary>
    /// Calculates the last extent number in the input extent order that are included in this extent set.
    /// If no extent number is included then the first extent number is returned.
    /// </summary>
    /// <param name="extentOrder">An extent order.</param>
    /// <returns>An extent number.</returns>
    internal Int32 GetLastIncluded(List<Int32> extentOrder)
    {
        if (extentOrder == null || extentOrder.Count == 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentOrder.");
        }
        Int32 extentNum = extentOrder[0];
        for (Int32 i = 1; i < extentOrder.Count; i++)
        {
            if (IncludesExtentNumber(extentOrder[i]))
            {
                extentNum = extentOrder[i];
            }
        }
        return extentNum;
    }

    /// <summary>
    /// Returns true if the specified expression includes a reference to the extent with the specified extent number.
    /// </summary>
    /// <param name="expression">The expression to be investigated.</param>
    /// <param name="extentNum">The extent number to be looked for.</param>
    /// <returns>True, if a reference to the extent was found, otherwise false.</returns>
    internal static Boolean IncludesExtentReference(IValueExpression expression, Int32 extentNum)
    {
        if (expression == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expression.");
        }
        ExtentSet extentSet = new ExtentSet();
        expression.InstantiateExtentSet(extentSet);
        return extentSet.IncludesExtentNumber(extentNum);
    }

    /// <summary>
    /// Returns true if the specified expression includes no reference to any extent.
    /// </summary>
    /// <param name="expression">The expression to be investigated.</param>
    /// <returns>True, if no reference to any extent was found, otherwise false.</returns>
    internal static Boolean IncludesNoExtentReference(IValueExpression expression)
    {
        if (expression == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expression.");
        }
        ExtentSet extentSet = new ExtentSet();
        expression.InstantiateExtentSet(extentSet);
        return extentSet.Value == 0;
    }

    /// <summary>
    /// Creates all extent sets that represent all combinations of the input current extent number and
    /// some subset of extent numbers occuring prior to the current extent number in the extent order.
    /// For example, if the input current extent number is 1 and the input extent order is [0, 2, 3, 1, 4]
    /// then we have the following combinations
    /// {{1}, {1, 0}, {1, 2}, {1, 0, 2}, {1, 3}, {1, 0, 3}, {1, 2, 3}, {1, 0, 2, 3}}
    /// which in the internal representation of the extent sets are represented by the integers
    /// [2, 3, 6, 7, 10, 11, 14, 15].
    /// </summary>
    /// <param name="currentExtentNum">The current extent num.</param>
    /// <param name="extentOrder">The extent order.</param>
    /// <returns>List{ExtentSet}.</returns>
    internal static List<ExtentSet> CreateAllExtentSets(Int32 currentExtentNum, List<Int32> extentOrder)
    {
        if (extentOrder == null || extentOrder.Count == 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentOrder.");
        }
        List<ExtentSet> extentSetList = new List<ExtentSet>();
        ExtentSet extentSet = new ExtentSet();
        extentSet.AddExtentNumber(currentExtentNum);
        extentSetList.Add(extentSet);
        Int32 extentSetListCount = 0;
        Int32 i = 0;
        while (i < extentOrder.Count && extentOrder[i] != currentExtentNum)
        {
            extentSetListCount = extentSetList.Count;
            for (Int32 j = 0; j < extentSetListCount; j++)
            {
                extentSet = extentSetList[j].Clone();
                extentSet.AddExtentNumber(extentOrder[i]);
                extentSetList.Add(extentSet);
            }
            i++;
        }
        return extentSetList;
    }
}
}
