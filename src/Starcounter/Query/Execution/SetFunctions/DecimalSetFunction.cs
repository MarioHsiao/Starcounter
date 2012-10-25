﻿// ***********************************************************************
// <copyright file="DecimalSetFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query;
using System;
using System.Collections.Generic;
using System.Collections;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class DecimalSetFunction : SetFunction, ISetFunction
{
    INumericalExpression numExpr;
    ITypeExpression valueExpr;
    Nullable<Decimal> result;
    Decimal sum;
    Decimal count;

    internal DecimalSetFunction(SetFunctionType setFunc, INumericalExpression expr)
    {
        if (setFunc != SetFunctionType.MAX && setFunc != SetFunctionType.MIN
            && setFunc != SetFunctionType.AVG && setFunc != SetFunctionType.SUM)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFunc: " + setFunc);
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");
        }
        setFuncType = setFunc;
        numExpr = expr;
        valueExpr = null;
        result = null;
        sum = 0;
        count = 0;
    }

    internal DecimalSetFunction(ITypeExpression expr)
    {
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");
        }
        setFuncType = SetFunctionType.COUNT;
        numExpr = null;
        valueExpr = expr;
        result = null;
        sum = 0;
        count = 0;
    }

    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Decimal;
        }
    }

    internal Nullable<Decimal> Result
    {
        get
        {
            switch (setFuncType)
            {
                case SetFunctionType.MAX:
                case SetFunctionType.MIN:
                    return result;

                case SetFunctionType.SUM:
                    if (count != 0)
                        return sum;
                    else
                        return null;

                case SetFunctionType.COUNT:
                    return count;

                case SetFunctionType.AVG:
                    if (count != 0)
                        return sum / count;
                    else
                        return null;

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
            }
        }
    }

    public ILiteral GetResult()
    {
        return new DecimalLiteral(Result);
    }

    public void UpdateResult(IObjectView obj)
    {
        Nullable<Decimal> numValue = null;
        switch (setFuncType)
        {
            case SetFunctionType.MAX:
                numValue = numExpr.EvaluateToDecimal(obj);
                if (numValue != null)
                {
                    if (result == null)
                    {
                        result = numValue;
                    }
                    else if (numValue.Value.CompareTo(result.Value) > 0)
                    {
                        result = numValue;
                    }
                }
                break;
            case SetFunctionType.MIN:
                numValue = numExpr.EvaluateToDecimal(obj);
                if (numValue != null)
                {
                    if (result == null)
                    {
                        result = numValue;
                    }
                    else if (numValue.Value.CompareTo(result.Value) < 0)
                    {
                        result = numValue;
                    }
                }
                break;
            case SetFunctionType.SUM:
            case SetFunctionType.AVG:
                numValue = numExpr.EvaluateToDecimal(obj);
                if (numValue != null)
                {
                    sum = sum + numValue.Value;
                    count++;
                }
                break;
            case SetFunctionType.COUNT:
                if (!valueExpr.EvaluatesToNull(obj))
                {
                    count++;
                }
                break;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
        }
    }

    public void ResetResult()
    {
        result = null;
        sum = 0;
        count = 0;
    }

    public ISetFunction Clone(VariableArray varArray)
    {
        if (numExpr != null)
        {
            return new DecimalSetFunction(setFuncType, numExpr.CloneToNumerical(varArray));
        }
        if (valueExpr != null)
        {
            return new DecimalSetFunction(valueExpr.Clone(varArray));
        }
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect decimal set function");
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "DecimalSetFunction(");
        stringBuilder.AppendLine(tabs, setFuncType.ToString());
        if (numExpr != null)
        {
            numExpr.BuildString(stringBuilder, tabs + 1);
        }
        else if (valueExpr != null)
        {
            valueExpr.BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        valueExpr.GenerateCompilableCode(stringGen);
    }
}
}
