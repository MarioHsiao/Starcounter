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
/// Class that holds information about a literal of type Type.
/// </summary>
internal class TypeLiteral : Literal, ILiteral, ITypeExpression
{
    ITypeBinding value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal TypeLiteral(ITypeBinding value)
    {
        this.value = value;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">The name of the type that should be the value of this literal.</param>
    internal TypeLiteral(String name)
    {
        this.value = Bindings.GetTypeBinding(name);
    }

    ///// If this literal is a pre-evaluated pattern or not.
    ///// </summary>
    //internal Boolean IsPreEvaluatedPattern
    //{
    //    get
    //    {
    //        return isPreEvaluatedPattern;
    //    }
    //}

    public override DbTypeCode DbTypeCode
    {
        get
        {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "DbTypeCode is not available for Type");
        }
    }

    //public QueryTypeCode QueryTypeCode
    //{
    //    get
    //    {
    //        return QueryTypeCode.String;
    //    }
    //}

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public ITypeBinding EvaluateToType(IObjectView obj)
    {
        return value;
    }

    public String EvaluateToString() {
        ITypeBinding type = EvaluateToType(null);
        if (type == null)
            return null;
        return "type " + type.Name;
    }

    /// <summary>
    /// Examines if the value of this literal is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if null-literal, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToType(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public ITypeExpression Instantiate(Row obj)
    {
        return new TypeLiteral(value);
    }

    public IValueExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public ITypeExpression CloneToType(VariableArray varArray)
    {
        return this;
    }

    //// String representation of this instruction.
    //protected override String CodeAsString()
    //{
    //    return "LDV_STR_LIT";
    //}

    //// Instruction code value.
    //protected override UInt32 InstrCode()
    //{
    //    return CodeGenFilterInstrCodes.LDV_STR;
    //}

    /// <summary>
    /// Builds a string presentation of this literal using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "TypeLiteral(");
        if (value != null)
        {
            stringBuilder.Append(value.Name);
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
        throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        TypeLiteral otherNode = other as TypeLiteral;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(TypeLiteral other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        if (this.value == null) {
            Debug.Assert(other.value == null);
            if (other.value != null)
                return false;
        } else {
            Debug.Assert(this.value.Name == other.value.Name);
            if (this.value.Name != other.value.Name)
                return false;
        }
        return true;
    }
#endif
}
}
