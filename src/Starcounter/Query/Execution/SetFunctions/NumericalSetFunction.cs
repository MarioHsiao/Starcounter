
using Starcounter;
using Starcounter.Query;
using System;
using System.Collections.Generic;
using System.Collections;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class NumericalSetFunction : SetFunction, ISetFunction
{
    INumericalExpression numExpr;
    Nullable<Decimal> decResult;
    Nullable<Double> dblResult;
    Decimal decSum;
    Double dblSum;
    Decimal count;
    DbTypeCode typeCode;

    internal NumericalSetFunction(SetFunctionType setFunc, INumericalExpression expr)
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
        if (numExpr.DbTypeCode != DbTypeCode.Double)
        {
            typeCode = DbTypeCode.Decimal;
        }
        else
        {
            typeCode = DbTypeCode.Double;
        }
        decResult = null;
        dblResult = null;
        decSum = 0;
        dblSum = 0;
        count = 0;
    }

    public DbTypeCode DbTypeCode
    {
        get
        {
            return typeCode;
        }
    }

    internal Nullable<Decimal> DecimalResult
    {
        get
        {
            switch (setFuncType)
            {
                case SetFunctionType.MAX:
                case SetFunctionType.MIN:
                    return decResult;

                case SetFunctionType.SUM:
                    if (count != 0)
                        return decSum;
                    else
                        return null;

                case SetFunctionType.AVG:
                    if (count != 0)
                        return decSum / count;
                    else
                        return null;

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
            }
        }
    }

    internal Nullable<Double> DoubleResult
    {
        get
        {
            switch (setFuncType)
            {
                case SetFunctionType.MAX:
                case SetFunctionType.MIN:
                    return dblResult;

                case SetFunctionType.SUM:
                    if (count != 0)
                        return dblSum;
                    else
                        return null;

                case SetFunctionType.AVG:
                    if (count != 0)
                        return dblSum / (Double)count;
                    else
                        return null;

                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
            }
        }
    }

    public ILiteral GetResult()
    {
        if (typeCode == DbTypeCode.Decimal)
        {
            return new DecimalLiteral(DecimalResult);
        }
        else
        {
            return new DoubleLiteral(DoubleResult);
        }
    }

    public void UpdateResult(IObjectView obj)
    {
        if (typeCode == DbTypeCode.Decimal)
        {
            Nullable<Decimal> decValue = numExpr.EvaluateToDecimal(obj);
            switch (setFuncType)
            {
                case SetFunctionType.MAX:
                    if (decValue != null)
                    {
                        if (decResult == null)
                        {
                            decResult = decValue;
                        }
                        else if (decValue.Value.CompareTo(decResult.Value) > 0)
                        {
                            decResult = decValue;
                        }
                    }
                    break;
                case SetFunctionType.MIN:
                    if (decValue != null)
                    {
                        if (decResult == null)
                        {
                            decResult = decValue;
                        }
                        else if (decValue.Value.CompareTo(decResult.Value) < 0)
                        {
                            decResult = decValue;
                        }
                    }
                    break;
                case SetFunctionType.SUM:
                case SetFunctionType.AVG:
                    if (decValue != null)
                    {
                        decSum = decSum + decValue.Value;
                        count++;
                    }
                    break;
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
            }
        }
        else
        {
            Nullable<Double> dblValue = numExpr.EvaluateToDouble(obj);
            switch (setFuncType)
            {
                case SetFunctionType.MAX:
                    if (dblValue != null)
                    {
                        if (dblResult == null)
                        {
                            dblResult = dblValue;
                        }
                        else if (dblValue.Value.CompareTo(dblResult.Value) > 0)
                        {
                            dblResult = dblValue;
                        }
                    }
                    break;
                case SetFunctionType.MIN:
                    if (dblValue != null)
                    {
                        if (dblResult == null)
                        {
                            dblResult = dblValue;
                        }
                        else if (dblValue.Value.CompareTo(dblResult.Value) < 0)
                        {
                            dblResult = dblValue;
                        }
                    }
                    break;
                case SetFunctionType.SUM:
                case SetFunctionType.AVG:
                    if (dblValue != null)
                    {
                        dblSum = dblSum + dblValue.Value;
                        count++;
                    }
                    break;
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncType: " + setFuncType);
            }
        }
    }

    public void ResetResult()
    {
        decResult = null;
        dblResult = null;
        decSum = 0;
        dblSum = 0;
        count = 0;
    }

    public ISetFunction Clone(VariableArray varArray)
    {
        return new NumericalSetFunction(setFuncType, numExpr.CloneToNumerical(varArray));
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "NumericalSetFunction(");
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
}
}
