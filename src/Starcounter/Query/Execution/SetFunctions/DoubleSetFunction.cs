// ***********************************************************************
// <copyright file="DoubleSetFunction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query;
using System;
using System.Collections.Generic;
using System.Collections;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class DoubleSetFunction : SetFunction, ISetFunction
{
    INumericalExpression numExpr;
    Nullable<Double> result;
    Double sum;
    Decimal count;

    internal DoubleSetFunction(SetFunctionType setFunc, INumericalExpression expr)
    {
        if (setFunc != SetFunctionType.MAX && setFunc != SetFunctionType.MIN
            && setFunc != SetFunctionType.SUM && setFunc != SetFunctionType.AVG)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFunc: " + setFunc);
        }
        if (expr == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr.");
        }
        setFuncType = setFunc;
        numExpr = expr;
        result = null;
        sum = 0;
        count = 0;
    }

    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Double;
        }
    }

    internal Nullable<Double> Result
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

                case SetFunctionType.AVG:
                    if (count != 0)
                        return sum / (Double)count;
                    else
                        return null;

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
            }
        }
    }

    public ILiteral GetResult()
    {
        return new DoubleLiteral(Result);
    }

    public void UpdateResult(IObjectView obj)
    {
        Nullable<Double> value = numExpr.EvaluateToDouble(obj);
        switch (setFuncType)
        {
            case SetFunctionType.MAX:
                if (value != null)
                {
                    if (result == null)
                    {
                        result = value;
                    }
                    else if (value.Value.CompareTo(result.Value) > 0)
                    {
                        result = value;
                    }
                }
                break;
            case SetFunctionType.MIN:
                if (value != null)
                {
                    if (result == null)
                    {
                        result = value;
                    }
                    else if (value.Value.CompareTo(result.Value) < 0)
                    {
                        result = value;
                    }
                }
                break;
            case SetFunctionType.SUM:
            case SetFunctionType.AVG:
                if (value != null)
                {
                    sum = sum + value.Value;
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
        return new DoubleSetFunction(setFuncType, numExpr.CloneToNumerical(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "DoubleSetFunction(");
        stringBuilder.AppendLine(tabs, setFuncType.ToString());
        numExpr.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        numExpr.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(ISetFunction other) {
        DoubleSetFunction otherNode = other as DoubleSetFunction;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(DoubleSetFunction other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check if there are not cyclic references
        Debug.Assert(!this.AssertEqualsVisited);
        if (this.AssertEqualsVisited)
            return false;
        Debug.Assert(!other.AssertEqualsVisited);
        if (other.AssertEqualsVisited)
            return false;
        // Check basic types
        Debug.Assert(this.result == other.result);
        if (this.result != other.result)
            return false;
        Debug.Assert(this.sum == other.sum);
        if (this.sum != other.sum)
            return false;
        Debug.Assert(this.count == other.count);
        if (this.count != other.count)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.numExpr == null) {
            Debug.Assert(other.numExpr == null);
            areEquals = other.numExpr == null;
        } else
            areEquals = this.numExpr.AssertEquals(other.numExpr);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
