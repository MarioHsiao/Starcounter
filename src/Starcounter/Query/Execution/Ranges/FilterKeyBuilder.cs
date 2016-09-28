// ***********************************************************************
// <copyright file="ByteArrayBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using Starcounter.Internal;

namespace Starcounter.Query.Execution {

    internal sealed class FilterKeyBuilder {

        // Appending precomputed byte keyDataBuffer to the end of the data stream.
        internal void AppendPrecomputedBuffer(Byte[] precompBuffer) {
            Buffer.BlockCopy(precompBuffer, 0, dataBuffer, position, precompBuffer.Length);
            position += precompBuffer.Length;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<Int64> value) {
            if (value == null) {
                Byte[] dataArrayForNull = { 0 }; // Undefined value.
                return dataArrayForNull;
            }

            Byte[] dataArray = new Byte[9];
            AppendNonNullValue(value.Value, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<UInt64> value) {
            if (value == null) {
                Byte[] dataArrayForNull = { 0 }; // Undefined value.
                return dataArrayForNull;
            }

            Byte[] dataArray = new Byte[9];
            AppendNonNullValue(value.Value, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<Double> value) {
            if (value == null) {
                Byte[] dataArrayForNull = { 0 }; // Undefined value.
                return dataArrayForNull;
            }

            Byte[] dataArray = new Byte[9];
            AppendNonNullValue(value.Value, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<Decimal> value) {
            if (value == null) {
                Byte[] dataArrayForNull = { 0 }; // Undefined value.
                return dataArrayForNull;
            }

            Byte[] dataArray = new Byte[17]; // Decimal is 128-bits long.
            AppendNonNullValue(value.Value, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<Binary> value) {
            if (value == null) {
                Byte[] dataArrayForNull = { 0 }; // Represents undefined value.
                return dataArrayForNull;
            }

            Int32 totalLengthBytes = value.Value.GetLength() + 5;
            Byte[] dataArray = new Byte[totalLengthBytes + 1];
            AppendNonNullValue(value.Value, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<Boolean> value) {
            if (value == null) {
                Byte[] dataArrayForNull = { 0 }; // Represents undefined value.
                return dataArrayForNull;
            }

            Byte[] dataArray = new Byte[9];
            AppendNonNullValue(value.Value, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(Nullable<DateTime> value) {
            // Checking if value is undefined.
            if (value == null) {
                Byte[] dataArrayForNull = { 0 };
                return dataArrayForNull;
            }
            Byte[] dataArray = new Byte[9];

            // Using UInt64 function.
            AppendNonNullValue(value.Value.Ticks, dataArray);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] PrecomputeBuffer(IObjectView value) {
            // Checking if value is undefined.
            if (value == null) {
                Byte[] dataArrayForNull = { 0 };
                return dataArrayForNull;
            }
            Byte[] dataArray = new Byte[9];

            // First byte is non-zero for defined values.
            dataArray[0] = 1;

            // Next eight bytes represent the value.
            Byte[] valueArr;
            if (value is MaxValueObject) {
                valueArr = BitConverter.GetBytes(UInt64.MaxValue);
            }
            else {
                valueArr = BitConverter.GetBytes(value.Identity);
            }
            Buffer.BlockCopy(valueArr, 0, dataArray, 1, 8);
            return dataArray;
        }

        /// <summary>
        /// Precomputes the buffer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="appendMaxChar">The append max char.</param>
        /// <returns>Byte[][].</returns>
        internal unsafe static Byte[] PrecomputeBuffer(
            String value, Boolean appendMaxChar) {
            // Checking if value is undefined.
            if (value == null) {
                Byte[] dataArrayForNull = { 0 };
                return dataArrayForNull;
            }

            UInt32 flags = 0;
            if (appendMaxChar) {
                flags += 1;  // TODO: Will SC_APPEND_INFINITE_CHAR be represented by value 1?
            }

            Byte[] dataArray = null;

            Byte* valuePtr = null;
            UInt32 errorCode = sccoredb.star_context_convert_ucs2_to_turbotext(
                ThreadData.ContextHandle, value, flags, &valuePtr
                );

            if (errorCode != 0) {
                throw ErrorCode.ToException(errorCode);
            }

            Int32 length = *(Int32*)valuePtr;

            // 1 byte for defined value + 4 bytes for length.
            dataArray = new Byte[length + 5];

            // Copying length and string data.
            Marshal.Copy((IntPtr)valuePtr, dataArray, 1, length + 4);

            // First byte is non-zero for defined values.
            dataArray[0] = 1;

            return dataArray;
        }

        private Byte[] dataBuffer; // Data array.
        private Int32 position; // The current position in the buffer where data should be appended.

        internal FilterKeyBuilder() {
            dataBuffer = new Byte[SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES];
            position = 4; // Because size of the data stream is in first 4 bytes.
        }

        // Reseting buffer for further re-usage.
        internal void ResetCached() {
            position = 4; // Because size of the data stream is in first 4 bytes.
        }

        // Getting key data stream.
        /// <summary>
        /// Gets the buffer cached.
        /// </summary>
        /// <returns>Byte[][].</returns>
        internal unsafe Byte[] GetBufferCached() {
            // First four bytes represent the total length of the key.
            fixed (Byte* buf = dataBuffer) {
                *(Int32*)buf = position;
            }
            return dataBuffer;
        }

        private void AppendNullValue() {
            dataBuffer[position] = 0;
            position++;
        }

        ////////////////////////////////////////////////////////////////
        // Group of functions for appending int64 to the data stream. //
        ////////////////////////////////////////////////////////////////

        private unsafe void AppendNonNullValue(Int64 value) {
            fixed (Byte* buf = dataBuffer) {
                // First byte is non-zero for defined values.
                *(buf + position) = 1;

                // Copying actual data bytes.
                *(Int64*)(buf + position + 1) = value;
            }

            position += 9;
        }

        private unsafe static void AppendNonNullValue(Int64 value, Byte[] dataArray) {
            fixed (Byte* buf = dataArray) {
                // First byte is non-zero for defined values.
                *buf = 1;

                // Copying actual data bytes.
                *(Int64*)(buf + 1) = value;
            }
        }

        internal void Append(Nullable<Int64> value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            AppendNonNullValue(value.Value);
        }

        /////////////////////////////////////////////////////////
        // Group of functions for appending uint64 to the key. //
        /////////////////////////////////////////////////////////

        private unsafe void AppendNonNullValue(UInt64 value) {
            fixed (Byte* buf = dataBuffer) {
                // First byte is non-zero for defined values.
                *(buf + position) = 1;

                // Copying actual data bytes.
                *(UInt64*)(buf + position + 1) = value;
            }

            position += 9;
        }

        private unsafe static void AppendNonNullValue(UInt64 value, Byte[] dataArray) {
            fixed (Byte* buf = dataArray) {
                // First byte is non-zero for defined values.
                *buf = 1;

                // Copying actual data bytes.
                *(UInt64*)(buf + 1) = value;
            }
        }

        internal void Append(Nullable<UInt64> value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            AppendNonNullValue(value.Value);
        }

        //////////////////////////////////////////////////////////
        // Group of functions for appending decimal to the key. //
        //////////////////////////////////////////////////////////

        private unsafe void AppendNonNullValue(Decimal value) {
            Int64 value2 = X6Decimal.ToEncoded(value);
            AppendNonNullValue(value2);
        }

        private unsafe static void AppendNonNullValue(Decimal value, Byte[] dataArray) {
            Int64 value2 = X6Decimal.ToEncoded(value);
            AppendNonNullValue(value2, dataArray);
        }

        internal void Append(Nullable<Decimal> value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            AppendNonNullValue(value.Value);
        }

        /////////////////////////////////////////////////////////
        // Group of functions for appending binary to the key. //
        /////////////////////////////////////////////////////////

        private void AppendNonNullValue(Binary value) {
            // First byte is non-zero for defined values.
            dataBuffer[position] = 1;
            position++;

            var valueLen = value.GetLength();
            var adjustedLen = valueLen + 5;
            Buffer.BlockCopy(value.GetInternalBuffer(), 0, dataBuffer, position, adjustedLen);
            position += adjustedLen;
        }

        private static void AppendNonNullValue(Binary value, Byte[] dataArray) {
            // First byte is non-zero for defined values.
            dataArray[0] = 1;

            var valueLen = value.GetLength();
            var adjustedLen = valueLen + 5;
            Buffer.BlockCopy(value.GetInternalBuffer(), 0, dataArray, 1, adjustedLen);
        }

        internal void Append(Nullable<Binary> value) {
            if (value == null) {
                AppendNullValue();
                return;
            }

            AppendNonNullValue(value.Value);
        }

        //////////////////////////////////////////////////////////
        // Group of functions for appending boolean to the key. //
        //////////////////////////////////////////////////////////

        private unsafe void AppendNonNullValue(Boolean value) {
            fixed (Byte* buf = dataBuffer) {
                // First byte is non-zero for defined values.
                *(buf + position) = 1;
                position++;

                // Zeroing memory.
                *(UInt64*)(buf + position) = 0;

                // When TRUE first byte is 1.
                if (value == true) {
                    *(buf + position) = 1;
                }
                else {
                    *(buf + position) = 0;
                }
            }

            position += 8;
        }

        private unsafe static void AppendNonNullValue(Boolean value, Byte[] dataArray) {
            fixed (Byte* buf = dataArray) {
                // First byte is non-zero for defined values.
                *buf = 1;

                // Zeroing memory.
                *(UInt64*)(buf + 1) = 0;

                // When TRUE first byte is 1.
                if (value == true) {
                    *(buf + 1) = 1;
                }
                else {
                    *(buf + 1) = 0;
                }
            }
        }

        internal void Append(Nullable<Boolean> value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            AppendNonNullValue(value.Value);
        }

        ///////////////////////////////////////////////////////////
        // Group of functions for appending datetime to the key. //
        ///////////////////////////////////////////////////////////

        internal void Append(Nullable<DateTime> value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            // Using UInt64 function.
            AppendNonNullValue(value.Value.Ticks);
        }

        ///////////////////////////////////////////////////////////////////
        // Group of functions for appending object reference to the key. //
        ///////////////////////////////////////////////////////////////////

        internal void Append(IObjectView value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            // First byte is non-zero for defined values.
            dataBuffer[position] = 1;
            position++;

            // Next eight bytes represent the value.
            Byte[] valueArr;
            if (value is MaxValueObject) {
                valueArr = BitConverter.GetBytes(UInt64.MaxValue);
            }
            else {
                valueArr = BitConverter.GetBytes(value.Identity);
            }

            Buffer.BlockCopy(valueArr, 0, dataBuffer, position, 8);
            position += 8;
        }

        /////////////////////////////////////////////////////////
        // Group of functions for appending string to the key. //
        /////////////////////////////////////////////////////////

        internal unsafe void Append(String value, Boolean appendMaxChar) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            UInt32 flags = 0, errorCode = 0;
            if (appendMaxChar) {
                flags += 1;  // TODO: Will SC_APPEND_INFINITE_CHAR be represented by value 1?
            }

            // First byte is non-zero for defined values.
            dataBuffer[position] = 1;
            position++;

            Int32 outputLen = -1;
            fixed (Byte* buf = dataBuffer) {
                errorCode = sccoredb.star_convert_ucs2_to_turbotext_OLD(
                    value, flags, buf + position,
                    (UInt32)(SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES - position)
                    );
                outputLen = *(Int32*)(buf + position); // Calculating output string length.
            }

            if (errorCode != 0) {
                throw ErrorCode.ToException(errorCode);
            }

            // 4 bytes for string length.
            position += (outputLen + 4);
        }

        /////////////////////////////////////////////////////////
        // Group of functions for appending double to the key. //
        /////////////////////////////////////////////////////////

        private unsafe void AppendNonNullValue(Double value) {
            fixed (Byte* buf = dataBuffer) {
                // First byte is non-zero for defined values.
                *(buf + position) = 1;

                // Copying actual data bytes.
                *(Double*)(buf + position + 1) = value;
            }

            position += 9;
        }

        private unsafe static void AppendNonNullValue(Double value, Byte[] dataArray) {
            fixed (Byte* buf = dataArray) {
                // First byte is non-zero for defined values.
                *buf = 1;

                // Copying actual data bytes.
                *(Double*)(buf + 1) = value;
            }
        }

        internal void Append(Nullable<Double> value) {
            // Checking if value is undefined.
            if (value == null) {
                AppendNullValue();
                return;
            }

            AppendNonNullValue(value.Value);
        }
    }
}