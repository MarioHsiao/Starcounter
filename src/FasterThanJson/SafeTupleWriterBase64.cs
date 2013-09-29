using System;
using System.Diagnostics;

namespace Starcounter.Internal {
    public unsafe struct SafeTupleWriterBase64 {
        /// <summary>
        /// The fast tuple storing the values.
        /// </summary>
        TupleWriterBase64 theTuple;
        /// <summary>
        /// Maximum possible size of tuple in bytes if set by user
        /// </summary>
        public uint TupleMaxLength;

        /// <summary>
        /// The available size left (in bytes)
        /// </summary>
        public uint AvailableSize;

        public byte* AtEnd { get { return theTuple.AtEnd; } }

        public int Length { get { return theTuple.Length; } }

        /// <summary>
        /// Method
        /// </summary>
        /// <param name="start"></param>
        /// <param name="valueCount"></param>
        /// <param name="offsetElementSize"></param>
        public SafeTupleWriterBase64(byte* start, uint valueCount, uint offsetElementSize, uint length) {
            theTuple = new TupleWriterBase64(start, valueCount, offsetElementSize);
            if (length >= Math.Pow(64, 5))
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Maximum length of a tuple cannot be bigger than 64^5.");
            if (theTuple.AtEnd - theTuple.AtStart >= length)
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Too small length of the tuple");
            TupleMaxLength = length;
            AvailableSize = length;
            AvailableSize -= (uint)(theTuple.AtEnd - theTuple.AtStart);
        }

        /// <summary>
        /// Checks if string value fits the tuple and writes it
        /// </summary>
        /// <param name="value">String to write</param>
        private uint ValidateLength(uint expectedLen) {
            if (theTuple.ValuesWrittenSoFar() == theTuple.ValueCount)
                throw ErrorCode.ToException(Error.SCERRTUPLEOUTOFRANGE, "Cannot write since the index will be out of range.");
            uint neededOffsetSize = Base64Int.MeasureNeededSize((ulong)(theTuple.ValueOffset + expectedLen));
            if (theTuple.OffsetElementSize < neededOffsetSize)
                expectedLen += theTuple.MoveValuesRightSize(neededOffsetSize);
            if (expectedLen > AvailableSize)
                throw ErrorCode.ToException(Error.SCERRTUPLEVALUETOOBIG, "The value to write requires " + 
                    expectedLen + " bytes, while " + AvailableSize + " bytes are available.");
            return expectedLen;
        }

        /// <summary>
        /// Finalizes writing the tuple, checks if all values were written and
        /// returns length of the tuple.
        /// </summary>
        /// <returns>The length of the tuple.</returns>
        public uint SealTuple() {
            var nrValues = theTuple.ValuesWrittenSoFar();
            if (theTuple.ValueCount != nrValues)
                throw ErrorCode.ToException(Error.SCERRTUPLEINCOMPLETE, nrValues +
                    " values in the tuple with length " + theTuple.ValueCount);
            return theTuple.SealTuple();
        }

        /// <summary>
        /// Writes that nested tuple was written at the current position.
        /// Checks if writing will fit the tuple. Thus expensive.
        /// Then moves to the end of written area in the tuple, i.e., to the place
        /// where next value will be written.
        /// </summary>
        /// <param name="len">The length of written nested tuple.</param>
        public unsafe void HaveWritten(uint len) {
            var size = ValidateLength(len);
            theTuple.HaveWritten(len);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The string value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeString(char[] str) {
            if (str == null)
                return 1; // null flag
            uint expectedLen = 1; // null flag
            if (str.Length > 0)
                fixed (char* pStr = str) {
                    expectedLen += (uint)SessionBlobProxy.Utf8Encode.GetByteCount(pStr, str.Length, true);
                }
            return expectedLen;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The string value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeString(String str) {
            if (str == null)
                return 1; // null flag
            uint expectedLen = 1; // null flag
            fixed (char* pStr = str) {
                expectedLen += (uint)SessionBlobProxy.Utf8Encode.GetByteCount(pStr, str.Length, true);
            }
            return expectedLen;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeULong(ulong n) {
            return Base64Int.MeasureNeededSize(n);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeNullableULong(ulong? n) {
            return Base64Int.MeasureNeededSizeNullable(n);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeLong(long n) {
            if (n >= 0)
                return Base64Int.MeasureNeededSize((ulong)n << 1);
            return Base64Int.MeasureNeededSize(((ulong)(-(n + 1)) << 1) + 1);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeNullableLong(long? n) {
            if (n == null)
                return 1;
            if (n >= 0)
                return Base64Int.MeasureNeededSizeNullable((ulong)n << 1);
            else
                return Base64Int.MeasureNeededSizeNullable(((ulong)(-(n + 1)) << 1) + 1);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The byte array value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeByteArray(uint length) {
            return Base64Binary.MeasureNeededSizeToEncode(length);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The byte array value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static uint MeasureNeededSizeByteArray(byte[] b) {
            return MeasureNeededSizeByteArray((uint)b.Length);
        }


        public void WriteULong(ulong n) {
            uint size = MeasureNeededSizeULong(n);
            size = ValidateLength(size);
            theTuple.WriteULong(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteLong(long n) {
            uint size = MeasureNeededSizeLong(n);
            size = ValidateLength(size);
            theTuple.WriteLong(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteULongNullable(ulong? n) {
            uint size = MeasureNeededSizeNullableULong(n);
            size = ValidateLength(size);
            theTuple.WriteULongNullable(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteLongNullable(long? n) {
            uint size = MeasureNeededSizeNullableLong(n);
            size = ValidateLength(size);
            theTuple.WriteLongNullable(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteString(String str) {
            uint size = MeasureNeededSizeString(str);
            uint writeSize = ValidateLength(size);
            uint len = 1;
            if (str == null)
                Base64Int.WriteBase64x1(1, theTuple.AtEnd); // Write null flag
            else {
                Base64Int.WriteBase64x1(0, theTuple.AtEnd); // Write null flag
                fixed (char* pStr = str) {
                    // Write the string to the end of this tuple.
                    len += (uint)SessionBlobProxy.Utf8Encode.GetBytes(pStr, str.Length, theTuple.AtEnd + 1, (int)size - 1, true);
                }
            }
            Debug.Assert(len == size);
            theTuple.HaveWritten(len);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= writeSize;
        }

        public unsafe void WriteByteArray(byte* b, uint length) {
            uint size = MeasureNeededSizeByteArray(length);
            size = ValidateLength(size);
            theTuple.WriteByteArray(b, length);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public unsafe void WriteByteArray(byte[] b) {
            uint size = 0;
            if (b == null)
                size = 1;
            else
                size = MeasureNeededSizeByteArray((uint)b.Length);
            size = ValidateLength(size);
            theTuple.WriteByteArray(b);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

    }
}
