﻿
using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Globalization;
using Starcounter.Internal;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a variable of type DateTime.
/// </summary>
internal class DateTimeVariable : Variable, IVariable, IDateTimeExpression
{
    Nullable<DateTime> value;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    internal DateTimeVariable(Int32 number)
    {
        this.number = number;
        value = null;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    /// <param name="value">The value of the variable.</param>
    internal DateTimeVariable(Int32 number, Nullable<DateTime> value)
    {
        this.number = number;
        this.value = value;
    }

    /// <summary>
    /// The value of this variable.
    /// </summary>
    public Nullable<DateTime> Value
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
            return DbTypeCode.DateTime;
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
        key.Append(DateTimeRangeValue.MAX_VALUE);
    }

    /// <summary>
    /// Appends minimum value to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    public override void AppendMinToKey(ByteArrayBuilder key)
    {
        key.Append(DateTimeRangeValue.MIN_VALUE);
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
    public Nullable<DateTime> EvaluateToDateTime(IObjectView obj)
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
        return (EvaluateToDateTime(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this variable.</returns>
    public IDateTimeExpression Instantiate(CompositeObject obj)
    {
        return this;
    }

    public IDateTimeExpression CloneToDateTime(VariableArray varArray)
    {
        IVariable variable = varArray.GetElement(number);

        if (variable == null)
        {
            DateTimeVariable dtmVariable = new DateTimeVariable(number);
            varArray.SetElement(number, dtmVariable);
            return dtmVariable;
        }

        if (variable is DateTimeVariable)
            return variable as DateTimeVariable;

        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "DateTimeVariable(");
        if (value != null)
            stringBuilder.Append(value.Value.ToString(DateTimeFormatInfo.InvariantInfo));
        else
            stringBuilder.Append(Starcounter.Db.NullString);
        stringBuilder.AppendLine(")");
    }

    public override String ToString()
    {
        if (value != null)
            return value.Value.ToString(DateTimeFormatInfo.InvariantInfo);
        else
            return Starcounter.Db.NullString;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_UINT";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_UINT;
    }

    public override void SetNullValue()
    {
        value = null;
    }

    public override void SetValue(DateTime newValue)
    {
        value = newValue;
    }

    // Throws an InvalidCastException if newValue is of an incompatible type.
    public override void SetValue(Object newValue)
    {
        value = (DateTime)newValue;
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetDateTimeVariableValue();");
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
        if (*buffer != SqlConnectivityInterface.QUERY_VARTYPE_DATETIME)
            throw ErrorCode.ToException(Error.SCERRQUERYWRONGPARAMTYPE, "Incorrect query parameter type: " + number);

        // Defined value (ticks).
        value = new DateTime((*(Int64*)(buffer + 1)));
        buffer += 9;
    }
}
}
