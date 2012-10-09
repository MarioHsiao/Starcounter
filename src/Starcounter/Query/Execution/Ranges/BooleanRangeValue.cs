
using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
internal class BooleanRangeValue : RangeValue, IComparable<BooleanRangeValue>
{
    public static readonly Nullable<Boolean> MIN_VALUE = null;
    public static readonly Nullable<Boolean> MAX_VALUE = true;

    private Nullable<Boolean> value;

    internal BooleanRangeValue()
    {
        compOp = 0;
        value = null;
    }

    internal Nullable<Boolean> GetValue
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

    public void SetValue(ComparisonOperator compOper, Nullable<Boolean> newValue)
    {
        compOp = compOper;
        value = newValue;
    }

    public Int32 CompareTo(BooleanRangeValue rightRangeValue)
    {
        Int32 result = Nullable.Compare(GetValue, rightRangeValue.GetValue);

        // Checking comparison operator.
        if (result == 0)
            result = ((Int32)compOp).CompareTo((Int32)rightRangeValue.Operator);

        return result;
    }
}
}
