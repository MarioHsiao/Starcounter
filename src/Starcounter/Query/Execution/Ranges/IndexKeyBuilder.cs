// ***********************************************************************
// <copyright file="ByteArrayBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using Starcounter.Internal;

namespace Starcounter.Query.Execution {

    internal sealed class IndexKeyBuilder {

        const byte S_UKN = 0x40;

        const byte S_SEP = 0x80;

        const byte S_EOK = 0xC0;

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
            position = 0;
        }

        // Reseting buffer for further re-usage.
        internal void ResetCached() {
            position = 0;
        }

        // Getting key data stream.
        /// <summary>
        /// Gets the buffer cached.
        /// </summary>
        /// <returns>Byte[][].</returns>
        internal unsafe Byte[] GetBufferCached() {
            dataBuffer[position++] = S_EOK;
            while ((position % 8) != 0) dataBuffer[position++] = 0;
            return dataBuffer;
        }

        private void AppendNullValue() {
            dataBuffer[position++] = S_UKN | 0x20;
        }

        private void RaiseNoBufferException() {
            throw new ArgumentException();
        }

        ////////////////////////////////////////////////////////////////
        // Group of functions for appending int64 to the data stream. //
        ////////////////////////////////////////////////////////////////

        [DllImport("sccoredb.dll")]
        private static extern unsafe void star_ukf_encode_long(long input, byte* output9);

        private unsafe void AppendNonNullValue(Int64 value) {
            int left = (dataBuffer.Length - position);
            if (left >= 9) {
                fixed (byte* data = dataBuffer) { star_ukf_encode_long(value, data + position); }
                position += 9;
            }
            else
                RaiseNoBufferException();
        }

        internal void Append(Nullable<Int64> value) {
            if (value != null) AppendNonNullValue(value.Value);
            else AppendNullValue();
        }

        /////////////////////////////////////////////////////////
        // Group of functions for appending uint64 to the key. //
        /////////////////////////////////////////////////////////

        [DllImport("sccoredb.dll")]
        private static extern unsafe void star_ukf_encode_ulong(ulong input, byte* output9);

        private unsafe void AppendNonNullValue(UInt64 value) {
            int left = (dataBuffer.Length - position);
            if (left >= 9) {
                fixed (byte* data = dataBuffer) { star_ukf_encode_ulong(value, data + position); }
                position += 9;
            }
            else
                RaiseNoBufferException();
        }

        internal void Append(Nullable<UInt64> value) {
            if (value != null) AppendNonNullValue(value.Value);
            else AppendNullValue();
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

        [DllImport("sccoredb.dll")]
        private static extern unsafe int star_ukf_encode_binaryx(
            byte* input, int in_len, byte* output, int out_len
            );

        private unsafe void AppendNonNullValue(Binary value) {
            var in_len = value.GetLength() + 1;
            int left = (dataBuffer.Length - position);
            int used;
            fixed (byte* output = dataBuffer) {
                fixed (byte* input = value.GetInternalBuffer()) {
                    used = star_ukf_encode_binaryx(input + 4, in_len, output + position, left);
                }
            }
            if (used <= left)
                position += used;
            else
                RaiseNoBufferException();
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
            AppendNonNullValue(value ? 1UL : 0UL);
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
            AppendNonNullValue((ulong)value.Value.Ticks);
        }

        ///////////////////////////////////////////////////////////////////
        // Group of functions for appending object reference to the key. //
        ///////////////////////////////////////////////////////////////////

        internal void Append(IObjectView value) {
            if (value == null) AppendNullValue();
            else AppendNonNullValue((value is MaxValueObject) ? ulong.MaxValue : value.Identity);
        }

        /////////////////////////////////////////////////////////
        // Group of functions for appending string to the key. //
        /////////////////////////////////////////////////////////

        [DllImport("sccoredb.dll", CharSet = CharSet.Unicode)]
        private static extern unsafe int star_ukf_encode_string(
            string input, int tt_conv_flags, byte* output, int out_len
            );

        internal unsafe void Append(String value, Boolean appendMaxChar) {
            if (value != null) {
                int tt_conv_flags = appendMaxChar ? 1 : 0;
                int left = (dataBuffer.Length - position);
                int used;
                fixed (byte* data = dataBuffer) {
                    used = star_ukf_encode_string(value, tt_conv_flags, data + position, left);
                }

                if (used <= left)
                    position += used;
                else
                    RaiseNoBufferException();
            }
            else AppendNullValue();
        }

        [DllImport("sccoredb.dll", CharSet = CharSet.Unicode)]
        private static extern unsafe int star_ukf_encode_setspec(
            string input, int tt_conv_flags, byte* output, int out_len
            );

        internal unsafe void Append_Setspec(string value, bool appendInfiniteChar) {
            if (value != null) {
                int tt_conv_flags = appendInfiniteChar ? 1 : 0;
                int left = (dataBuffer.Length - position);
                int used;
                fixed (byte* data = dataBuffer) {
                    used = star_ukf_encode_setspec(value, tt_conv_flags, data + position, left);
                }

                if (used <= left)
                    position += used;
                else
                    RaiseNoBufferException();
            }
            else AppendNullValue();
        }
    }
}