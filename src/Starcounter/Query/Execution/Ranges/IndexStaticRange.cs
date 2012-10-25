// ***********************************************************************
// <copyright file="IndexStaticRange.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;

namespace Starcounter.Query.Execution
{

// TODO: Remove?
/*
internal class IndexStaticRange
{
    IndexRangeValue lower;
    IndexRangeValue upper;

    internal IndexStaticRange(IndexRangeValue lower, IndexRangeValue upper)
    {
        this.lower = lower;
        this.upper = upper;
    }

    internal ComparisonOperator LowerOperator
    {
        get
        {
            return lower.Operator;
        }
    }

    internal ComparisonOperator UpperOperator
    {
        get
        {
            return upper.Operator;
        }
    }

    internal Byte[] LowerKey
    {
        get
        {
            return lower.KeyValue;
        }
    }

    internal Byte[] UpperKey
    {
        get
        {
            return upper.KeyValue;
        }
    }

    internal Boolean IsEqualityRange()
    {
        return (lower.KeyValue == upper.KeyValue && lower.Operator == ComparisonOperator.GreaterThanOrEqual &&
                upper.Operator == ComparisonOperator.LessThanOrEqual);
    }

    internal void AppendKeyValues(IndexStaticRange staticRange)
    {
        lower.AppendKeyValue(staticRange.lower);
        upper.AppendKeyValue(staticRange.upper);
    }
}
*/
}
