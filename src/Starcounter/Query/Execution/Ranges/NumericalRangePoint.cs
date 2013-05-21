// ***********************************************************************
// <copyright file="NumericalRangePoint.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
internal class NumericalRangePoint : RangePoint
{
    INumericalExpression expr;
    IntegerRangeValue cachedIntRangeValue;
    UIntegerRangeValue cachedUintRangeValue;
    DecimalRangeValue cachedDecRangeValue;

    internal NumericalRangePoint(ComparisonOperator compOp, INumericalExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr.");
        }
        this.compOp = compOp;
        this.expr = expr;
        cachedIntRangeValue = new IntegerRangeValue();
        cachedUintRangeValue = new UIntegerRangeValue();
        cachedDecRangeValue = new DecimalRangeValue();
    }

    public IntegerRangeValue EvaluateToInteger1(IObjectView obj)
    {
        Nullable<Int64> floor = expr.EvaluateToIntegerFloor(obj);
        Nullable<Int64> ceiling = expr.EvaluateToIntegerCeiling(obj);

        // Value = null.
        if (floor == null && ceiling == null)
        {
            cachedIntRangeValue.ResetValueToMin(compOp);
            return cachedIntRangeValue;
        }

        // Value < Int64.MinValue.
        if (floor == null && ceiling == Int64.MinValue)
        {
            switch (compOp)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.LessThan:
                case ComparisonOperator.LessThanOrEqual:
                {
                    cachedIntRangeValue.ResetValueToMin(compOp);
                    return cachedIntRangeValue;
                }
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.GreaterThanOrEqual:
                {
                    cachedIntRangeValue.SetValue(ComparisonOperator.GreaterThanOrEqual, ceiling);
                    return cachedIntRangeValue;
                }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
            }
        }

        // Value > Int64.MaxValue.
        if (floor == Int64.MaxValue && ceiling == null)
        {
            switch (compOp)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.GreaterThanOrEqual:
                {
                    cachedIntRangeValue.ResetValueToMin(compOp);
                    return cachedIntRangeValue;
                }
                case ComparisonOperator.LessThan:
                case ComparisonOperator.LessThanOrEqual:
                {
                    cachedIntRangeValue.SetValue(ComparisonOperator.LessThanOrEqual, floor);
                    return cachedIntRangeValue;
                }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
            }
        }

        // Int64.MinValue <= value <= Int64.MaxValue.
        switch (compOp)
        {
            case ComparisonOperator.Equal:
            {
                if (ceiling == floor)
                {
                    cachedIntRangeValue.SetValue(compOp, ceiling);
                    return cachedIntRangeValue;
                }
                cachedIntRangeValue.ResetValueToMin(compOp);
                return cachedIntRangeValue;
            }
            case ComparisonOperator.GreaterThan:
            case ComparisonOperator.LessThanOrEqual:
            {
                cachedIntRangeValue.SetValue(compOp, floor);
                return cachedIntRangeValue;
            }
            case ComparisonOperator.GreaterThanOrEqual:
            case ComparisonOperator.LessThan:
            {
                cachedIntRangeValue.SetValue(compOp, ceiling);
                return cachedIntRangeValue;
            }
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
        }
    }

    public UIntegerRangeValue EvaluateToUInteger1(IObjectView obj)
    {
        Nullable<UInt64> floor = expr.EvaluateToUIntegerFloor(obj);
        Nullable<UInt64> ceiling = expr.EvaluateToUIntegerCeiling(obj);

        // Value = null.
        if (floor == null && ceiling == null)
        {
            cachedUintRangeValue.ResetValueToMin(compOp);
            return cachedUintRangeValue;
        }

        // Value < UInt64.MinValue.
        if (floor == null && ceiling == UInt64.MinValue)
        {
            switch (compOp)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.LessThan:
                case ComparisonOperator.LessThanOrEqual:
                {
                    cachedUintRangeValue.ResetValueToMin(compOp);
                    return cachedUintRangeValue;
                }
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.GreaterThanOrEqual:
                {
                    cachedUintRangeValue.SetValue(ComparisonOperator.GreaterThanOrEqual, ceiling);
                    return cachedUintRangeValue;
                }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
            }
        }

        // Value > UInt64.MaxValue.
        if (floor == UInt64.MaxValue && ceiling == null)
        {
            switch (compOp)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.GreaterThanOrEqual:
                {
                    cachedUintRangeValue.ResetValueToMin(compOp);
                    return cachedUintRangeValue;
                }
                case ComparisonOperator.LessThan:
                case ComparisonOperator.LessThanOrEqual:
                {
                    cachedUintRangeValue.SetValue(ComparisonOperator.LessThanOrEqual, floor);
                    return cachedUintRangeValue;
                }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
            }
        }

        // UInt64.MinValue <= value <= UInt64.MaxValue.
        switch (compOp)
        {
            case ComparisonOperator.Equal:
            {
                if (ceiling == floor)
                {
                    cachedUintRangeValue.SetValue(compOp, ceiling);
                    return cachedUintRangeValue;
                }
                cachedUintRangeValue.ResetValueToMin(compOp);
                return cachedUintRangeValue;
            }
            case ComparisonOperator.GreaterThan:
            case ComparisonOperator.LessThanOrEqual:
            {
                cachedUintRangeValue.SetValue(compOp, floor);
                return cachedUintRangeValue;
            }
            case ComparisonOperator.GreaterThanOrEqual:
            case ComparisonOperator.LessThan:
            {
                cachedUintRangeValue.SetValue(compOp, ceiling);
                return cachedUintRangeValue;
            }
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
        }
    }

    public DecimalRangeValue EvaluateToDecimal(IObjectView obj)
    {
        // TODO: Actually we need to calculate different floor and ceiling Decimal values 
        // when dealing with Double values outside the range of Decimal, which is from +/-1.0E-28 to +/-7.9E+28.
        Nullable<Decimal> floor = expr.EvaluateToDecimal(obj);
        Nullable<Decimal> ceiling = floor;

        // Value = null.
        if (floor == null && ceiling == null)
        {
            cachedDecRangeValue.ResetValueToMin(compOp);
            return cachedDecRangeValue;
        }

        // Value < Decimal.MinValue.
        if (floor == null && ceiling == Decimal.MinValue) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "It is not expected to hit it");
