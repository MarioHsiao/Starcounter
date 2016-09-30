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
/// Class that holds information about a variable of type Binary.
/// </summary>
internal class BinaryVariable : Variable, IVariable, IBinaryExpression
{
    Nullable<Binary> value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The number.</param>
    internal BinaryVariable(Int32 number)
    {
        this.number = number;
        value = null;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The number.</param>
    /// <param name="value">The value of the variable.</param>
    internal BinaryVariable(Int32 number, Nullable<Binary> value)
    {
        this.number = number;
        this.value = value;
    }

    /// <summary>
    /// The value of this variable.
    /// </summary>
    public Nullable<Binary> Value
    {
        get
        {
            return value;
        }
        set
        {
            this.value = value;
        }
    }

    /// <summary>
    /// The DbTypeCode of this variable.
    /// </summary>
    public override DbTypeCode DbTypeCode
    {
        get
        {
            return DbTypeCode.Binary;
        }
    }

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(FilterKeyBuilder key, IObjectView obj)
    {
        key.Append(value);
    }

    /// <summary>
    /// Sets value to variable in execution enumerator.
    /// </summary>
    public void ProlongValue(IExecutionEnumerator destEnum)
    {
        if (value == null)
            destEnum.SetVariableToNull(number);
        else
            destEnum.SetVariable(number, value.Value);
    }

    /// <summary>
    /// Calculates the value of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public Nullable<Binary> EvaluateToBinary(IObjectView obj)
    {
        return value;
    }

    /// <summary>
    /// Examines if the value of this variable is null.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>True, if value is null, otherwise false.</returns>
    public override Boolean EvaluatesToNull(IObjectView obj)
    {
        return (EvaluateToBinary(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this variable.</returns>
    public IBinaryExpression Instantiate(Row obj)
    {
        return this;
    }

    public override IValueExpression Clone(VariableArray varArray)
    {
        return CloneToBinary(varArray);
    }

    public IBinaryExpression CloneToBinary(VariableArray varArray)
    {
        IVariable variable = varArray.GetElement(number);
        
        if (variable == null)
        {
            BinaryVariable binVariable = new BinaryVariable(number);
            varArray.SetElement(number, binVariable);
            return binVariable;
        }
        
        if (variable is BinaryVariable)
            return variable as BinaryVariable;

        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "BinaryVariable(");
        if (value != null)
            stringBuilder.Append(Starcounter.Db.BinaryToHex(value.Value));
        else
            stringBuilder.Append(Starcounter.Db.NullString);
        stringBuilder.AppendLine(")");
    }

    public override String ToString()
    {
        if (value != null)
            return Starcounter.Db.BinaryToHex(value.Value);
        else
            return Starcounter.Db.NullString;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_BIN";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_BIN;
    }

    public override void SetNullValue()
    {
        value = null;
    }

    public override void SetValue(Binary newValue)
    {
        value = newValue;
    }

    public override void SetValue(Byte[] newValue)
    {
        value = new Binary(newValue);
    }

    // Throws an InvalidCastException if newValue is of an incompatible type.
    public override void SetValue(Object newValue)
    {
        if (newValue is Byte[])
            value = new Binary((Byte[])newValue);
        else if (newValue is Binary)
            value = (Binary)newValue;
        else
            throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
                "Type of query variable value is expected to be Binary or Byte[], while actual type is " + 
                newValue.GetType().ToString());
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetBinaryVariableValue();");
    }

#if DEBUG
    public bool AssertEquals(IValueExpression other) {
        BinaryVariable otherNode = other as BinaryVariable;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(BinaryVariable other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check parent
        if (!base.AssertEquals(other))
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
