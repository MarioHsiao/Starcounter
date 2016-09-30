// ***********************************************************************
// <copyright file="NumericalLiteral.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Globalization;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a numerical literal which can hold
/// a value of one of the types: integer, unsigned integer, decimal or double.
/// </summary>
internal class NumericalLiteral : Literal, ILiteral, INumericalExpression
{
    Nullable<Int64> intValue = null;
    Nullable<UInt64> uintValue = null;
    Nullable<Decimal> decValue = null;
    Nullable<Double> dblValue = null;
    Nullable<Single> snglValue = null;
    DbTypeCode dbTypeCode;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal NumericalLiteral(Nullable<Int64> value)
    {
        intValue = value;
        dbTypeCode = DbTypeCode.Int64;
        
        // Pre-computing byte array for this literal.
        byteData = FilterKeyBuilder.PrecomputeBuffer(value);
    }

    internal NumericalLiteral(Nullable<UInt64> value)
    {
        uintValue = value;
        dbTypeCode = DbTypeCode.UInt64;

        // Pre-computing byte array for this literal.
        byteData = FilterKeyBuilder.PrecomputeBuffer(value);
    }

    internal NumericalLiteral(Nullable<Decimal> value)
    {
        decValue = value;
        dbTypeCode = DbTypeCode.Decimal;

        // Pre-computing byte array for this literal.
        byteData = FilterKeyBuilder.PrecomputeBuffer(value);
    }

    internal NumericalLiteral(Nullable<Double> value)
    {
        dblValue = value;
        dbTypeCode = DbTypeCode.Double;

        // Pre-computing byte array for this literal.
        byteData = FilterKeyBuilder.PrecomputeBuffer(value);
    }

    /// <summary>
    /// The DbTypeCode of this literal.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return dbTypeCode;
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable Double.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                {
                    return null;
                }
                return (Double)intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                {
                    return null;
                }
                return (Double)uintValue;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                return (Double)decValue;

            case DbTypeCode.Double:
                return dblValue;

            case DbTypeCode.Single:
                return snglValue;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable Decimal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                {
                    return null;
                }
                return (Decimal)intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                {
                    return null;
                }
                return (Decimal)uintValue;

            case DbTypeCode.Decimal:
                return decValue;

            case DbTypeCode.Double:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect conversion to decimal.");

            case DbTypeCode.Single:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect conversion to decimal.");

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                {
                    return null;
                }
                if (uintValue.Value > Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)uintValue.Value;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                Decimal decRoundedValue = Math.Round(decValue.Value);
                if (decRoundedValue < (Decimal)Int64.MinValue)
                {
                    return null;
                }
                if (decRoundedValue > (Decimal)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)decRoundedValue;

            case DbTypeCode.Double:
                if (dblValue == null)
                {
                    return null;
                }
                Double dblRoundedValue = Math.Round(dblValue.Value);
                if (dblRoundedValue < (Double)Int64.MinValue)
                {
                    return null;
                }
                if (dblRoundedValue > (Double)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)dblRoundedValue;

