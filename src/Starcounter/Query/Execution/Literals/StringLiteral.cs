// ***********************************************************************
// <copyright file="StringLiteral.cs" company="Starcounter AB">
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
/// Class that holds information about a literal of type String.
/// </summary>
internal class StringLiteral : Literal, ILiteral, IStringPathItem
{
    String value;
    Boolean isPreEvaluatedPattern; // Pre-evaluated pattern for operator LIKEstatic.

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal StringLiteral(String value)
    {
        this.value = value;
        isPreEvaluatedPattern = false;
        
        // Pre-computing byte array for this literal.
        byteData = FilterKeyBuilder.PrecomputeBuffer(value, false);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    /// <param name="isPreEvaluatedPattern">If this literal is a pre-evaluated pattern or not.</param>
    internal StringLiteral(String value, Boolean isPreEvaluatedPattern)
    {
        this.value = value;
        this.isPreEvaluatedPattern = isPreEvaluatedPattern;
        
        // Pre-computing byte array for this literal.
        byteData = FilterKeyBuilder.PrecomputeBuffer(value, false);
    }

    /// <summary>
    /// If this literal is a pre-evaluated pattern or not.
    /// </summary>
    internal Boolean IsPreEvaluatedPattern
    {
        get
        {
            return isPreEvaluatedPattern;
        }
    }

    /// <summary>
    /// The DbTypeCode of this literal.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.String;
        }
    }
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.String;
        }
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public String EvaluateToString(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <param name="startObj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public String EvaluateToString(IObjectView obj, IObjectView startObj)
    {
        return value;
    }

    public String EvaluateToString() {
        return value;
    }

    /// <summary>
    /// Examines if the value of this literal is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if null-literal, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToString(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public IStringExpression Instantiate(Row obj)
    {
        return new StringLiteral(value);
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public IStringExpression CloneToString(VariableArray varArray)
    {
        return this;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_STR_LIT";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_STR;
    }

    /// <summary>
    /// Builds a string presentation of this literal using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "StringLiteral(");
        if (value != null)
        {
            stringBuilder.Append(value);
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
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "\"" + value.ToString() + "\"");
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        StringLiteral otherNode = other as StringLiteral;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(StringLiteral other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.value == other.value);
        if (this.value != other.value)
            return false;
        Debug.Assert(this.isPreEvaluatedPattern == other.isPreEvaluatedPattern);
        if (this.isPreEvaluatedPattern != other.isPreEvaluatedPattern)
            return false;
        return true;
    }
#endif
}
}
