
using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
internal class IntegerRangeValue : RangeValue, IComparable<IntegerRangeValue>
{
    public static readonly Nullable<Int64> MIN_VALUE = null;
    public static readonly Nullable<Int64> MAX_VALUE = Int64.MaxValue;

    private Nullable<Int64> value;

    internal IntegerRangeValue()
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

    public void SetValue(ComparisonOperator compOper, Nullable<Int64> newValue)
    {
        compOp = compOper;
        value = newValue;
    }

    public Int32 CompareTo(IntegerRangeValue rightRangeValue)
    {
        Int32 result = Nullable.Compare(GetValue, rightRangeValue.GetValue);

        // Checking comparison operator.
        if (result == 0)
            result = ((Int32)compOp).CompareTo((Int32)rightRangeValue.Operator);

        return result;
    }
}
}
