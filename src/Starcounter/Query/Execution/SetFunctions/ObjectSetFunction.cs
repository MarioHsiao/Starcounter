// ***********************************************************************
// <copyright file="ObjectSetFunction.cs" company="Starcounter AB">
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
internal class ObjectSetFunction : SetFunction, ISetFunction
{
    IObjectExpression expression;
    IObjectView result;

    internal ObjectSetFunction(SetFunctionType setFunc, IObjectExpression expr)
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
        expression = expr;
        result = null;
    }

    public DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Object;
        }
    }

    internal IObjectView Result
    {
        get
        {
            return result;
        }
    }

    public ILiteral GetResult()
    {
        return new ObjectLiteral(Result);
    }

    public void UpdateResult(IObjectView obj)
    {
        IObjectView value = expression.EvaluateToObject(obj);
        switch (setFuncType)
        {
            case SetFunctionType.MAX:
                if (value != null && value is Entity)
                {
                    if (result == null)
                    {
                        result = value;
                    }
                    else if ((value as Entity).ThisRef.ObjectID > (result as Entity).ThisRef.ObjectID)
                    {
                        result = value;
                    }
                }
                break;
            case SetFunctionType.MIN:
                if (value != null && value is Entity)
                {
                    if (result == null)
                    {
                        result = value;
                    }
                    else if ((value as Entity).ThisRef.ObjectID < (result as Entity).ThisRef.ObjectID)
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
        return new ObjectSetFunction(setFuncType, expression.CloneToObject(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ObjectSetFunction(");
        stringBuilder.AppendLine(tabs, setFuncType.ToString());
        expression.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expression.GenerateCompilableCode(stringGen);
    }
}
}
