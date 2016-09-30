// ***********************************************************************
// <copyright file="BooleanLiteral.cs" company="Starcounter AB">
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
/// Class that holds information about a literal of type Boolean.
/// </summary>
internal class BooleanLiteral : Literal, ILiteral, IBooleanPathItem
{
    Nullable<Boolean> value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal BooleanLiteral(Nullable<Boolean> value)
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
            return DbTypeCode.Boolean;
        }
    }
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Boolean;
        }
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Boolean> EvaluateToBoolean(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <param name="startObj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Boolean> EvaluateToBoolean(IObjectView obj, IObjectView startObj)
    {
        return value;
    }

    public String EvaluateToString() {
        return EvaluateToBoolean(null).ToString();
    }

    /// <summary>
    /// Examines if the value of this literal is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if null-literal, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToBoolean(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public IBooleanExpression Instantiate(Row obj)
    {
        return new BooleanLiteral(value);
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public IBooleanExpression CloneToBoolean(VariableArray varArray)
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
        stringBuilder.Append(tabs, "BooleanLiteral(");
        if (value != null)
        {
            stringBuilder.Append(value.Value.ToString());
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
        BooleanLiteral otherNode = other as BooleanLiteral;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(BooleanLiteral other) {
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
