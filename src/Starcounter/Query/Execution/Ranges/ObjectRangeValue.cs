// ***********************************************************************
// <copyright file="ObjectRangeValue.cs" company="Starcounter AB">
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
internal class ObjectRangeValue : RangeValue, IComparable<ObjectRangeValue>
{
    public static readonly IObjectView MIN_VALUE = null;
    public static readonly IObjectView MAX_VALUE = new MaxValueObject();

    private IObjectView value;

    internal ObjectRangeValue()
    {
        compOp = 0;
        value = null;
    }

    internal IObjectView GetValue
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

    public void SetValue(ComparisonOperator compOper, IObjectView newValue)
    {
        compOp = compOper;
        value = newValue;
    }

    public Int32 CompareTo(ObjectRangeValue rightRangeValue)
    {
        Int32 result = 0;

        Boolean leftIsNull = (value == null),
                rightIsNull = (rightRangeValue.GetValue == null),
                leftIsMax = (value is MaxValueObject),
                rightIsMax = (rightRangeValue.GetValue is MaxValueObject);
                //leftEntity = (value is Entity),
                //rightEntity = (rightRangeValue.GetValue is Entity);
        Boolean leftEntity = !leftIsNull && !leftIsMax;
        Boolean rightEntity = !rightIsNull && !rightIsMax;

        if ((leftEntity && rightEntity) &&
            (value.Identity < rightRangeValue.GetValue.Identity))
        {
            result = -1; // Less.
        }
        else if ((leftEntity && rightEntity) &&
                 (value.Identity > rightRangeValue.GetValue.Identity))
        {
            result = 1; // Greater.
        }
        else if (leftIsNull && rightIsNull)
        {
            result = 0; // Equal.
        }
        else if (leftIsNull)
        {
            result = -1; // Less.
        }
        else if (rightIsNull)
        {
            result = 1; // Greater.
        }
        else if (leftIsMax && rightIsMax)
        {
            result = 0; // Equal.
        }
        else if (leftIsMax)
        {
            result = 1; // Greater.
        }
        else if (rightIsMax)
        {
            result = -1; // Less.
        }
        else if (!leftEntity || !rightEntity)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Cannot compare non-entity objects.");
        }

        // Checking comparison operator.
        if (result == 0)
            result = ((Int32)compOp).CompareTo((Int32)rightRangeValue.Operator);

        return result;
    }
}
}
