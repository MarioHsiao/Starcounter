using Starcounter.Query.Optimization;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a variable of type Object (reference).
/// </summary>
internal class ObjectVariable : Variable, IVariable, IObjectExpression
{
    IObjectView value;
    ITypeBinding typeBinding;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    /// <param name="typeBind">The type-binding specifying the type of the variable.</param>
    internal ObjectVariable(Int32 number, ITypeBinding typeBind)
    {
        if (typeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");

        this.number = number;
        value = null;
        typeBinding = typeBind;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    /// <param name="value">The value of the variable.</param>
    internal ObjectVariable(Int32 number, IObjectView value)
    {
        if (value == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
        
        this.number = number;
        this.value = value;
        typeBinding = value.TypeBinding;
    }

    /// <summary>
    /// The value of this variable.
    /// </summary>
    public IObjectView Value
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
            return DbTypeCode.Object;
        }
    }

    /// <summary>
    /// The type resultTypeBind of the object.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Results object for which evaluation should be performed.</param>
    public override void AppendToByteArray(ByteArrayBuilder key, IObjectView obj)
    {
        key.Append(value);
    }

    /// <summary>
    /// Appends maximum value to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    public override void AppendMaxToKey(ByteArrayBuilder key)
    {
        key.Append(ObjectRangeValue.MAX_VALUE);
    }

    /// <summary>
    /// Appends minimum value to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    public override void AppendMinToKey(ByteArrayBuilder key)
    {
        key.Append(ObjectRangeValue.MIN_VALUE);
    }

    /// <summary>
    /// Sets value to variable in execution enumerator.
    /// </summary>
    public void ProlongValue(IExecutionEnumerator destEnum)
    {
        destEnum.SetVariable(number, value);
    }

    /// <summary>
    /// Calculates the value of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>The value of this variable.</returns>
    public IObjectView EvaluateToObject(IObjectView obj)
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
        return (EvaluateToObject(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this variable.</returns>
    public IObjectExpression Instantiate(CompositeObject obj)
    {
        return this;
    }

    public IObjectExpression CloneToObject(VariableArray varArray)
    {
        IVariable variable = varArray.GetElement(number);

        if (variable == null)
        {
            ObjectVariable objVariable = new ObjectVariable(number, typeBinding);
            varArray.SetElement(number, objVariable);
            return objVariable;
        }

        if (variable is ObjectVariable)
            return variable as ObjectVariable;

        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "ObjectVariable(");
        if (value != null)
            stringBuilder.Append((value as Entity).ThisRef.ObjectID.ToString());
        else
            stringBuilder.Append(Starcounter.Db.NullString);
        stringBuilder.AppendLine(")");
    }

    public override String ToString()
    {
        if (value != null)
            return value.ToString();
        else
            return Starcounter.Db.NullString;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_REF";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_REF;
    }

    public override void SetNullValue()
    {
        value = null;
    }

    public override void SetValue(IObjectView newValue)
    {
        value = newValue;
    }

    // Throws an InvalidCastException if newValue is of an incompatible type.
    public override void SetValue(Object newValue)
    {
        value = (IObjectView)newValue;
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetObjectVariableValue();");
    }

    /// <summary>
    /// Initializes variable from byte buffer.
    /// </summary>
    public override unsafe void InitFromBuffer(ref Byte* buffer)
    {
        if (*buffer == 0)
        {
            // Undefined value.
            value = null;
            buffer++;
            return;
        }

        // Checking variable data type.
        if (*buffer != SqlConnectivityInterface.QUERY_VARTYPE_OBJECT)
            throw ErrorCode.ToException(Error.SCERRQUERYWRONGPARAMTYPE, "Incorrect query parameter type: " + number);

        // Defined value (contains object id).
        UInt64 oid = (*(UInt64*)(buffer + 1));

        // Creating proxy object or max value object.
        if (oid == UInt64.MaxValue)
            value = new MaxValueObject();
        else
            value = DbHelper.FromID(oid);

        buffer += 9;
    }
}
}
