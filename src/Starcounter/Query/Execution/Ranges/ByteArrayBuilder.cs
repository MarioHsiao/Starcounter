// ***********************************************************************
// <copyright file="ByteArrayBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;

namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Class ByteArrayBuilder
    /// </summary>
internal sealed class ByteArrayBuilder
{
    Byte[] dataBuffer; // Data array.
    Int32 position; // The current position in the buffer where data should be appended.

    internal ByteArrayBuilder()
    {
        dataBuffer = new Byte[SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES];
        position = 4; // Because size of the data stream is in first 4 bytes.
    }

    // Reseting buffer for further re-usage.
    internal void ResetCached()
    {
        position = 4; // Because size of the data stream is in first 4 bytes.
    }

    // Copying one key builder data to another.
    internal void CopyToAnotherByteArray(ByteArrayBuilder anotherByteArray)
    {
        if (anotherByteArray.position == position)
        {
            return;  // If positions are the same we don't copy anything.
        }
        Buffer.BlockCopy(dataBuffer, 0, anotherByteArray.dataBuffer, 0, position);
        anotherByteArray.position = position;
    }

    // Appending precomputed byte keyDataBuffer to the end of the data stream.
    internal void AppendPrecomputedBuffer(Byte[] precompBuffer)
    {
        Buffer.BlockCopy(precompBuffer, 0, dataBuffer, position, precompBuffer.Length);
        position += precompBuffer.Length;
    }

    // Getting key data stream.
    /// <summary>
    /// Gets the buffer cached.
    /// </summary>
    /// <returns>Byte[][].</returns>
    internal unsafe Byte[] GetBufferCached()
    {
        // First four bytes represent the total length of the key.
        fixed (Byte *buf = dataBuffer)
        {
            *(Int32 *)buf = position;
        }
        return dataBuffer;
    }

    ////////////////////////////////////////////////////////////////
    // Group of functions for appending int64 to the data stream. //
    ////////////////////////////////////////////////////////////////

    internal unsafe void AppendNonNullValue(
        Int64 value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte *buf = dataBuffer)
        {
            // First byte is non-zero for defined values.
            *(buf + position) = embedType;

            // Copying actual data bytes.
            *(Int64 *)(buf + position + 1) = value;
        }

