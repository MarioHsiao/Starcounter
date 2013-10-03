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
        public int TupleMaxLength;

        /// <summary>
        /// The available size left (in bytes)
        /// </summary>
        public int AvailableSize;

        public byte* AtEnd { get { return theTuple.AtEnd; } }

        public int Length { get { return theTuple.Length; } }

        /// <summary>
        /// Method
        /// </summary>
        /// <param name="start"></param>
        /// <param name="valueCount"></param>
        /// <param name="offsetElementSize"></param>
        public SafeTupleWriterBase64(byte* start, uint valueCount, int offsetElementSize, int length) {
            theTuple = new TupleWriterBase64(start, valueCount, offsetElementSize);
            if (length >= Math.Pow(64, 5))
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Maximum length of a tuple cannot be bigger than 64^5.");
            if (theTuple.AtEnd - theTuple.AtStart >= length)
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Too small length of the tuple");
            TupleMaxLength = length;
            AvailableSize = length;
            AvailableSize -= theTuple.Length;
        }

        /// <summary>
        /// Checks if string value fits the tuple and writes it
        /// </summary>
        /// <param name="value">String to write</param>
        private int ValidateLength(int expectedLen) {
            if (theTuple.ValuesWrittenSoFar() == theTuple.ValueCount)
                throw ErrorCode.ToException(Error.SCERRTUPLEOUTOFRANGE, "Cannot write since the index will be out of range.");
            int neededOffsetSize = Base64Int.MeasureNeededSize((ulong)(theTuple.ValueOffset + expectedLen));
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
        public int SealTuple() {
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
        public unsafe void HaveWritten(int len) {
            int size = ValidateLength(len);
            theTuple.HaveWritten(len);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The string value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeString(char[] str) {
            if (str == null)
                return 1; // null flag
            int expectedLen = 1; // null flag
            if (str.Length > 0)
                fixed (char* pStr = str) {
                    expectedLen += SessionBlobProxy.Utf8Encode.GetByteCount(pStr, str.Length, true);
                }
            return expectedLen;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The string value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeString(String str) {
            if (str == null)
                return 1; // null flag
            int expectedLen = 1; // null flag
            fixed (char* pStr = str) {
                expectedLen += SessionBlobProxy.Utf8Encode.GetByteCount(pStr, str.Length, true);
            }
            return expectedLen;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeULong(ulong n) {
            return Base64Int.MeasureNeededSize(n);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeNullableULong(ulong? n) {
            return Base64Int.MeasureNeededSizeNullable(n);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeLong(long n) {
            if (n >= 0)
                return Base64Int.MeasureNeededSize((ulong)n << 1);
            return Base64Int.MeasureNeededSize(((ulong)(-(n + 1)) << 1) + 1);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The integer value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeNullableLong(long? n) {
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
        public static int MeasureNeededSizeByteArray(int length) {
            return Base64Binary.MeasureNeededSizeToEncode(length);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The byte array value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeByteArray(byte[] b) {
            return MeasureNeededSizeByteArray(b.Length);
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The Boolean value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeBoolean(Boolean val) {
            return 1;
        }

        /// <summary>
        /// Estimates the size of encoding the value.
        /// </summary>
        /// <param name="str">The Nullable Boolean value to encode.</param>
        /// <returns>The estimated length. </returns>
        public static int MeasureNeededSizeNullableBoolean(Boolean? val) {
            return 1;
        }

        public static int MeasureNeededSizeDecimalLossless(Decimal val) {
            return Base64DecimalLossless.MeasureNeededSize(val);
        }

        public void WriteULong(ulong n) {
            int size = MeasureNeededSizeULong(n);
            size = ValidateLength(size);
            theTuple.WriteULong(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteLong(long n) {
            int size = MeasureNeededSizeLong(n);
            size = ValidateLength(size);
            theTuple.WriteLong(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteULongNullable(ulong? n) {
            int size = MeasureNeededSizeNullableULong(n);
            size = ValidateLength(size);
            theTuple.WriteULongNullable(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteLongNullable(long? n) {
            int size = MeasureNeededSizeNullableLong(n);
            size = ValidateLength(size);
            theTuple.WriteLongNullable(n);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public void WriteString(String str) {
            int size = MeasureNeededSizeString(str);
            int writeSize = ValidateLength(size);
            int len = 1;
            if (str == null)
                Base64Int.WriteBase64x1(1, theTuple.AtEnd); // Write null flag
            else {
                Base64Int.WriteBase64x1(0, theTuple.AtEnd); // Write null flag
                fixed (char* pStr = str) {
                    // Write the string to the end of this tuple.
                    len += SessionBlobProxy.Utf8Encode.GetBytes(pStr, str.Length, theTuple.AtEnd + 1, (int)size - 1, true);
                }
            }
            Debug.Assert(len == size);
            theTuple.HaveWritten(len);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= writeSize;
        }

        public unsafe void WriteByteArray(byte* b, int length) {
            int size = MeasureNeededSizeByteArray(length);
            size = ValidateLength(size);
            theTuple.WriteByteArray(b, length);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public unsafe void WriteByteArray(byte[] b) {
            int size = 0;
            if (b == null)
                size = 1;
            else
                size = MeasureNeededSizeByteArray(b.Length);
            size = ValidateLength(size);
            theTuple.WriteByteArray(b);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public unsafe void WriteBoolean(Boolean b) {
            int size = MeasureNeededSizeBoolean(b);
            Debug.Assert(size == 1);
            size = ValidateLength(size);
            theTuple.WriteBoolean(b);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }

        public unsafe void WriteBooleanNullable(Boolean? b) {
            int size = MeasureNeededSizeNullableBoolean(b);
            Debug.Assert(size == 1);
            size = ValidateLength(size);
            theTuple.WriteBooleanNullable(b);
            Debug.Assert(theTuple.AtEnd - theTuple.AtStart <= TupleMaxLength);
            AvailableSize -= size;
        }
    }
}
