
using Starcounter;
using Sc.Server.Internal;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Query.Execution
{
internal class StringRangeValue : RangeValue, IComparable<StringRangeValue>
{
    public static readonly String MIN_VALUE = null;
    public static readonly String MAX_VALUE = "";

    private String value;
    private Boolean appendMaxChar;

    internal StringRangeValue()
    {
        compOp = 0;
        value = null;
        appendMaxChar = false;
    }

    internal String GetValue
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
        appendMaxChar = false;
    }

    public override void ResetValueToMax(ComparisonOperator compOper)
    {
        compOp = compOper;
        value = MAX_VALUE;
        appendMaxChar = true;
    }

    public void SetValue(ComparisonOperator compOper, String newValue, Boolean maxChar)
    {
        compOp = compOper;
        value = newValue;
        appendMaxChar = maxChar;
    }

    internal Boolean AppendMaxChar
    {
        get
        {
            return appendMaxChar;
        }
    }

    public Int32 CompareTo(StringRangeValue rightRangeValue)
    {
        Int32 result = 0;

        Boolean leftIsNull = (value == null),
                rightIsNull = (rightRangeValue.GetValue == null);

        if (!leftIsNull && !rightIsNull)
        {
            String leftString = GetValue,
                   rightString = rightRangeValue.GetValue;

            Boolean rightAppendMaxChar = rightRangeValue.AppendMaxChar;

            Int32 leftLength = leftString.Length;
            Int32 rightLength = rightString.Length;

            Int32 minLength = (leftLength < rightLength) ? leftLength : rightLength;

            Boolean equalPrefix = (leftString.Substring(0, minLength) == rightString.Substring(0, minLength));

            if (!equalPrefix)
            {
                result = DbHelper.StringCompare(leftString, rightString);
            }
            // string1 = AppendMaxChar('abc') and string2 = 'abcd'.
            else if ((leftLength < rightLength) && appendMaxChar)
            {
                result = 1; // Greater.
            }
            // string1 = 'abcd' and string2 = AppendMaxChar('abc').
            else if ((leftLength > rightLength) && rightAppendMaxChar)
            {
                result = -1; // Less.
            }
            // string1 = AppendMaxChar('abc') and string2 = 'abc'.
            else if ((leftLength == rightLength) && appendMaxChar && !rightAppendMaxChar)
            {
                result = 1; // Greater.
            }
            // string1 = 'abc' and string2 = AppendMaxChar('abc').
            else if ((leftLength == rightLength) && !appendMaxChar && rightAppendMaxChar)
            {
                result = -1; // Less.
            }
            else
            {
                result = DbHelper.StringCompare(leftString, rightString);
            }
        }
        else if (leftIsNull && rightIsNull)
        {
            result = 0;
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
            result = ((Int32) compOp).CompareTo((Int32) rightRangeValue.Operator);

        return result;
    }
}
}
