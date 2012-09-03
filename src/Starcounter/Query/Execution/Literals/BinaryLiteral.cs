
using Starcounter.Query.Optimization;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Globalization;
using Starcounter.Binding;


namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a literal of type Binary.
/// </summary>
internal class BinaryLiteral : Literal, ILiteral, IBinaryPathItem
{
    Nullable<Binary> value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value of this literal.</param>
    internal BinaryLiteral(Nullable<Binary> value)
    {
        this.value = value;
        
        // Pre-computing byte array for this literal.
        byteData = ByteArrayBuilder.PrecomputeBuffer(value);
    }

    /// <summary>
    /// The DbTypeCode of this literal.
    /// </summary>
    public override DbTypeCode DbTypeCode
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

    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Binary> EvaluateToBinary(IObjectView obj)
    {
        return value;
    }
    
    /// <summary>
    /// Calculates the value of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <param name="startObj">Not used.</param>
    /// <returns>The value of this literal.</returns>
    public Nullable<Binary> EvaluateToBinary(IObjectView obj, IObjectView startObj)
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
        return (EvaluateToBinary(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this literal.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this literal.</returns>
    public IBinaryExpression Instantiate(CompositeObject obj)
    {
        return new BinaryLiteral(value);
    }

    public ITypeExpression Clone(VariableArray varArray)
    {
        return this;
    }

    public IBinaryExpression CloneToBinary(VariableArray varArray)
    {
        return this;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_BIN_LIT";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_BIN;
    }

    /// <summary>
    /// Builds a string presentation of this literal using the input string-builder.
    /// </summary>
    /// <param name="stringBuilder">String-builder to use.</param>
    /// <param name="tabs">Number of tab indentations for the presentation.</param>
    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "BinaryLiteral(");
        if (value != null)
        {
            stringBuilder.Append(Starcounter.Db.BinaryToHex(value.Value));
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