            case DbTypeCode.Single:
                if (snglValue == null)
                {
                    return null;
                }
                Single snglRoundedValue = (Single)Math.Round(snglValue.Value);
                if (snglRoundedValue < (Single) Int64.MinValue)
                {
                    return null;
                }
                if (snglRoundedValue > (Single) Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)snglRoundedValue;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                {
                    return null;
                }
                if (uintValue.Value > Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)uintValue.Value;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)Int64.MinValue)
                {
                    return Int64.MinValue;
                }
                if (decValue.Value > (Decimal)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)Math.Ceiling(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)Int64.MinValue)
                {
                    return Int64.MinValue;
                }
                if (dblValue.Value > (Double)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)Math.Ceiling(dblValue.Value);

            case DbTypeCode.Single:
                if (snglValue == null)
                {
                    return null;
                }
                if (snglValue.Value < (Single)Int64.MinValue)
                {
                    return Int64.MinValue;
                }
                if (snglValue.Value > (Single)Int64.MaxValue)
                {
                    return null;
                }
                return (Int64)Math.Ceiling(snglValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                {
                    return null;
                }
                if (uintValue.Value > Int64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (Int64)uintValue.Value;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)Int64.MinValue)
                {
                    return null;
                }
                if (decValue.Value > (Decimal)Int64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (Int64)Math.Floor(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)Int64.MinValue)
                {
                    return null;
                }
                if (dblValue.Value > (Double)Int64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (Int64)Math.Floor(dblValue.Value);

            case DbTypeCode.Single:
                if (snglValue == null)
                {
                    return null;
                }
                if (snglValue.Value < (Single)Int64.MinValue)
                {
                    return null;
                }
                if (snglValue.Value > (Single)Int64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (Int64)Math.Floor(snglValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                {
                    return null;
                }
                if (intValue.Value < (Decimal)UInt64.MinValue)
                {
                    return null;
                }
                return (UInt64)intValue.Value;

            case DbTypeCode.UInt64:
                return uintValue;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                Decimal decRoundedValue = Math.Round(decValue.Value);
                if (decRoundedValue < (Decimal)UInt64.MinValue)
                {
                    return null;
                }
                if (decRoundedValue > (Decimal)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)decRoundedValue;

            case DbTypeCode.Double:
                if (dblValue == null)
                {
                    return null;
                }
                Double dblRoundedValue = Math.Round(dblValue.Value);
                if (dblRoundedValue < (Double)UInt64.MinValue)
                {
                    return null;
                }
                if (dblRoundedValue > (Double)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)dblRoundedValue;

            case DbTypeCode.Single:
                if (snglValue == null)
                {
                    return null;
                }
                Single snglRoundedValue = (Single)Math.Round(snglValue.Value);
                if (snglRoundedValue < (Single)UInt64.MinValue)
                {
                    return null;
                }
                if (snglRoundedValue > (Single) UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)snglRoundedValue;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                {
                    return null;
                }
                if (intValue.Value < (Decimal)UInt64.MinValue)
                {
                    return UInt64.MinValue;
                }
                return (UInt64)intValue.Value;

            case DbTypeCode.UInt64:
                return uintValue;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)UInt64.MinValue)
                {
                    return UInt64.MinValue;
                }
                if (decValue.Value > (Decimal)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)Math.Ceiling(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)UInt64.MinValue)
                {
                    return UInt64.MinValue;
                }
                if (dblValue.Value > (Double)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)Math.Ceiling(dblValue.Value);

            case DbTypeCode.Single:
                if (snglValue == null)
                {
                    return null;
                }
                if (snglValue.Value < (Single)UInt64.MinValue)
                {
                    return UInt64.MinValue;
                }
                if (snglValue.Value > (Single)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)Math.Ceiling(snglValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this literal as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                {
                    return null;
                }
                if (intValue.Value < (Decimal)UInt64.MaxValue)
                {
                    return null;
                }
                return (UInt64)intValue.Value;

            case DbTypeCode.UInt64:
                return uintValue;

            case DbTypeCode.Decimal:
                if (decValue == null)
                {
                    return null;
                }
                if (decValue.Value < (Decimal)UInt64.MinValue)
                {
                    return null;
                }
                if (decValue.Value > (Decimal)UInt64.MaxValue)
                {
                    return UInt64.MaxValue;
                }
                return (UInt64)Math.Floor(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                {
                    return null;
                }
                if (dblValue.Value < (Double)UInt64.MinValue)
                {
                    return null;
                }
                if (dblValue.Value > (Double)UInt64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (UInt64)Math.Floor(dblValue.Value);

            case DbTypeCode.Single:
                if (snglValue == null)
                {
                    return null;
                }
                if (snglValue.Value < (Single)UInt64.MinValue)
                {
                    return null;
                }
                if (snglValue.Value > (Single)UInt64.MaxValue)
                {
                    return Int64.MaxValue;
                }
                return (UInt64)Math.Floor(snglValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    public String EvaluateToString() {
        switch (dbTypeCode) {
            case DbTypeCode.Int64:
                return intValue.ToString();

            case DbTypeCode.UInt64:
                return uintValue.ToString();

            case DbTypeCode.Decimal:
                return decValue.ToString();

            case DbTypeCode.Double:
                return dblValue.ToString();

            case DbTypeCode.Single:
                return snglValue.ToString();

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Examines if the value of this literal is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if value is null, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return (intValue == null);

            case DbTypeCode.UInt64:
                return (uintValue == null);

            case DbTypeCode.Decimal:
                return (decValue == null);

            case DbTypeCode.Double:
                return (dblValue == null);

            case DbTypeCode.Single:
                return (snglValue == null);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public INumericalExpression Instantiate(Row obj)
    {
        return this;
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        return this;
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "NumericalLiteral(");
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
            {
                if (intValue != null)
                {
                    stringBuilder.Append(intValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                }
                else
                {
                    stringBuilder.Append(Starcounter.Db.NullString);
                }
                break;
            }

            case DbTypeCode.UInt64:
            {
                if (uintValue != null)
                {
                    stringBuilder.Append(uintValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                }
                else
                {
                    stringBuilder.Append(Starcounter.Db.NullString);
                }
                break;
            }

            case DbTypeCode.Decimal:
            {
                if (decValue != null)
                {
                    stringBuilder.Append(decValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                }
                else
                {
                    stringBuilder.Append(Starcounter.Db.NullString);
                }
                break;
            }

            case DbTypeCode.Double:
            {
                if (dblValue != null)
                {
                    stringBuilder.Append(dblValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                }
                else
                {
                    stringBuilder.Append(Starcounter.Db.NullString);
                }
                break;
            }

            case DbTypeCode.Single:
            {
                if (snglValue != null)
                {
                    stringBuilder.Append(snglValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                }
                else
                {
                    stringBuilder.Append(Starcounter.Db.NullString);
                }
                break;
            }

            default:
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
        stringBuilder.AppendLine(")");
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return "LDV_SINT_LIT";

            case DbTypeCode.UInt64:
                return "LDV_UINT_LIT";

            case DbTypeCode.Decimal:
                return "LDV_DEC_LIT";

            case DbTypeCode.Double:
                return "LDV_FLT8_LIT";

            case DbTypeCode.Single:
                return "LDV_FLT4_LIT";

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return CodeGenFilterInstrCodes.LDV_SINT;

            case DbTypeCode.UInt64:
                return CodeGenFilterInstrCodes.LDV_UINT;

            case DbTypeCode.Decimal:
                return CodeGenFilterInstrCodes.LDV_DEC;

            case DbTypeCode.Double:
                return CodeGenFilterInstrCodes.LDV_FLT8;

            case DbTypeCode.Single:
                return CodeGenFilterInstrCodes.LDV_FLT4;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, intValue.ToString());
                break;

            case DbTypeCode.UInt64:
                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, uintValue.ToString());
                break;

            case DbTypeCode.Decimal:
                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, decValue.ToString());
                break;

            case DbTypeCode.Double:
                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, dblValue.ToString());
                break;

            case DbTypeCode.Single:
                stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, snglValue.ToString());
                break;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        NumericalLiteral otherNode = other as NumericalLiteral;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(NumericalLiteral other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.intValue == other.intValue);
        if (this.intValue != other.intValue)
            return false;
        Debug.Assert(this.uintValue == other.uintValue);
        if (this.uintValue != other.uintValue)
            return false;
        Debug.Assert(this.decValue == other.decValue);
        if (this.decValue != other.decValue)
            return false;
        Debug.Assert(this.dblValue == other.dblValue);
        if (this.dblValue != other.dblValue)
            return false;
        Debug.Assert(this.snglValue == other.snglValue);
        if (this.snglValue != other.snglValue)
            return false;
        Debug.Assert(this.dbTypeCode == other.dbTypeCode);
        if (this.dbTypeCode != other.dbTypeCode)
            return false;
        return true;
    }
#endif
}
}
