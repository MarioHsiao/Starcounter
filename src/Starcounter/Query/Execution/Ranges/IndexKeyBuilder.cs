// ***********************************************************************
// <copyright file="ByteArrayBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Internal;

namespace Starcounter.Query.Execution {

    internal sealed class IndexKeyBuilder {

        // Copying one key builder data to another.
        internal void CopyToAnotherByteArray(IndexKeyBuilder anotherByteArray) {
            if (anotherByteArray.position == position) {
                return;  // If positions are the same we don't copy anything.
            }
            Buffer.BlockCopy(dataBuffer, 0, anotherByteArray.dataBuffer, 0, position);
            anotherByteArray.position = position;
        }

        private Byte[] dataBuffer; // Data array.
        private Int32 position; // The current position in the buffer where data should be appended.

        internal IndexKeyBuilder() {
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
                errorCode = sccoredb.star_convert_ucs2_to_turbotext(value, flags, buf + position, (UInt32)(SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES - position));
                outputLen = *(Int32*)(buf + position); // Calculating output string length.
            }

            if (errorCode != 0) {
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
            fixed (byte* buf = dataBuffer) {
                r = sccoredb.star_convert_ucs2_to_setspectt(
                    value, flags, buf + position,
                    (uint)(SqlConnectivityInterface.RECREATION_KEY_MAX_BYTES - position)
                    );
                outputLen = *(int*)(buf + position); // Calculating output string length.
            }

            if (r == 0) {
                position += (outputLen + 4); // 4 bytes for string length.
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }
    }
}