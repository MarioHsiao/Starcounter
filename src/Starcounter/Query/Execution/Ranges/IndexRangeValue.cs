// ***********************************************************************
// <copyright file="IndexRangeValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Text;

namespace Starcounter.Query.Execution
{
// TODO: Remove?
/*
internal class IndexRangeValue
{
    // TODO: Rewrite constants below to correct values for the indexes in the kernel.
    internal static readonly Byte[] NULL = { 0 };
    internal static readonly Byte[] MAX_BINARY = { 1, 255, 255, 255, 255, 255, 255, 255, 255 }; // TODO: Change value.
    internal static readonly Byte[] MAX_BOOLEAN = { 1, 1, 0, 0, 0, 0, 0, 0, 0};
    internal static readonly Byte[] MAX_DATETIME = { 1, 255, 255, 255, 255, 255, 255, 255, 255 };
    internal static readonly Byte[] MAX_INTEGER = { 1, 255, 255, 255, 255, 255, 255, 255, 255 };
    internal static readonly Byte[] MAX_OBJECT = { 1, 255, 255, 255, 255, 255, 255, 255, 255 };
    internal static readonly Byte[] MAX_STRING = { 1, 255, 255, 255, 255, 255, 255, 255, 255 }; // TODO: Change value;
    internal static readonly Byte[] MAX_UINTEGER = { 1, 255, 255, 255, 255, 255, 255, 255, 255 };

    ComparisonOperator compOperator;
    Byte[] keyValue; // keyValue is in a special format for the kernel.
    KeyValueFormat keyValueFormat;

    internal IndexRangeValue(ComparisonOperator compOp, Byte[] keyValue, KeyValueFormat format)
    {
        if (keyValue == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect keyValue.");
        }
        compOperator = compOp;
        this.keyValue = keyValue;
        keyValueFormat = format;
    }

    internal IndexRangeValue(ComparisonOperator compOp, Byte[] keyValue, KeyValueFormat format, Boolean appendMaxChar)
    {
        if (keyValue == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect keyValue.");
        }
        compOperator = compOp;
        this.keyValue = keyValue;
        keyValueFormat = format;
        // TODO: If appendMaxChar = true then add special end-characted (method SCAdjustStringTT with flag SC_APPEND_ENDCHAR).
    }

    internal IndexRangeValue(ComparisonOperator compOp, Nullable<Binary> value)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Variable;
        if (value != null)
        {
            keyValue = ConvertToKeyValueVariableLength(value.Value.ToArray());
        }
        else
        {
            keyValue = NULL;
        }
    }

    internal IndexRangeValue(ComparisonOperator compOp, Nullable<Boolean> value)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Fixed;
        if (value != null)
        {
            UInt64 uintValue = (value.Value == true ? (UInt64)1 : (UInt64)0);
            keyValue = ConvertToKeyValueFixedLength(BitConverter.GetBytes(uintValue));
        }
        else
        {
            keyValue = NULL;
        }
    }

    internal IndexRangeValue(ComparisonOperator compOp, Nullable<DateTime> value)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Fixed;
        if (value != null)
        {
            keyValue = ConvertToKeyValueFixedLength(BitConverter.GetBytes(value.Value.ToBinary()));
        }
        else
        {
            keyValue = NULL;
        }
    }

    internal IndexRangeValue(ComparisonOperator compOp, Nullable<Int64> value)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Fixed;
        if (value != null)
        {
            keyValue = ConvertToKeyValueFixedLength(BitConverter.GetBytes(value.Value));
        }
        else
        {
            keyValue = NULL;
        }
    }

    internal IndexRangeValue(ComparisonOperator compOp, Nullable<UInt64> value)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Fixed;
        if (value != null)
        {
            keyValue = ConvertToKeyValueFixedLength(BitConverter.GetBytes(value.Value));
        }
        else
        {
            keyValue = NULL;
        }
    }

    internal IndexRangeValue(ComparisonOperator compOp, String value, Boolean appendMaxChar)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Variable;
        if (value != null)
        {
            keyValue = ConvertToKeyValueVariableLength((new UTF8Encoding()).GetBytes(value));
        }
        else
        {
            keyValue = NULL;
        }
        // TODO: If appendMaxChar = true then add special end-characted (method SCAdjustStringTT with flag SC_APPEND_ENDCHAR.
    }

    internal IndexRangeValue(ComparisonOperator compOp, IObjectView value)
    {
        compOperator = compOp;
        keyValueFormat = KeyValueFormat.Fixed;
        if (value != null)
        {
            keyValue = ConvertToKeyValueFixedLength(BitConverter.GetBytes(value.ThisRef.ObjectID));
        }
        else
        {
            keyValue = NULL;
        }
    }

    internal ComparisonOperator Operator
    {
        get
        {
            return compOperator;
        }
        set
        {
            compOperator = value;
        }
    }

    internal Byte[] KeyValue
    {
        get
        {
            return keyValue;
        }
    }

    internal KeyValueFormat KeyValueFormat
    {
        get
        {
            return keyValueFormat;
        }
    }

    private static Byte[] Reverse(Byte[] value)
    {
        Int32 length = value.Length;
        Byte[] reversed = new Byte[length];
        for (Int32 i = 0; i < length; i++)
        {
            reversed[length - 1 - i] = value[i];
        }
        return reversed;
    }

    private Byte[] ConvertToKeyValueFixedLength(Byte[] value)
    {
        if (value.Length != 8)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
        }
        Byte[] key = new Byte[9];
        // First byte is "1" for defined values.
        key[0] = 1;
        // The rest of the bytes in the key value are the bytes of the input value in reverse order.
        Byte[] reversed = Reverse(value);
        Buffer.BlockCopy(reversed, 0, key, 1, value.Length);
        return key;
    }

    private Byte[] ConvertToKeyValueVariableLength(Byte[] value)
    {
        Int32 length = value.Length;
        Byte[] key = new Byte[length + 5];
        // First byte is "1" for defined values.
        key[0] = 1;
        // Next four bytes represent the length of the subsequent value.
        Byte[] reversed = Reverse(BitConverter.GetBytes(length));
        Buffer.BlockCopy(reversed, 0, key, 1, 4);
        // The rest of the bytes in the key value are the bytes of the input value.
        Buffer.BlockCopy(value, 0, key, 5, length);
        return key;
    }

    private static Int32 CompareIndexKeyValuesVariableLength(Byte[] key1, Byte[] key2)
    {
        Int32 result = 0;
        Int32 minLength = (key1.Length <= key2.Length ? key1.Length : key2.Length);
        Int32 i = 0;
        while (result == 0 && i < minLength)
        {
            result = key1[i].CompareTo(key2[i]);
            i++;
        }
        if (result == 0)
        {
            result = key1.Length.CompareTo(key2.Length);
        }
        return result;
    }

    private static Int32 CompareIndexKeyValuesFixedLength(Byte[] key1, Byte[] key2)
    {
        if (key1.Length != 9)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect key1.");
        }
        if (key2.Length != 9)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect key2.");
        }
        Int32 result = 0;
        // The most significant byte is last and the least significant byte is first.
        Int32 i = 8;
        while (result == 0 && i >= 0)
        {
            result = key1[i].CompareTo(key2[i]);
            i--;
        }
        return result;
    }

    internal IndexRangeValue Clone()
    {
        return new IndexRangeValue(compOperator, keyValue, KeyValueFormat);
    }

    // TODO: Test.
    internal Int32 CompareTo(IndexRangeValue rangeValue)
    {
        if (rangeValue == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rangeValue.");
        }
        Int32 result = 0;
        switch (KeyValueFormat)
        {
            case KeyValueFormat.Variable:
                result = CompareIndexKeyValuesVariableLength(keyValue, rangeValue.keyValue);
                break;
            case KeyValueFormat.Fixed:
                result = CompareIndexKeyValuesFixedLength(keyValue, rangeValue.keyValue);
                break;
        }
        if (result == 0)
        {
            result = ((Int32)compOperator).CompareTo((Int32)rangeValue.compOperator);
        }
        return result;
    }

    internal void AppendKeyValue(IndexRangeValue rangeValue)
    {
        if (rangeValue == null || rangeValue.keyValue == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rangeValue.");
        }
        Byte[] newValue = new Byte[keyValue.Length + rangeValue.keyValue.Length];
        Buffer.BlockCopy(keyValue, 0, newValue, 0, keyValue.Length);
        Buffer.BlockCopy(rangeValue.keyValue, 0, newValue, keyValue.Length, rangeValue.keyValue.Length);
        keyValue = newValue;
    }
}
*/
}
