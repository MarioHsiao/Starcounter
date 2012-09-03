
using Starcounter;
using Starcounter.Query;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Collections;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class DoubleSetFunction : SetFunction, ISetFunction
{
    IDoubleExpression expression;
    Nullable<Double> result;
    Double sum;
    Decimal count;

    internal DoubleSetFunction(SetFunctionType setFunc, IDoubleExpression expr)
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
        expression = expr;
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
        Nullable<Double> value = expression.EvaluateToDouble(obj);
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
        return new DoubleSetFunction(setFuncType, expression.CloneToDouble(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "DoubleSetFunction(");
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
