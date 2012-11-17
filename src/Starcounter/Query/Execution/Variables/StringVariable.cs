﻿// ***********************************************************************
// <copyright file="StringVariable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using Starcounter.Internal;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a variable of type String.
/// </summary>
internal class StringVariable : Variable, IVariable, IStringExpression
{
    String value;
    Char[] stringBuffer = new Char[512];

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    internal StringVariable(Int32 number)
    {
        this.number = number;
        value = null;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="number">The order number (starting at 0) of this variable in an SQL statement.</param>
    /// <param name="value">The value of the variable.</param>
    internal StringVariable(Int32 number, String value)
    {
        this.number = number;
        this.value = value;
    }

    /// <summary>
    /// The value of this variable.
    /// </summary>
    public String Value
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
            return DbTypeCode.String;
        }
    }

    /// <summary>
    /// Appends data of this leaf to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    /// <param name="obj">Row for which evaluation should be performed.</param>
    public override void AppendToByteArray(ByteArrayBuilder key, IObjectView obj)
    {
        // Appending the current value, not MAXIMUM.
        key.Append(value, false);
    }

    /// <summary>
    /// Appends maximum value to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    public override void AppendMaxToKey(ByteArrayBuilder key)
    {
        key.Append(StringRangeValue.MAX_VALUE, true);
    }

    /// <summary>
    /// Appends minimum value to the provided filter key.
    /// </summary>
    /// <param name="key">Reference to the filter key to which data should be appended.</param>
    public override void AppendMinToKey(ByteArrayBuilder key)
    {
        key.Append(StringRangeValue.MIN_VALUE, false);
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
    public String EvaluateToString(IObjectView obj)
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
        return (EvaluateToString(obj) == null);
    }

    /// <summary>
    /// Creates a copy of this variable.
    /// </summary>
    /// <param name="obj">Not used.</param>
    /// <returns>A copy of this variable.</returns>
    public IStringExpression Instantiate(Row obj)
    {
        return this;
    }

    public IStringExpression CloneToString(VariableArray varArray)
    {
        IVariable variable = varArray.GetElement(number);

        if (variable == null)
        {
            StringVariable strVariable = new StringVariable(number);
            varArray.SetElement(number, strVariable);
            return strVariable;
        }

        if (variable is StringVariable)
            return variable as StringVariable;

        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, "StringVariable(");
        if (value != null)
            stringBuilder.Append(value);
        else
            stringBuilder.Append(Starcounter.Db.NullString);
        stringBuilder.AppendLine(")");
    }

    public override String ToString()
    {
        if (value != null)
            return value;
        else
            return Starcounter.Db.NullString;
    }

    // String representation of this instruction.
    protected override String CodeAsString()
    {
        return "LDV_STR";
    }

    // Instruction code value.
    protected override UInt32 InstrCode()
    {
        return CodeGenFilterInstrCodes.LDV_STR;
    }

    public override void SetNullValue()
    {
        value = null;
    }

    public override void SetValue(String newValue)
    {
        value = newValue;
    }

    // Throws an InvalidCastException if newValue is of an incompatible type.
    public override void SetValue(Object newValue)
    {
        value = (String)newValue;
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "GetStringVariableValue();");
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
        if (*buffer != SqlConnectivityInterface.QUERY_VARTYPE_STRING)
            throw ErrorCode.ToException(Error.SCERRQUERYWRONGPARAMTYPE, "Incorrect query parameter type: " + number);

        // Defined value.
        buffer++;

        Int32 outLenChars = stringBuffer.Length;
        UInt32 inLenBytes = *(UInt32*) buffer;
        UInt32 errorCode = 1;

        fixed (Char* pStringBuffer = stringBuffer)
        {
            errorCode = sccoredb.SCConvertNativeStringToUTF16(buffer + 4, inLenBytes, pStringBuffer, (UInt32 *)&outLenChars);
        }

        if (errorCode != 0)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Can't re-create the String variable from Byte buffer.");

        buffer += inLenBytes + 4;

        // Converting to String variable.
        value = new String(stringBuffer, 0, outLenChars - 1);

        // Using Unicode string conversion instead.
        /*
        if (*buffer == 0)
        {
            // Undefined value.
            value = null;
            buffer++;
            return;
        }

        // Defined value.
        buffer++;
        Int32 lenBytes = *(Int32*)buffer;

        // Copying byte data.
        Marshal.Copy(new IntPtr(buffer + 4), stringByteBuffer, 0, lenBytes);
        value = Encoding.Unicode.GetString(stringByteBuffer, 0, lenBytes);

        buffer += lenBytes + 4;
        */
    }

#if DEBUG
    public bool AssertEquals(ITypeExpression other) {
        StringVariable otherNode = other as StringVariable;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(StringVariable other) {
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
        Debug.Assert(this.stringBuffer == other.stringBuffer);
        if (this.stringBuffer != other.stringBuffer)
            return false;
        return true;
    }
#endif
}
}
