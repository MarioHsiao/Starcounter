
using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
internal class DecimalRangeValue : RangeValue, IComparable<DecimalRangeValue>
{
    public static readonly Nullable<Decimal> MIN_VALUE = null;
    public static readonly Nullable<Decimal> MAX_VALUE = Decimal.MaxValue;

    private Nullable<Decimal> value;

    internal DecimalRangeValue()
    {
        compOp = 0;
        value = null;
    }

    internal Nullable<Decimal> GetValue
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
