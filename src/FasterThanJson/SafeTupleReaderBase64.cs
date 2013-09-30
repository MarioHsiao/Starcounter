using System;
using System.Diagnostics;

namespace Starcounter.Internal {
    public unsafe struct SafeTupleReaderBase64 {
        TupleReaderBase64 theTuple;

        public SafeTupleReaderBase64(byte* start, uint valueCount) {
            theTuple = new TupleReaderBase64(start, valueCount);
        }


        /// <summary>
        /// Gets pointer to and lenght of the value at the given position
        /// </summary>
        /// <param name="index">Position of the value in the tuple</param>
        /// <param name="valuePos">The pointer to the value</param>
        /// <param name="valueLength">The length of the value</param>
        private unsafe void GetAtPosition(int index, out byte* valuePos, out int valueLength) {
            if (index >= theTuple.ValueCount)
                throw ErrorCode.ToException(Error.SCERRTUPLEOUTOFRANGE, "Cannot read value since the index " +
                    index + " is out of range for this tuple with " + theTuple.ValueCount + " values.");
            int firstValue = TupleReaderBase64.OffsetElementSizeSize + (int)(theTuple.ValueCount * theTuple.OffsetElementSize);
            // Get value position
            int valueOffset;
            if (index == 0) {
                valueOffset = 0;
                valuePos = theTuple.AtStart + firstValue;
            } else {
                int offsetPos = TupleReaderBase64.OffsetElementSizeSize + (int)((index - 1) * theTuple.OffsetElementSize);
                byte* atOffset = theTuple.AtStart + offsetPos;
                valueOffset = (int)Base64Int.ReadSafe(theTuple.OffsetElementSize, atOffset);
                valuePos = theTuple.AtStart + firstValue + valueOffset;
            }
            // Get value length
            byte* nextOffsetPos = theTuple.AtStart + TupleReaderBase64.OffsetElementSizeSize + index * theTuple.OffsetElementSize;
            int nextOffset = (int)Base64Int.ReadSafe(theTuple.OffsetElementSize, nextOffsetPos);
            valueLength = nextOffset - valueOffset;
        }

        /// <summary>
        /// Returns length of the value at given position in the tuple.
        /// </summary>
        /// <param name="index">The position in the tuple</param>
        /// <returns>The length of the value.</returns>
        public unsafe int GetValueLength(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            return valueLength;
        }

        /// <summary>
        /// Calculates the pointer to the value at the given index in the tuple.
        /// </summary>
        /// <param name="index">The index of the value in the tuple.</param>
        /// <returns>The pointer.</returns>
        public unsafe byte* GetPosition(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            return valuePos;
        }

        /// <summary>
        /// Reads unsigned long integer at the given position of the tuple.
        /// The implementation cannot be used in performance critical applications,
        /// since it checks correctness of the data.
        /// </summary>
        /// <param name="index">Index of the value to read in this tuple.</param>
        /// <returns>The read value.</returns>
        public unsafe ulong ReadULong(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            // Read the value at the position with the length
            return Base64Int.ReadSafe(valueLength, valuePos);
        }

        /// <summary>
        /// Reads signed long integer at the given position of the tuple.
        /// The implementation cannot be used in performance critical applications,
        /// since it checks correctness of the data.
        /// </summary>
        /// <param name="index">Index of the value to read in this tuple.</param>
        /// <returns>The read value.</returns>
        public unsafe long ReadLong(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            // Read the value at the position with the length
            ulong ret = Base64Int.ReadSafe(valueLength, valuePos);
            return TupleReaderBase64.ConvertToLong(ret);
        }

        /// <summary>
        /// Reads nullable unsigned long integer at the given position of the tuple.
        /// The implementation cannot be used in performance critical applications,
        /// since it checks correctness of the data.
        /// </summary>
        /// <param name="index">Index of the value to read in this tuple.</param>
        /// <returns>The read value.</returns>
        public unsafe ulong? ReadULongNullable(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            // Read the value at the position with the length
            return Base64Int.ReadNullable(valueLength, valuePos);
        }

        /// <summary>
        /// Reads nullable signed long integer at the given position of the tuple.
        /// The implementation cannot be used in performance critical applications,
        /// since it checks correctness of the data.
        /// </summary>
        /// <param name="index">Index of the value to read in this tuple.</param>
        /// <returns>The read value.</returns>
        public unsafe long? ReadLongNullable(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            // Read the value at the position with the length
            ulong? ret = Base64Int.ReadNullable(valueLength, valuePos);
            if (ret == null)
                return null;
            else
                return TupleReaderBase64.ConvertToLong((ulong)ret);
        }

        /// <summary>
        /// Reads the string value at index position in the tuple.
        /// The implementation cannot be used in performance critical applications,
        /// since it checks correctness of the data.
        /// </summary>
        /// <param name="index">The value position in the tuple. </param>
        /// <returns>The read string. </returns>
        public unsafe string ReadString(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            String str = theTuple.ReadString(valueLength, valuePos);
            return str;
        }

        /// <summary>
        /// Reads the byte array value at index position in the tuple.
        /// The implementation cannot be used in performance critical applications,
        /// since it checks correctness of the data.
        /// </summary>
        /// <param name="index">The value position in the tuple. </param>
        /// <returns>The read byte array. </returns>
        public unsafe byte[] ReadByteArray(int index) {
            byte* valuePos;
            int len;
            GetAtPosition(index, out valuePos, out len);
            byte[] value = Base64Binary.Read((uint)len, valuePos);
            return value;
        }

        public unsafe int ReadByteArray(int index, byte* value, int valueMaxLength) {
            byte* valuePos;
            int len;
            GetAtPosition(index, out valuePos, out len);
            if (Base64Binary.MeasureNeededSizeToDecode((uint)len) > valueMaxLength)
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
                    "Cannot read byte array value into given byte array pointer, since the value is too big. The actual value is " +
                    len + " bytes, while " + valueMaxLength + " bytes are provided to write.");
            return Base64Binary.Read((uint)len, valuePos, value);
        }

        /// <summary>
        /// Reads Boolean at the given position of the tuple.
        /// </summary>
        /// <param name="index">Index of the value to read in this tuple.</param>
        /// <returns>The read value.</returns>
        public unsafe bool ReadBoolean(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            Debug.Assert(valueLength == 1);
            // Read the value at the position with the length
            return theTuple.ReadBoolean(valuePos);
        }

        /// <summary>
        /// Reads Nullable Boolean at the given position of the tuple.
        /// </summary>
        /// <param name="index">Index of the value to read in this tuple.</param>
        /// <returns>The read value.</returns>
        public unsafe bool? ReadBooleanNullable(int index) {
            byte* valuePos;
            int valueLength;
            GetAtPosition(index, out valuePos, out valueLength);
            Debug.Assert(valueLength == 1);
            // Read the value at the position with the length
            return theTuple.ReadBooleanNullable(valuePos);
        }
    }
}
