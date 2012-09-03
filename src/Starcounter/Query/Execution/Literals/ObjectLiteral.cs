﻿
using Starcounter.Query.Optimization;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Globalization;
using Starcounter.Binding;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a literal of type "Object"
/// (a value of an object reference).
/// </summary>
internal class ObjectLiteral : Literal, ILiteral, IObjectPathItem
{
    IObjectView value;
    ITypeBinding typeBinding;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal ObjectLiteral(IObjectView value)
    {
        if (value == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
        }
        this.value = value;
        typeBinding = value.TypeBinding;
        
        // Pre-computing byte array for this literal.
        byteData = ByteArrayBuilder.PrecomputeBuffer(value);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="objId">The ObjectID of this literal.</param>
    internal ObjectLiteral(UInt64 objId)
    {
        value = DbHelper.FromID(objId);
        if (value != null)
        {
            typeBinding = value.TypeBinding;
        }
        else
            // Default type of an object literal.
        {
            typeBinding = TypeRepository.GetTypeBinding("Starcounter.Entity");
        }
        
        // Pre-computing byte array for this literal.
        byteData = ByteArrayBuilder.PrecomputeBuffer(value);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="typeBind">The type resultTypeBind of this null-literal.</param>
    internal ObjectLiteral(ITypeBinding typeBind)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        value = null;
        typeBinding = typeBind;
        
        // Pre-computing byte array for this literal.
        byteData = ByteArrayBuilder.PrecomputeBuffer(value);
    }

    public string Name
    {
        get
        {
            return "Object";
        }
    }

    /// <summary>
    /// The DbTypeCode of this literal.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Object;
        }
    }

    /// <summary>
    /// The type resultTypeBind of the object.
    public QueryTypeCode QueryTypeCode
    {
        get
        {
            return QueryTypeCode.Object;
        }
    }
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public IObjectView EvaluateToObject(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <param name="startObj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public IObjectView EvaluateToObject(IObjectView obj, IObjectView startObj)
    {
        return value;
    }

    /// <summary>
    /// Examines if the value of this literal is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if null-literal, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToObject(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public IObjectExpression Instantiate(CompositeObject obj)
    {
        if (value != null)
        {
            return new ObjectLiteral(value);
        }
        return new ObjectLiteral(typeBinding);
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public IObjectExpression CloneToObject(VariableArray varArray)
    {
        return this;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_REF_LIT";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_REF;
    }

    /// <summary>
    /// Builds a string presentation of this literal using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "ObjectLiteral(");
        if (value != null)
        {
            stringBuilder.Append((value as Entity).ThisRef.ObjectID.ToString());
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
}
}
