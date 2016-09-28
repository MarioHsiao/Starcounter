// ***********************************************************************
// <copyright file="NumericalVariable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Globalization;
using Starcounter.Internal;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a numerical variable which can hold
/// a value of one of the types: integer, unsigned integer, decimal or double.
/// </summary>
internal class NumericalVariable : Variable, IVariable, INumericalExpression
{
    Nullable<Int64> intValue = null;
    Nullable<UInt64> uintValue = null;
    Nullable<Decimal> decValue = null;
    Nullable<Double> dblValue = null;

    DbTypeCode dbTypeCode;
    CodeGenFilterPrivate privFilterRef = null;
    Int32 numVarIndex = -1;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    internal NumericalVariable(Int32 number)
    {
        this.number = number;
        dbTypeCode = DbTypeCode.Int64; // Default.
    }

    /// <summary>
    /// Constructor with inherited type code, used for cloning.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    /// <param name="dbTypeCode">Type code of the previous numerical variable instance.</param>
    internal NumericalVariable(Int32 number, DbTypeCode dbTypeCode)
    {
        this.number = number;
        this.dbTypeCode = dbTypeCode;
    }

    /// <summary>
    /// The DbTypeCode of this variable.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return dbTypeCode;
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a nullable Double.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return (Nullable<Double>) intValue;

            case DbTypeCode.UInt64:
                return (Nullable<Double>) uintValue;

            case DbTypeCode.Decimal:
                return (Nullable<Double>) decValue;

            case DbTypeCode.Double:
                return dblValue;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a nullable Decimal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return (Nullable<Decimal>) intValue;

            case DbTypeCode.UInt64:
                return (Nullable<Decimal>) uintValue;

            case DbTypeCode.Decimal:
                return decValue;

            case DbTypeCode.Double:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect conversion to decimal.");

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                    return null;
                if (uintValue.Value > Int64.MaxValue)
                    return null;
                return (Int64) uintValue.Value;

            case DbTypeCode.Decimal:
                if (decValue == null)
                    return null;

                Decimal decRoundedValue = Math.Round(decValue.Value);
                if (decRoundedValue < (Decimal) Int64.MinValue)
                    return null;
                if (decRoundedValue > (Decimal) Int64.MaxValue)
                    return null;
                return (Int64) decRoundedValue;

            case DbTypeCode.Double:
                if (dblValue == null)
                    return null;
                Double dblRoundedValue = Math.Round(dblValue.Value);
                if (dblRoundedValue < (Double) Int64.MinValue)
                    return null;
                if (dblRoundedValue > (Double) Int64.MaxValue)
                    return null;
                return (Int64) dblRoundedValue;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                    return null;
                if (uintValue.Value > Int64.MaxValue)
                    return null;
                return (Int64) uintValue.Value;

