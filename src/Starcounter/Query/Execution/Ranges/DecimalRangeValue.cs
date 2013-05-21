// ***********************************************************************
// <copyright file="DecimalRangeValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
internal class DecimalRangeValue : RangeValue, IComparable<DecimalRangeValue>
{
    public static readonly Nullable<Int64> MIN_VALUE = null;
    public static readonly Nullable<Int64> MAX_VALUE = DbState.X6DECIMALMAX;

    private Nullable<Int64> value;

    internal DecimalRangeValue()
    {
        compOp = 0;
        value = null;
    }

    internal Nullable<Int64> GetValue
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

    public void SetValue(ComparisonOperator compOper, Nullable<Decimal> newValue)
    {
        compOp = compOper;
        if (newValue == null)
            value = null;
        else
            value = DbState.ClrDecimalToEncodedX6Decimal((Decimal)newValue);
    }

    public void SetValue(ComparisonOperator compOper, Nullable<Int64> newValue) {
        compOp = compOper;
        value = newValue;
    }

    public Int32 CompareTo(DecimalRangeValue rightRangeValue)
    {
        Int32 result = Nullable.Compare(GetValue, rightRangeValue.GetValue);

        // Checking comparison operator.
        if (result == 0)
            result = ((Int32)compOp).CompareTo((Int32)rightRangeValue.Operator);

        return result;
    }
}
}
