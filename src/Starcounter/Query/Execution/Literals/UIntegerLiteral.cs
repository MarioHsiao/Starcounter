// ***********************************************************************
// <copyright file="UIntegerLiteral.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Globalization;
using Starcounter.Binding;
using System.Diagnostics;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a literal of type unsigned integer.
/// </summary>
internal class UIntegerLiteral : Literal, ILiteral, IUIntegerPathItem
{
    Nullable<UInt64> value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal UIntegerLiteral(Nullable<UInt64> value)
    {
        this.value = value;
        
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
            return DbTypeCode.UInt64;
        }
    }
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.UInteger;
        }
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable Decimal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Decimal> EvaluateToDecimal(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable Double.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Double> EvaluateToDouble(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal as a nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Int64> EvaluateToInteger(IObjectView obj)
    {
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return null;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this literal as a ceiling (round up) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Int64> EvaluateToIntegerCeiling(IObjectView obj)
    {
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return null;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this literal as a floor (round down) nullable Int64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Int64> EvaluateToIntegerFloor(IObjectView obj)
    {
        if (value == null)
        {
            return null;
        }
        if (value.Value > Int64.MaxValue)
        {
            return Int64.MaxValue;
        }
        return (Int64)value.Value;
    }

    /// <summary>
    /// Calculates the value of this literal as a ceiling (round up) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUIntegerCeiling(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal as a floor (round down) nullable UInt64.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUIntegerFloor(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <param name="startObj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<UInt64> EvaluateToUInteger(IObjectView obj, IObjectView startObj)
    {
        return value;
    }

    public String EvaluateToString() {
        return EvaluateToUInteger(null).ToString();
    }

    /// <summary>
    /// Examines if the value of this literal is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if null-literal, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToUInteger(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public INumericalExpression Instantiate(Row obj)
    {
        return new UIntegerLiteral(value);
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public IUIntegerExpression CloneToUInteger(VariableArray varArray)
    {
        return this;
    }

    public INumericalExpression CloneToNumerical(VariableArray varArray)
    {
        return this;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_UINT_LIT";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_UINT;
    }

    /// <summary>
    /// Builds a string presentation of this literal using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "UIntegerLiteral(");
        if (value != null)
        {
            stringBuilder.Append(value.Value.ToString(NumberFormatInfo.InvariantInfo));
        }
        else
        {
            stringBuilder.Append(Starcounter.Db.NullString);
        }
        stringBuilder.AppendLine(")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, value.ToString());
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        UIntegerLiteral otherNode = other as UIntegerLiteral;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(UIntegerLiteral other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.value == other.value);
        if (this.value != other.value)
            return false;
        return true;
    }
#endif
}
}
