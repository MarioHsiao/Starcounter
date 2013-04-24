// ***********************************************************************
// <copyright file="ComparisonNumerical.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a numerical comparison which is an operation
/// with operands that are numerical and a result value of type TruthValue.
/// </summary>
internal class ComparisonNumerical : CodeGenFilterNode, IComparison
{
    ComparisonOperator compOperator;
    INumericalExpression expr1;
    INumericalExpression expr2;
    DbTypeCode typeCode;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="compOp">The comparison operator of the operation.</param>
    /// <param name="expr1">The first operand of the operation.</param>
    /// <param name="expr2">The second operand of the operation.</param>
    internal ComparisonNumerical(ComparisonOperator compOp, INumericalExpression expr1, INumericalExpression expr2)
    {
        if (compOp == ComparisonOperator.LIKEdynamic || compOp == ComparisonOperator.LIKEstatic)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOp.");
        }
        if (expr1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1.");
        }
        if (expr2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2.");
        }
        compOperator = compOp;
        this.expr1 = expr1;
        this.expr2 = expr2;
        if (expr1.DbTypeCode == expr2.DbTypeCode)
        {
            typeCode = expr1.DbTypeCode;
        }
        // a_m-TODO: Make code consistent with CodeGen
        // where different numerical types are converted
        // into Decimal (even Double type).
        // However straight conversion from Double to Decimal
        // can result in out-of-range exception.
        else if (expr1.DbTypeCode == DbTypeCode.Double || expr2.DbTypeCode == DbTypeCode.Double ||
                 expr1.DbTypeCode == DbTypeCode.Single || expr2.DbTypeCode == DbTypeCode.Single)
        {
            // For the CodeGen type is determined dynamically
            // by investigating both operands types.
            typeCode = DbTypeCode.Double;
        }
        else
        {
            typeCode = DbTypeCode.Decimal;
        }
    }

    public ComparisonOperator Operator
    {
        get
        {
            return compOperator;
        }
    }

    /// <summary>
    /// Gets if both expressions can code gen.
    /// </summary>
    public override bool CanCodeGen {
        get {
            if ((expr1 is CodeGenFilterNode) && (expr2 is CodeGenFilterNode))
                return (expr1 as CodeGenFilterNode).CanCodeGen && (expr2 as CodeGenFilterNode).CanCodeGen;
            else
                return false;
        }
    }

    public Boolean InvolvesCodeExecution()
    {
        return (expr1.InvolvesCodeExecution() || expr2.InvolvesCodeExecution());
    }

    /// <summary>
    /// Calculates the truth value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The truth value of this operation when evaluated on the input object.</returns>
    public TruthValue Evaluate(IObjectView obj)
    {
        switch (typeCode)
        {
            case DbTypeCode.Int64:
                Nullable<Int64> intValue1 = expr1.EvaluateToIntegerCeiling(obj);
                Nullable<Int64> intValue2 = expr2.EvaluateToIntegerCeiling(obj);
                switch (compOperator)
                {
                    case ComparisonOperator.Equal:
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.NotEqual:
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) != 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThan:
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) < 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThanOrEqual:
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) <= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThan:
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) > 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThanOrEqual:
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) >= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.IS:
                        if (intValue1 == null && intValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.ISNOT:
                        if (intValue1 == null && intValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (intValue1 == null || intValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (intValue1.Value.CompareTo(intValue2.Value) == 0)
                        {
                            return TruthValue.FALSE;
                        }
                        return TruthValue.TRUE;
                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOperator: " + compOperator);
                }

            case DbTypeCode.UInt64:
                Nullable<UInt64> uintValue1 = expr1.EvaluateToUIntegerCeiling(obj);
                Nullable<UInt64> uintValue2 = expr2.EvaluateToUIntegerCeiling(obj);
                switch (compOperator)
                {
                    case ComparisonOperator.Equal:
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.NotEqual:
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) != 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThan:
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) < 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThanOrEqual:
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) <= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThan:
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) > 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThanOrEqual:
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) >= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.IS:
                        if (uintValue1 == null && uintValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.ISNOT:
                        if (uintValue1 == null && uintValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (uintValue1 == null || uintValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (uintValue1.Value.CompareTo(uintValue2.Value) == 0)
                        {
                            return TruthValue.FALSE;
                        }
                        return TruthValue.TRUE;
                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOperator: " + compOperator);
                }

            case DbTypeCode.Decimal:
                Nullable<Decimal> decValue1 = expr1.EvaluateToDecimal(obj);
                Nullable<Decimal> decValue2 = expr2.EvaluateToDecimal(obj);
                switch (compOperator)
                {
                    case ComparisonOperator.Equal:
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.NotEqual:
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) != 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThan:
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) < 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThanOrEqual:
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) <= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThan:
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) > 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThanOrEqual:
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) >= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.IS:
                        if (decValue1 == null && decValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.ISNOT:
                        if (decValue1 == null && decValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (decValue1 == null || decValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (decValue1.Value.CompareTo(decValue2.Value) == 0)
                        {
                            return TruthValue.FALSE;
                        }
                        return TruthValue.TRUE;
                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOperator: " + compOperator);
                }

            case DbTypeCode.Double:
                Nullable<Double> dblValue1 = expr1.EvaluateToDouble(obj);
                Nullable<Double> dblValue2 = expr2.EvaluateToDouble(obj);
                switch (compOperator)
                {
                    case ComparisonOperator.Equal:
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.NotEqual:
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) != 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThan:
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) < 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.LessThanOrEqual:
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) <= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThan:
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) > 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.GreaterThanOrEqual:
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.UNKNOWN;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) >= 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.IS:
                        if (dblValue1 == null && dblValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) == 0)
                        {
                            return TruthValue.TRUE;
                        }
                        return TruthValue.FALSE;
                    case ComparisonOperator.ISNOT:
                        if (dblValue1 == null && dblValue2 == null)
                        {
                            return TruthValue.FALSE;
                        }
                        if (dblValue1 == null || dblValue2 == null)
                        {
                            return TruthValue.TRUE;
                        }
                        if (dblValue1.Value.CompareTo(dblValue2.Value) == 0)
                        {
                            return TruthValue.FALSE;
                        }
                        return TruthValue.TRUE;

                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compOperator: " + compOperator);
                }
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode: " + typeCode);
        }
    }

    /// <summary>
    /// Calculates the Boolean value of this operation when evaluated on an input object.
    /// All properties in this operation are evaluated on the input object.
    /// </summary>
    /// <param name="obj">The object on which to evaluate this operation.</param>
    /// <returns>The Boolean value of this operation when evaluated on the input object.</returns>
    public Boolean Filtrate(IObjectView obj)
    {
        return Evaluate(obj) == TruthValue.TRUE;
    }

    /// <summary>
    /// Creates an more instantiated copy of this expression by evaluating it on a Row.
    /// Properties, with extent numbers for which there exist objects attached to the Row,
    /// are evaluated and instantiated to literals, other properties are not changed.
    /// </summary>
    /// <param name="obj">The Row on which to evaluate the expression.</param>
    /// <returns>A more instantiated expression.</returns>
    public ILogicalExpression Instantiate(Row obj)
    {
        return new ComparisonNumerical(compOperator, expr1.Instantiate(obj), expr2.Instantiate(obj));
    }

    /// <summary>
    /// Gets a path to the given extent.
    /// The path is used for an index scan for the extent with the input extent number, 
    /// if there is such a path and if there is a corresponding index.
    /// </summary>
    /// <param name="extentNum">Input extent number.</param>
    /// <returns>A path, if an appropriate path is found, otherwise null.</returns>
    public IPath GetPathTo(Int32 extentNum)
    {
        // Control if the comparison operator allows an eventual path to be used in an index scan.
        if (!Optimizer.RangeOperator(compOperator))
        {
            return null;
        }
        if (expr1 is IPath && (expr1 as IPath).ExtentNumber == extentNum)
        {
            // Control there is no reference to the current extent (extentNum) in the other expression.
            ExtentSet extentSet = new ExtentSet();
            expr2.InstantiateExtentSet(extentSet);
            if (!extentSet.IncludesExtentNumber(extentNum))
            {
                return (expr1 as IPath);
            }
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNum)
        {
            // Control there is no reference to the current extent (extentNum) in the other expression.
            ExtentSet extentSet = new ExtentSet();
            expr1.InstantiateExtentSet(extentSet);
            if (!extentSet.IncludesExtentNumber(extentNum))
            {
                return (expr2 as IPath);
            }
        }
        return null;
    }

    public ILogicalExpression Clone(VariableArray varArray)
    {
        return new ComparisonNumerical(compOperator, expr1.CloneToNumerical(varArray), expr2.CloneToNumerical(varArray));
    }

    public override void InstantiateExtentSet(ExtentSet extentSet)
    {
        expr1.InstantiateExtentSet(extentSet);
        expr2.InstantiateExtentSet(extentSet);
    }

    public RangePoint CreateRangePoint(Int32 extentNumber, String strPath)
    {
        if (!Optimizer.RangeOperator(compOperator))
        {
            return null;
        }
        if (expr1 is IPath && (expr1 as IPath).ExtentNumber == extentNumber && (expr1 as IPath).FullName == strPath)
        {
            return new NumericalRangePoint(compOperator, expr2);
        }
        if (expr2 is IPath && (expr2 as IPath).ExtentNumber == extentNumber && (expr2 as IPath).FullName == strPath && Optimizer.ReversableOperator(compOperator))
        {
            return new NumericalRangePoint(Optimizer.ReverseOperator(compOperator), expr1);
        }
        return null;
    }

    /// <summary>
    /// Gets a numerical expression to which ObjectNo of this extent is equivalent.
    /// </summary>
    /// <param name="extentNumber"></param>
    /// <returns></returns>
    internal INumericalExpression GetObjectNoExpression(int extentNumber) {
        if (compOperator == ComparisonOperator.Equal) {
            INumericalExpression expr = null;
            if (expr1 is IProperty && (expr1 as IProperty).ExtentNumber == extentNumber && (expr1 as IProperty).FullName == DbHelper.ObjectNoName)
                expr = expr2;
            if (expr2 is IProperty && (expr2 as IProperty).ExtentNumber == extentNumber && (expr2 as IProperty).FullName == DbHelper.ObjectNoName)
                if (expr != null)
                    return null; // Both expressions are ObjectNo properties on the same extent, i.e, self referencing.
                else
                    expr = expr1;
            return expr; // Assuming only useful expressions can be found.
        }
        return null;
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "ComparisonNumerical(");
        stringBuilder.AppendLine(tabs + 1, compOperator.ToString());
        expr1.BuildString(stringBuilder, tabs + 1);
        expr2.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        // Getting instruction string.
        switch (typeCode)
        {
            case DbTypeCode.Int64:
            case DbTypeCode.Int32:
            case DbTypeCode.Int16:
            case DbTypeCode.SByte:
            {
                return CodeAsStringGeneric(compOperator, "ComparisonNumerical", "SINT");
            }

            case DbTypeCode.UInt64:
            case DbTypeCode.UInt32:
            case DbTypeCode.UInt16:
            case DbTypeCode.Byte:
            {
                return CodeAsStringGeneric(compOperator, "ComparisonNumerical", "UINT");
            }
            
            case DbTypeCode.Double:
            {
                return CodeAsStringGeneric(compOperator, "ComparisonNumerical", "FLT8");
            }

            case DbTypeCode.Single:
            {
                return CodeAsStringGeneric(compOperator, "ComparisonNumerical", "FLT4");
            }

            case DbTypeCode.Decimal:
            {
                return CodeAsStringGeneric(compOperator, "ComparisonNumerical", "DEC");
            }

            default:
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode: " + typeCode);
        }
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        // Getting instruction code value.
        switch (typeCode)
        {
            case DbTypeCode.Int64:
            case DbTypeCode.Int32:
            case DbTypeCode.Int16:
            case DbTypeCode.SByte:
            {
                return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.SINT_INCR, "ComparisonNumerical");
            }

            case DbTypeCode.UInt64:
            case DbTypeCode.UInt32:
            case DbTypeCode.UInt16:
            case DbTypeCode.Byte:
            {
                return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.UINT_INCR, "ComparisonNumerical");
            }

            case DbTypeCode.Double:
            {
                return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.FLT8_INCR, "ComparisonNumerical");
            }

            case DbTypeCode.Single:
            {
                return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.FLT4_INCR, "ComparisonNumerical");
            }

            case DbTypeCode.Decimal:
            {
                return InstrCodeGeneric(compOperator, CodeGenFilterInstrCodes.DEC_INCR, "ComparisonNumerical");
            }

            default:
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode: " + typeCode);
        }
    }

    // Appends 'Convert-to-Decimal' instruction depending on the operand's type.
    public static String AddConversionInstr(CodeGenFilterInstrArray instrArray, DbTypeCode origTypeCode)
    {
        // Adding conversion instruction.
        switch (origTypeCode)
        {
            case DbTypeCode.Int64:
            case DbTypeCode.Int32:
            case DbTypeCode.Int16:
            case DbTypeCode.SByte:
            {
                instrArray.Add(CodeGenFilterInstrCodes.CTD_SINT);
                return CodeGenFilterInstrCodes.CTD_SINT + ": CTD_SINT\n";
            }

            case DbTypeCode.UInt64:
            case DbTypeCode.UInt32:
            case DbTypeCode.UInt16:
            case DbTypeCode.Byte:
            {
                instrArray.Add(CodeGenFilterInstrCodes.CTD_UINT);
                return CodeGenFilterInstrCodes.CTD_UINT + ": CTD_UINT\n";
            }

            case DbTypeCode.Double:
            {
                instrArray.Add(CodeGenFilterInstrCodes.CTD_FLT8);
                return CodeGenFilterInstrCodes.CTD_FLT8 + ": CTD_FLT8\n";
            }

            case DbTypeCode.Single:
            {
                instrArray.Add(CodeGenFilterInstrCodes.CTD_FLT4);
                return CodeGenFilterInstrCodes.CTD_FLT4 + ": CTD_FLT4\n";
            }

            case DbTypeCode.Decimal:
            {
                // Do nothing since its already decimal.
                return "";
            }

            default:
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode: " + origTypeCode);
        }
    }

    // Group same numerical types.
    public static DbTypeCode GroupSameNumericalTypes(DbTypeCode origTypeCode)
    {
        // Adding conversion instruction.
        switch (origTypeCode)
        {
            case DbTypeCode.Int64:
            case DbTypeCode.Int32:
            case DbTypeCode.Int16:
            case DbTypeCode.SByte:
            {
                return DbTypeCode.Int64;
            }

            case DbTypeCode.UInt64:
            case DbTypeCode.UInt32:
            case DbTypeCode.UInt16:
            case DbTypeCode.Byte:
            {
                return DbTypeCode.UInt64;
            }

            case DbTypeCode.Double:
            {
                return DbTypeCode.Double;
            }

            case DbTypeCode.Decimal:
            {
                return DbTypeCode.Decimal;
            }

            case DbTypeCode.Single:
            {
                return DbTypeCode.Single;
            }

            default:
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode: " + origTypeCode);
        }
    }

    // Append this node to filter instructions and leaves.
    public override UInt32 AppendToInstrAndLeavesList(List<CodeGenFilterNode> dataLeaves,
                                                      CodeGenFilterInstrArray instrArray,
                                                      Int32 currentExtent,
                                                      StringBuilder filterText)
    {
        UInt32 stackChangeLeft = 0, stackChangeRight = 0;

        // Checking data types for the sub-nodes.
        if (GroupSameNumericalTypes(expr1.DbTypeCode) == GroupSameNumericalTypes(expr2.DbTypeCode))
        {
            // Same type of the data for underlying numerical variables.
            typeCode = expr1.DbTypeCode;
            stackChangeLeft = expr1.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);
            stackChangeRight = expr2.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);
        }
        else
        {
            // Different underlying types so decimal as general type.
            typeCode = DbTypeCode.Decimal;

            // Processing first operand.
            stackChangeLeft = expr1.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);

            // Adding conversion instruction.
            if (instrArray != null)
            {
                String convCodeStr = AddConversionInstr(instrArray, expr1.DbTypeCode);
                if (filterText != null)
                {
                    filterText.Append(convCodeStr);
                }
            }

            // Processing second operand.
            stackChangeRight = expr2.AppendToInstrAndLeavesList(dataLeaves, instrArray, currentExtent, filterText);

            // Adding conversion instruction.
            if (instrArray != null)
            {
                String convCodeStr = AddConversionInstr(instrArray, expr2.DbTypeCode);
                if (filterText != null)
                {
                    filterText.Append(convCodeStr);
                }
            }
        }

        // Fetching the instruction code with reflection of subtypes.
        UInt32 newInstrCode = InstrCode();
        if (instrArray != null)
        {
            instrArray.Add(newInstrCode);
        }

        if (filterText != null)
        {
            filterText.Append(InstrCode() + ": " + CodeAsString() + "\n");
        }

        // Returning total stack change.
        return stackChangeLeft + stackChangeRight + StackChange();
    }

    /// <summary>
    /// Appends operation type to the node type list.
    /// </summary>
    /// <param name="nodeTypeList">List with condition nodes types.</param>
    public override void AddNodeTypeToList(List<ConditionNodeType> nodeTypeList)
    {
        // Calling the base function.
        AddNodeCompTypeToList(compOperator, expr1, expr2, nodeTypeList);
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        expr1.GenerateCompilableCode(stringGen);
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, " " + compOperator.ToString() + " ");
        expr2.GenerateCompilableCode(stringGen);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(ILogicalExpression other) {
        ComparisonNumerical otherNode = other as ComparisonNumerical;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ComparisonNumerical other) {
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
        Debug.Assert(this.compOperator == other.compOperator);
        if (this.compOperator != other.compOperator)
            return false;
        Debug.Assert(this.typeCode == other.typeCode);
        if (this.typeCode != other.typeCode)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.expr1 == null) {
            Debug.Assert(other.expr1 == null);
            areEquals = other.expr1 == null;
        } else
            areEquals = this.expr1.AssertEquals(other.expr1);
        if (areEquals)
            if (this.expr2 == null) {
                Debug.Assert(other.expr2 == null);
                areEquals = other.expr2 == null;
            } else
                areEquals = this.expr2.AssertEquals(other.expr2);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
