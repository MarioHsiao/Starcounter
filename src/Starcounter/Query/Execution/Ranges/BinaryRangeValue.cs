// ***********************************************************************
// <copyright file="BinaryRangeValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
internal class BinaryRangeValue : RangeValue, IComparable<BinaryRangeValue>
{
    public static readonly Nullable<Binary> MIN_VALUE = null;
    public static readonly Byte[] maxBinary = { 0, 255, 255, 255, 255, 255, 255, 255 };
    public static readonly Nullable<Binary> MAX_VALUE = new Binary(maxBinary); // TODO: Change to correct max value!

    private Nullable<Binary> value;

    internal BinaryRangeValue()
    {
        compOp = 0;
        value = null;
    }

    internal Nullable<Binary> GetValue
    {
        get
        {
            return value;
        }
    }

    public override void ResetValueToMin(ComparisonOperator compOper)
    {
        compOp = compOper;
        value = MIN_VALUE;
    }

    public override void ResetValueToMax(ComparisonOperator compOper)
    {
        compOp = compOper;
        value = MAX_VALUE;
    }

    public void SetValue(ComparisonOperator compOper, Nullable<Binary> newValue)
    {
        compOp = compOper;
        value = newValue;
    }

    public Int32 CompareTo(BinaryRangeValue rightRangeValue)
    {
        Int32 result = 0;

        Boolean leftIsNull = value.HasValue,
                rightIsNull = rightRangeValue.GetValue.HasValue;

        if (!leftIsNull && !rightIsNull)
        {
            result = GetValue.Value.CompareTo(rightRangeValue.GetValue.Value);
        }
        else if (leftIsNull && rightIsNull)
        {
            result = 0; // Equal.
        }
        else if (leftIsNull)
        {
            result = -1; // Less.
        }
        else
        {
            result = 1; // Greater.
        }

        // Checking comparison operator.
        if (result == 0)
            result = ((Int32)compOp).CompareTo((Int32)rightRangeValue.Operator);

        return result;
    }
}
}
