// ***********************************************************************
// <copyright file="UIntegerSetFunction.cs" company="Starcounter AB">
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
internal class UIntegerSetFunction : SetFunction, ISetFunction
{
    INumericalExpression numExpr;
    Nullable<UInt64> result;

    internal UIntegerSetFunction(SetFunctionType setFunc, INumericalExpression expr)
    {
        if (setFunc != SetFunctionType.MAX && setFunc != SetFunctionType.MIN)
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
    }

    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.UInt64;
        }
    }

    internal Nullable<UInt64> Result
    {
        get
        {
            return result;
        }
    }

    public ILiteral GetResult()
    {
        return new UIntegerLiteral(Result);
    }

    public void UpdateResult(IObjectView obj)
    {
        Nullable<UInt64> value = numExpr.EvaluateToUInteger(obj);
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
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
        }
    }

    public void ResetResult()
    {
        result = null;
    }

    public ISetFunction Clone(VariableArray varArray)
    {
        return new UIntegerSetFunction(setFuncType, numExpr.CloneToNumerical(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "UIntegerSetFunction(");
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
        UIntegerSetFunction otherNode = other as UIntegerSetFunction;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(UIntegerSetFunction other) {
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