            case DbTypeCode.Decimal:
                if (decValue == null)
                    return null;
                if (decValue.Value < (Decimal) Int64.MinValue)
                    return Int64.MinValue;
                if (decValue.Value > (Decimal) Int64.MaxValue)
                    return null;
                return (Int64) Math.Ceiling(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                    return null;
                if (dblValue.Value < (Double) Int64.MinValue)
                    return Int64.MinValue;
                if (dblValue.Value > (Double) Int64.MaxValue)
                    return null;
                return (Int64) Math.Ceiling(dblValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return intValue;

            case DbTypeCode.UInt64:
                if (uintValue == null)
                    return null;
                if (uintValue.Value > Int64.MaxValue)
                    return Int64.MaxValue;
                return (Int64) uintValue.Value;
            
            case DbTypeCode.Decimal:
                if (decValue == null)
                    return null;
                if (decValue.Value < (Decimal)Int64.MinValue)
                    return null;
                if (decValue.Value > (Decimal)Int64.MaxValue)
                    return Int64.MaxValue;
                return (Int64)Math.Floor(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                    return null;
                if (dblValue.Value < (Double)Int64.MinValue)
                    return null;
                if (dblValue.Value > (Double)Int64.MaxValue)
                    return Int64.MaxValue;
                return (Int64)Math.Floor(dblValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                    return null;
                if (intValue.Value < (Int64) UInt64.MinValue)
                    return null;
                return (UInt64) intValue.Value;

            case DbTypeCode.UInt64:
                return uintValue;

            case DbTypeCode.Decimal:
                if (decValue == null)
                    return null;
                Decimal decRoundedValue = Math.Round(decValue.Value);
                if (decRoundedValue < (Decimal) UInt64.MinValue)
                    return null;
                if (decRoundedValue > (Decimal) UInt64.MaxValue)
                    return null;
                return (UInt64) decRoundedValue;

            case DbTypeCode.Double:
                if (dblValue == null)
                    return null;
                Double dblRoundedValue = Math.Round(dblValue.Value);
                if (dblRoundedValue < (Double) UInt64.MinValue)
                    return null;
                if (dblRoundedValue > (Double) UInt64.MaxValue)
                    return null;
                return (UInt64) dblRoundedValue;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                    return null;
                if (intValue.Value < (Int64) UInt64.MinValue)
                    return UInt64.MinValue;
                return (UInt64) intValue.Value;

            case DbTypeCode.UInt64:
                return uintValue;
            
            case DbTypeCode.Decimal:
                if (decValue == null)
                    return null;
                if (decValue.Value < (Decimal) UInt64.MinValue)
                    return UInt64.MinValue;
                if (decValue.Value > (Decimal) UInt64.MaxValue)
                    return null;
                return (UInt64) Math.Ceiling(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                    return null;
                if (dblValue.Value < (Double) UInt64.MinValue)
                    return UInt64.MinValue;
                if (dblValue.Value > (Double) UInt64.MaxValue)
                    return null;
                return (UInt64) Math.Ceiling(dblValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Calculates the value of this variable as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue == null)
                    return null;
                if (intValue.Value < (Int64) UInt64.MinValue)
                    return null;
                return (UInt64) intValue.Value;

            case DbTypeCode.UInt64:
                return uintValue;
            
            case DbTypeCode.Decimal:
                if (decValue == null)
                    return null;
                if (decValue.Value < (Decimal) UInt64.MinValue)
                    return null;
                if (decValue.Value > (Decimal) UInt64.MaxValue)
                    return UInt64.MaxValue;
                return (UInt64) Math.Floor(decValue.Value);

            case DbTypeCode.Double:
                if (dblValue == null)
                    return null;
                if (dblValue.Value < (Double) UInt64.MinValue)
                    return null;
                if (dblValue.Value > (Double) UInt64.MaxValue)
                    return UInt64.MaxValue;
                return (UInt64) Math.Floor(dblValue.Value);

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Examines if the value of this variable is null.
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
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Sets value to given variable.
    /// </summary>
    public void ProlongValue(IExecutionEnumerator destEnum)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
            {
                if (intValue == null)
                    destEnum.SetVariableToNull(number);
                else
                    destEnum.SetVariable(number, intValue.Value);

                return;
            }

            case DbTypeCode.UInt64:
            {
                if (uintValue == null)
                    destEnum.SetVariableToNull(number);
                else
                    destEnum.SetVariable(number, uintValue.Value);

                return;
            }

            case DbTypeCode.Decimal:
            {
                if (decValue == null)
                    destEnum.SetVariableToNull(number);
                else
                    destEnum.SetVariable(number, decValue.Value);

                return;
            }

            case DbTypeCode.Double:
            {
                if (dblValue == null)
                    destEnum.SetVariableToNull(number);
                else
                    destEnum.SetVariable(number, dblValue.Value);

                return;
            }

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    /// <summary>
    /// Creates a copy of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this variable.</returns>
    public INumericalExpression Instantiate(Row obj)
    {
        return this;
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        IVariable variable = varArray.GetElement(number);

        if (variable == null)
        {
            // Variable array has just been allocated for cloning,
            // so we need to fill it out.
            NumericalVariable numVariable = new NumericalVariable(number, dbTypeCode);
            varArray.SetElement(number, numVariable);
            return numVariable;
        }

        if (variable is NumericalVariable)
        {
            return variable as NumericalVariable;
        }

        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "NumericalVariable(");
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue != null)
                    stringBuilder.Append(intValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                else
                    stringBuilder.Append(Starcounter.Db.NullString);
                break;

            case DbTypeCode.UInt64:
                if (uintValue != null)
                    stringBuilder.Append(uintValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                else
                    stringBuilder.Append(Starcounter.Db.NullString);
                break;
            
            case DbTypeCode.Decimal:
                if (decValue != null)
                    stringBuilder.Append(decValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                else
                    stringBuilder.Append(Starcounter.Db.NullString);
                break;
            
            case DbTypeCode.Double:
                if (dblValue != null)
                    stringBuilder.Append(dblValue.Value.ToString(NumberFormatInfo.InvariantInfo));
                else
                    stringBuilder.Append(Starcounter.Db.NullString);
                break;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
        stringBuilder.AppendLine(")");
    }

    public override String ToString()
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                if (intValue != null)
                    return intValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                else
                    return Starcounter.Db.NullString;

            case DbTypeCode.UInt64:
                if (uintValue != null)
                    return uintValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                else
                    return Starcounter.Db.NullString;
            
            case DbTypeCode.Decimal:
                if (decValue != null)
                    return decValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                else
                    return Starcounter.Db.NullString;

            case DbTypeCode.Double:
                if (dblValue != null)
                    return dblValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                else
                    return Starcounter.Db.NullString;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                return "LDV_SINT";
            case DbTypeCode.UInt64:
                return "LDV_UINT";
            case DbTypeCode.Decimal:
                return "LDV_DEC";
            case DbTypeCode.Double:
                return "LDV_FLT8";
            case DbTypeCode.Single:
                return "LDV_FLT4";
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
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(ByteArrayBuilder key, IObjectView obj)
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                key.Append(intValue);
                break;
            case DbTypeCode.UInt64:
                key.Append(uintValue);
                break;
            case DbTypeCode.Decimal:
                key.Append(decValue);
                break;
            case DbTypeCode.Double:
                key.Append(dblValue);
                break;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    // Attaches the private filter for later callbacks when type is changed.
    public void AttachPrivateFilter(CodeGenFilterPrivate privFilterRef,
                                    Int32 numVarIndex)
    {
        this.privFilterRef = privFilterRef;
        this.numVarIndex = numVarIndex;
    }

    // Updating numerical variable type in the referenced private filter.
    public void VariableTypeChanged()
    {
        if (privFilterRef != null)
        {
            privFilterRef.NumericalVariableTypeChanged(numVarIndex, dbTypeCode);
        }
    }

    public override void SetNullValue()
    {
        switch (dbTypeCode)
        {
            case DbTypeCode.Int64:
                intValue = null;
                return;

            case DbTypeCode.UInt64:
                uintValue = null;
                return;

            case DbTypeCode.Decimal:
                decValue = null;
                return;

            case DbTypeCode.Double:
                dblValue = null;
                return;

            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect dbTypeCode: " + dbTypeCode);
        }
    }

    public override void SetValue(Int64 newValue)
    {
        intValue = newValue;
        if (dbTypeCode != DbTypeCode.Int64)
        {
            dbTypeCode = DbTypeCode.Int64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(Int32 newValue)
    {
        intValue = newValue;
        if (dbTypeCode != DbTypeCode.Int64)
        {
            dbTypeCode = DbTypeCode.Int64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(Int16 newValue)
    {
        intValue = newValue;
        if (dbTypeCode != DbTypeCode.Int64)
        {
            dbTypeCode = DbTypeCode.Int64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(SByte newValue)
    {
        intValue = newValue;
        if (dbTypeCode != DbTypeCode.Int64)
        {
            dbTypeCode = DbTypeCode.Int64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(UInt64 newValue)
    {
        uintValue = newValue;
        if (dbTypeCode != DbTypeCode.UInt64)
        {
            dbTypeCode = DbTypeCode.UInt64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(UInt32 newValue)
    {
        uintValue = newValue;
        if (dbTypeCode != DbTypeCode.UInt64)
        {
            dbTypeCode = DbTypeCode.UInt64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(UInt16 newValue)
    {
        uintValue = newValue;
        if (dbTypeCode != DbTypeCode.UInt64)
        {
            dbTypeCode = DbTypeCode.UInt64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(Byte newValue)
    {
        uintValue = newValue;
        if (dbTypeCode != DbTypeCode.UInt64)
        {
            dbTypeCode = DbTypeCode.UInt64;
            VariableTypeChanged();
        }
    }

    public override void SetValue(Decimal newValue)
    {
        decValue = newValue;
        if (dbTypeCode != DbTypeCode.Decimal)
        {
            dbTypeCode = DbTypeCode.Decimal;
            VariableTypeChanged();
        }
    }

    public override void SetValue(Double newValue)
    {
        dblValue = newValue;
        if (dbTypeCode != DbTypeCode.Double)
        {
            dbTypeCode = DbTypeCode.Double;
            VariableTypeChanged();
        }
    }

    public override void SetValue(Single newValue)
    {
        dblValue = newValue;
        if (dbTypeCode != DbTypeCode.Double)
        {
            dbTypeCode = DbTypeCode.Double;
            VariableTypeChanged();
        }
    }

    // Throws an InvalidCastException if newValue is of an incompatible type.
    public override void SetValue(Object newValue)
    {
        // Need to check type of each variable.
        TypeCode typeCode = Type.GetTypeCode(newValue.GetType());

        switch (typeCode)
        {
            case TypeCode.SByte:
            {
                SetValue((SByte)newValue);
                return;
            }

            case TypeCode.Int16:
            {
                SetValue((Int16)newValue);
                return;
            }

            case TypeCode.Int32:
            {
                SetValue((Int32)newValue);
                return;
            }

            case TypeCode.Int64:
            {
                SetValue((Int64)newValue);
                return;
            }

            case TypeCode.Byte:
            {
                SetValue((Byte)newValue);
                return;
            }

            case TypeCode.UInt16:
            {
                SetValue((UInt16)newValue);
                return;
            }

            case TypeCode.UInt32:
            {
                SetValue((UInt32)newValue);
                return;
            }

            case TypeCode.UInt64:
            {
                SetValue((UInt64)newValue);
                return;
            }

            case TypeCode.Decimal:
            {
                SetValue((Decimal)newValue);
                return;
            }

            case TypeCode.Double:
            {
                SetValue((Double)newValue);
                return;
            }

            case TypeCode.Single:
            {
                SetValue((Single)newValue);
                return;
            }

            default:
            throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
    "Type of query variable value is expected to be a numerical type, while actual type is " +
    newValue.GetType().ToString());
        }
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetNumericalVariableValue();");
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IValueExpression other) {
        NumericalVariable otherNode = other as NumericalVariable;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(NumericalVariable other) {
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
        // Check parent
        if (!base.AssertEquals(other))
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
        Debug.Assert(this.dbTypeCode == other.dbTypeCode);
        if (this.dbTypeCode != other.dbTypeCode)
            return false;
        Debug.Assert(this.numVarIndex == other.numVarIndex);
        if (this.numVarIndex != other.numVarIndex)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        //bool areEquals = this.privFilterRef.AssertEquals(other.privFilterRef);
        AssertEqualsVisited = false;
        //return areEquals;
        return true;
    }
#endif
}
}
