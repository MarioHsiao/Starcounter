
using Starcounter;
using Sc.Server.Internal;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
internal class UIntegerRangeValue : RangeValue, IComparable<UIntegerRangeValue>
{
    public static readonly Nullable<UInt64> MIN_VALUE = null;
    public static readonly Nullable<UInt64> MAX_VALUE = UInt64.MaxValue;

    Nullable<UInt64> value;

    internal UIntegerRangeValue()
    {
        compOp = 0;
        value = null;
    }

    internal Nullable<UInt64> GetValue
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

    public void SetValue(ComparisonOperator compOper, Nullable<UInt64> newValue)
    {
        compOp = compOper;
        value = newValue;
    }

    public Int32 CompareTo(UIntegerRangeValue rightRangeValue)
    {
        Int32 result = Nullable.Compare(GetValue, rightRangeValue.GetValue);

        // Checking comparison operator.
        if (result == 0)
            result = ((Int32)compOp).CompareTo((Int32)rightRangeValue.Operator);

        return result;
    }
}
}
