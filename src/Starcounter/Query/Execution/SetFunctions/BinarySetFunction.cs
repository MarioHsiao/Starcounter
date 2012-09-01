
using Starcounter;
using Starcounter.Query;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Starcounter.Query.Execution
{
internal class BinarySetFunction : SetFunction, ISetFunction
{
    IBinaryExpression expression;
    Nullable<Binary> result;

    internal BinarySetFunction(SetFunctionType setFunc, IBinaryExpression expr)
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
            return DbTypeCode.Binary;
        }
    }

    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Binary;
        }
    }

    internal Nullable<Binary> Result
    {
        get
        {
            return result;
        }
    }

    public ILiteral GetResult()
    {
        return new BinaryLiteral(Result);
    }

    public void UpdateResult(IObjectView obj)
    {
        Nullable<Binary> value = expression.EvaluateToBinary(obj);
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
        return new BinarySetFunction(setFuncType, expression.CloneToBinary(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "BinarySetFunction(");
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