#if false
            switch (compOp)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.LessThan:
                case ComparisonOperator.LessThanOrEqual:
                    {
                        cachedDecRangeValue.ResetValueToMin(compOp);
                        return cachedDecRangeValue;
                    }
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.GreaterThanOrEqual:
                    {
                        cachedDecRangeValue.SetValue(ComparisonOperator.GreaterThanOrEqual, DbState.X6DECIMALMIN);
                        return cachedDecRangeValue;
                    }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
            }
#endif
        }

        // Value > Decimal.MaxValue.
        if (floor == Decimal.MaxValue && ceiling == null) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "It is not expected to hit it");
#if false
            switch (compOp)
            {
                case ComparisonOperator.Equal:
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.GreaterThanOrEqual:
                    {
                        cachedDecRangeValue.ResetValueToMin(compOp);
                        return cachedDecRangeValue;
                    }
                case ComparisonOperator.LessThan:
                case ComparisonOperator.LessThanOrEqual:
                    {
                        cachedDecRangeValue.SetValue(ComparisonOperator.LessThanOrEqual, DbState.X6DECIMALMAX);
                        return cachedDecRangeValue;
                    }
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
            }
#endif
        }

        // Decimal.MinValue <= value <= Decimal.MaxValue.
        switch (compOp)
        {
            case ComparisonOperator.Equal:
                {
                    if (ceiling == floor)
                    {
                        try {
                            cachedDecRangeValue.SetValue(compOp, ceiling);
                        } catch {
                            cachedDecRangeValue.ResetValueToMin(compOp);
                        }
                        return cachedDecRangeValue;
                    }
                    cachedDecRangeValue.ResetValueToMin(compOp); // Null
                    return cachedDecRangeValue;
                }
            case ComparisonOperator.GreaterThan:
            case ComparisonOperator.LessThanOrEqual:
                {
                    try {
                        cachedDecRangeValue.SetValue(compOp, floor);
                    } catch {
                        cachedDecRangeValue.ResetValueToMin(compOp);
                    }
                    return cachedDecRangeValue;
                }
            case ComparisonOperator.GreaterThanOrEqual:
            case ComparisonOperator.LessThan:
                {
                    try {
                        cachedDecRangeValue.SetValue(compOp, ceiling);
                    } catch {
                        cachedDecRangeValue.ResetValueToMax(compOp);
                    }
                    return cachedDecRangeValue;
                }
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp: " + compOp);
        }
    }

    public INumericalExpression Expression
    {
        get
        {
            return expr;
        }
    }

    public NumericalRangePoint Clone(VariableArray varArray)
    {
        return new NumericalRangePoint(compOp, expr.CloneToNumerical(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "NumericalRangePoint(");
        stringBuilder.AppendLine(tabs + 1, compOp.ToString());
        expr.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expr.GenerateCompilableCode(stringGen);
    }
}
}