        position += 9;
    }

    private unsafe static void AppendNonNullValue(
        Int64 value,
        Byte[] dataArray,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte *buf = dataArray)
        {
            // First byte is non-zero for defined values.
            *buf = embedType;

            // Copying actual data bytes.
            *(Int64 *) (buf + 1) = value;
        }
    }

    internal void Append(
        Nullable<Int64> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        AppendNonNullValue(value.Value, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<Int64> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 }; // Undefined value.
            return dataArrayForNull;
        }

        Byte[] dataArray = new Byte[9];
        AppendNonNullValue(value.Value, dataArray, embedType);
        return dataArray;
    }

    /////////////////////////////////////////////////////////
    // Group of functions for appending uint64 to the key. //
    /////////////////////////////////////////////////////////

    internal unsafe void AppendNonNullValue(
        UInt64 value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte *buf = dataBuffer)
        {
            // First byte is non-zero for defined values.
            *(buf + position) = embedType;

            // Copying actual data bytes.
            *(UInt64 *)(buf + position + 1) = value;
        }

        position += 9;
    }

    private unsafe static void AppendNonNullValue(
        UInt64 value,
        Byte[] dataArray,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte *buf = dataArray)
        {
            // First byte is non-zero for defined values.
            *buf = embedType;

            // Copying actual data bytes.
            *(UInt64 *) (buf + 1) = value;
        }
    }

    internal void Append(
        Nullable<UInt64> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        AppendNonNullValue(value.Value, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<UInt64> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 }; // Undefined value.
            return dataArrayForNull;
        }

        Byte[] dataArray = new Byte[9];
        AppendNonNullValue(value.Value, dataArray, embedType);
        return dataArray;
    }

    /////////////////////////////////////////////////////////
    // Group of functions for appending double to the key. //
    /////////////////////////////////////////////////////////

    internal unsafe void AppendNonNullValue(
        Double value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte *buf = dataBuffer)
        {
            // First byte is non-zero for defined values.
            *(buf + position) = embedType;

            // Copying actual data bytes.
            *(Double *)(buf + position + 1) = value;
        }

        position += 9;
    }

    private unsafe static void AppendNonNullValue(
        Double value,
        Byte[] dataArray,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte * buf = dataArray)
        {
            // First byte is non-zero for defined values.
            *buf = embedType;

            // Copying actual data bytes.
            *(Double *)(buf + 1) = value;
        }
    }

    internal void Append(
        Nullable<Double> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        AppendNonNullValue(value.Value, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<Double> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 }; // Undefined value.
            return dataArrayForNull;
        }

        Byte[] dataArray = new Byte[9];
        AppendNonNullValue(value.Value, dataArray, embedType);
        return dataArray;
    }

    //////////////////////////////////////////////////////////
    // Group of functions for appending decimal to the key. //
    //////////////////////////////////////////////////////////

    internal unsafe void AppendNonNullValue(
        Decimal value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        Int64 value2 = X6Decimal.ToEncoded(value);
        AppendNonNullValue(value2, embedType);
    }

    private unsafe static void AppendNonNullValue(
        Decimal value,
        Byte[] dataArray,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        Int64 value2 = X6Decimal.ToEncoded(value);
        AppendNonNullValue(value2, dataArray, embedType);
    }

    internal void Append(
        Nullable<Decimal> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        AppendNonNullValue(value.Value, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<Decimal> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 }; // Undefined value.
            return dataArrayForNull;
        }

        Byte[] dataArray = new Byte[17]; // Decimal is 128-bits long.
        AppendNonNullValue(value.Value, dataArray, embedType);
        return dataArray;
    }

    /////////////////////////////////////////////////////////
    // Group of functions for appending binary to the key. //
    /////////////////////////////////////////////////////////

    internal void AppendNonNullValue(
        Binary value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // First byte is non-zero for defined values.
        dataBuffer[position] = embedType;
        position++;

        var valueLen = value.GetLength();
        var adjustedLen = valueLen + 5;
        Buffer.BlockCopy(value.GetInternalBuffer(), 0, dataBuffer, position, adjustedLen);
        position += adjustedLen;
    }

    private static void AppendNonNullValue(
        Binary value,
        Byte[] dataArray,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // First byte is non-zero for defined values.
        dataArray[0] = embedType;

        var valueLen = value.GetLength();
        var adjustedLen = valueLen + 5;
        Buffer.BlockCopy(value.GetInternalBuffer(), 0, dataArray, 1, adjustedLen);
    }

    internal void Append(
        Nullable<Binary> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        AppendNonNullValue(value.Value, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<Binary> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 }; // Represents undefined value.
            return dataArrayForNull;
        }

        Int32 totalLengthBytes = value.Value.GetLength() + 5;
        Byte[] dataArray = new Byte[totalLengthBytes + 1];
        AppendNonNullValue(value.Value, dataArray, embedType);
        return dataArray;
    }

    //////////////////////////////////////////////////////////
    // Group of functions for appending boolean to the key. //
    //////////////////////////////////////////////////////////

    internal unsafe void AppendNonNullValue(
        Boolean value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte* buf = dataBuffer)
        {
            // First byte is non-zero for defined values.
            *(buf + position) = embedType;
            position++;

            // Zeroing memory.
            *(UInt64*) (buf + position) = 0;

            // When TRUE first byte is 1.
            if (value == true)
            {
                *(buf + position) = 1;
            }
            else
            {
                *(buf + position) = 0;
            }
        }

        position += 8;
    }

    private unsafe static void AppendNonNullValue(
        Boolean value,
        Byte[] dataArray,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        fixed (Byte* buf = dataArray)
        {
            // First byte is non-zero for defined values.
            *buf = embedType;

            // Zeroing memory.
            *(UInt64*) (buf + 1) = 0;

            // When TRUE first byte is 1.
            if (value == true)
            {
                *(buf + 1) = 1;
            }
            else
            {
                *(buf + 1) = 0;
            }
        }
    }

    internal void Append(
        Nullable<Boolean> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        AppendNonNullValue(value.Value, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<Boolean> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 }; // Represents undefined value.
            return dataArrayForNull;
        }

        Byte[] dataArray = new Byte[9];
        AppendNonNullValue(value.Value, dataArray, embedType);
        return dataArray;
    }

    ///////////////////////////////////////////////////////////
    // Group of functions for appending datetime to the key. //
    ///////////////////////////////////////////////////////////

    internal void Append(
        Nullable<DateTime> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        // Using UInt64 function.
        AppendNonNullValue(value.Value.Ticks, embedType);
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        Nullable<DateTime> value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 };
            return dataArrayForNull;
        }
        Byte[] dataArray = new Byte[9];

        // Using UInt64 function.
        AppendNonNullValue(value.Value.Ticks, dataArray, embedType);
        return dataArray;
    }

    ///////////////////////////////////////////////////////////////////
    // Group of functions for appending object reference to the key. //
    ///////////////////////////////////////////////////////////////////
    
    internal void Append(
        IObjectView value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        // First byte is non-zero for defined values.
        dataBuffer[position] = embedType;
        position++;
        
        // Next eight bytes represent the value.
        Byte[] valueArr;
        if (value is MaxValueObject)
        {
            valueArr = BitConverter.GetBytes(UInt64.MaxValue);
        }
        else
        {
            valueArr = BitConverter.GetBytes(value.Identity);
        }
        
        Buffer.BlockCopy(valueArr, 0, dataBuffer, position, 8);
        position += 8;
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal static Byte[] PrecomputeBuffer(
        IObjectView value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 };
            return dataArrayForNull;
        }
        Byte[] dataArray = new Byte[9];

        // First byte is non-zero for defined values.
        dataArray[0] = embedType;

        // Next eight bytes represent the value.
        Byte[] valueArr;
        if (value is MaxValueObject)
        {
            valueArr = BitConverter.GetBytes(UInt64.MaxValue);
        }
        else
        {
            valueArr = BitConverter.GetBytes(value.Identity);
        }
        Buffer.BlockCopy(valueArr, 0, dataArray, 1, 8);
        return dataArray;
    }

    /////////////////////////////////////////////////////////
    // Group of functions for appending string to the key. //
    /////////////////////////////////////////////////////////

    internal unsafe void Append(
        String value,
        Boolean appendMaxChar,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        UInt32 flags = 0, errorCode = 0;
        if (appendMaxChar)
        {
            flags += 1;  // TODO: Will SC_APPEND_INFINITE_CHAR be represented by value 1?
        }

        // First byte is non-zero for defined values.
        dataBuffer[position] = embedType;
        position++;

        Int32 outputLen = -1;
        fixed (Byte *buf = dataBuffer)
        {
            errorCode = sccoredb.star_convert_ucs2_to_turbotext(value, flags, buf + position, (UInt32)(SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES - position));
            outputLen = *(Int32 *)(buf + position); // Calculating output string length.
        }

        if (errorCode != 0)
        {
            throw ErrorCode.ToException(errorCode);
        }

        // 4 bytes for string length.
        position += (outputLen + 4);
    }

    internal unsafe void Append_Setspec(string value, bool appendInfiniteChar) {
        // Checking if value is undefined.
        if (value == null) {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        uint flags = appendInfiniteChar ? 1U : 0U;

        dataBuffer[position] = 1; // First byte is non-zero for defined values.
        position++;

        uint r;
        int outputLen;
        fixed (byte *buf = dataBuffer) {
            r = sccoredb.star_convert_ucs2_to_setspectt(
                value, flags, buf + position,
                (uint)(SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES - position)
                );
            outputLen = *(int *)(buf + position); // Calculating output string length.
        }

        if (r == 0) {
            position += (outputLen + 4); // 4 bytes for string length.
        }
        else {
            throw ErrorCode.ToException(r);
        }
    }

    /// <summary>
    /// Precomputes the buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="appendMaxChar">The append max char.</param>
    /// <param name="embedType">Type of the embed.</param>
    /// <returns>Byte[][].</returns>
    internal unsafe static Byte[] PrecomputeBuffer(
        String value,
        Boolean appendMaxChar,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            Byte[] dataArrayForNull = { 0 };
            return dataArrayForNull;
        }
        
        UInt32 flags = 0;
        if (appendMaxChar)
        {
            flags += 1;  // TODO: Will SC_APPEND_INFINITE_CHAR be represented by value 1?
        }
        
        Byte[] dataArray = null;

        Byte *valuePtr = null;
        UInt32 errorCode = sccoredb.star_context_convert_ucs2_to_turbotext(
            ThreadData.ContextHandle, value, flags, &valuePtr
            );

        if (errorCode != 0)
        {
            throw ErrorCode.ToException(errorCode);
        }

        Int32 length = *(Int32 *)valuePtr;

        // 1 byte for defined value + 4 bytes for length.
        dataArray = new Byte[length + 5];

        // Copying length and string data.
        Marshal.Copy((IntPtr)valuePtr, dataArray, 1, length + 4);

        // First byte is non-zero for defined values.
        dataArray[0] = embedType;

        return dataArray;
    }

    internal unsafe void AppendUnicodeString(
        String value,
        Byte embedType = SqlConnectivityInterface.QUERY_VARTYPE_DEFINED)
    {
        // Checking if value is undefined.
        if (value == null)
        {
            dataBuffer[position] = 0;
            position++;
            return;
        }

        // First byte is non-zero for defined values.
        dataBuffer[position] = embedType;
        position++;

        // Converting string to bytes.
        Int32 outLenBytes = Encoding.Unicode.GetBytes(value, 0, value.Length, dataBuffer, position + 4);

        // Writing length.
        fixed (Byte* buf = dataBuffer)
        {
            // Copying length.
            *(Int32*)(buf + position) = outLenBytes;
        }

        // 4 bytes for string length.
        position += (outLenBytes + 4);
    }

#if DEBUG
    internal bool AssertEquals(ByteArrayBuilder other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check basic types
        Debug.Assert(this.position == other.position);
        if (this.position != other.position)
            return false;
        // Check basic collections
        Debug.Assert(this.dataBuffer.Length == other.dataBuffer.Length);
        if (this.dataBuffer.Length != other.dataBuffer.Length)
            return false;
        for (int i = 0; i < this.dataBuffer.Length; i++) {
            Debug.Assert(this.dataBuffer[i] == other.dataBuffer[i]);
            if (this.dataBuffer[i] != other.dataBuffer[i])
                return false;
        }
        return true;

    }
#endif
}
}