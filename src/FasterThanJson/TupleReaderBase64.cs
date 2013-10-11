// ***********************************************************************
// <copyright file="TupleReader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Starcounter.Internal
{
    /// <summary>
    /// Struct TupleReader
    /// </summary>
   public unsafe struct TupleReaderBase64
   {
       public const int OffsetElementSizeSize = 1; // The tuple begins with an integer telling the size. The size of this integer is always 1 byte.
       
       /// <summary>
       /// Offset integer pointing to the end of the tuple with 0 being the beginning of the value count
       /// Kept to speed up writing of offsets into the offset list
       /// </summary>
      public Int32 ValueOffset;

      /// <summary>
      /// Counter remembering the number of values written
      /// </summary>
      public UInt32 ValueCount;

      /// <summary>
      /// Pointer to the start of this tuple
      /// </summary>
      public byte* AtStart;

      /// <summary>
      /// Pointer to the end of the offset list of the tuple
      /// </summary>
      public byte* AtOffsetEnd;

	   // TODO: 
	   // Should be renamed to Current.
      /// <summary>
      /// Pointer to the end of the tuple
      /// </summary>
      public byte* AtEnd;

      /// <summary>
      /// The offset element size
      /// </summary>
      public int OffsetElementSize;

      /// <summary>
      /// Initializes a new instance of the <see cref="TupleReaderBase64" /> struct.
      /// </summary>
      /// <param name="start">The start.</param>
      /// <param name="valueCount">The value count.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public TupleReaderBase64(byte* start, uint valueCount)
      {
         AtStart = start;
         AtOffsetEnd = AtStart + OffsetElementSizeSize;
         ValueOffset = 0;
         ValueCount = valueCount;
         OffsetElementSize = (int)Base16Int.ReadBase16x1((Base16x1*)AtStart); // The first byte in the tuple tells the offset element size of the tuple
         AtEnd = AtStart + OffsetElementSizeSize + valueCount * OffsetElementSize;

      }


      /// <summary>
      /// Reads an unsigned 4 bit integer
      /// </summary>
      /// <returns>System.UInt32.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe uint ReadQuartet()
      {
         ValueOffset++;
#if BASE32
         uint ret = (uint)Base16Int.ReadBase16x1((Base16x1*)AtEnd);
#endif
#if BASE64
         uint ret = (uint)Base16Int.ReadBase16x1((Base16x1*)AtEnd);
#endif
#if BASE256
         uint ret = (uint)(*((byte*)AtEnd));
#endif
         AtEnd++;
         return ret;
      }


      /// <summary>
      /// Reads an unsigned 5 bit integer
      /// </summary>
      /// <returns>System.UInt32.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe uint ReadQuintet()
      {
         ValueOffset++;
#if BASE32
         var ret = (uint)Base32Int.ReadBase32x1((IntPtr)AtEnd);
#endif
#if BASE64
         var ret = (uint)Base64Int.ReadBase64x1(AtEnd);
#endif
#if BASE256
         uint ret = (uint)(*((byte*)AtEnd));
#endif
         AtEnd++;
         return ret;
      }


      /// <summary>
      /// Reads the unsigned long integer.
      /// </summary>
      /// <returns>The read value.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available from .NET framework version 4.5
      public unsafe ulong ReadULong() {
#if BASE32
         int len = (int)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = (uint)Base32Int.Read(len, (IntPtr)AtEnd);
#endif
#if BASE64
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
         len -= ValueOffset;
         ulong ret = Base64Int.Read(len, AtEnd);
#endif
#if BASE256
         int len = (int)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = (uint)Base256Int.Read(len, (IntPtr)AtEnd);
#endif
         ValueOffset += len;
         AtOffsetEnd += OffsetElementSize;
         AtEnd += len;
         return ret;
      }

       /// <summary>
       /// Reads nullable unsigned long integer.
       /// </summary>
       /// <returns>The value</returns>
      public unsafe ulong? ReadULongNullable() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          ulong? ret = Base64Int.ReadNullable(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return ret;
      }

       /// <summary>
       /// Converst from unsigned long integer to singed long integer.
       /// </summary>
       /// <param name="uval">The input value</param>
       /// <returns>The converted value</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available from .NET framework version 4.5
      public static unsafe long ConvertToLong(ulong uval) {
          long ret = (long)(uval >> 1);
          if ((uval & 0x00000001) == 1)
              ret = -ret - 1;
          return ret;
      }

       /// <summary>
       /// Reads signed long integer.
       /// </summary>
       /// <returns>The read value</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available from .NET framework version 4.5
      public unsafe long ReadLong() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          ulong uval = Base64Int.Read(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return ConvertToLong(uval);
      }


       /// <summary>
       /// Reads nullable signed long integer.
       /// </summary>
       /// <returns>The read value.</returns>
      public unsafe long? ReadLongNullable() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          ulong? uval = Base64Int.ReadNullable(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          if (uval == null)
              return null;
          else
              return ConvertToLong((ulong)uval);
      }


      /// <summary>
      /// Skip one value
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void Skip()
      {
#if BASE32
         uint len = (uint)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
#if BASE64
         int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
#endif
#if BASE256
         uint len = (uint)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
         len -= ValueOffset;
         AtOffsetEnd += OffsetElementSize;
         AtEnd += len;
         ValueOffset += len;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe int ReadString(int valueLength, byte* start, char* value) {
          Debug.Assert(valueLength > 0);
          if (Base64Int.ReadBase64x1(start) == 0)
              return SessionBlobProxy.Utf8Decode.GetChars(start + 1, valueLength - 1, value, valueLength, true);
          else
              return -1;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe string ReadString(int valueLength, byte* start) {
          Debug.Assert(valueLength > 0);
          String str;
          if (Base64Int.ReadBase64x1(start) == 0) {
              char* buffer = stackalloc char[(int)valueLength];
              int stringLen = ReadString(valueLength, start, buffer);
              //int stringLen = SessionBlobProxy.Utf8Decode.GetChars(start + 1, valueLength - 1, buffer, valueLength, true);
              str = new String(buffer, 0, stringLen);
          } else
              str = null;
          return str;
      }

       /// <summary>
       /// Reads the next string value from the tuple into the given char array pointer.
       /// </summary>
       /// <param name="value">The pointer to read into.</param>
       /// <returns>The length of the array written. -1 means null value. </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe int ReadString(char* value) {
         int valueLength = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
         valueLength -= ValueOffset;
         int leng = ReadString(valueLength, AtEnd, value);
         AtEnd += valueLength;
         ValueOffset += valueLength;
         AtOffsetEnd += OffsetElementSize;
         return leng;
      }

      /// <summary>
      /// Reads the next string value from the tuple into the given char array.
      /// </summary>
      /// <param name="value">The char array to read into.</param>
      /// <returns>The length of the array written. -1 means null value. </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe int ReadString(char[] value) {
          fixed (char* valuePtr = value)
              return ReadString(valuePtr);
      }

      /// <summary>
      /// Reads the next string from the tuple and allocates the string in the heap.
      /// </summary>
      /// <returns>System.String.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe string ReadString()
      {
#if BASE32
         uint len = (uint)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
#if BASE64
         int valueLength = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
#endif
#if BASE256
         uint len = (uint)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
         valueLength -= ValueOffset;
         String str = ReadString(valueLength, AtEnd);
         AtEnd += valueLength;
         ValueOffset += valueLength;
         AtOffsetEnd += OffsetElementSize;
         return str;
      }

      /// <summary>
      /// Reads the next byte array value from the tuple into the given byte array pointer.
      /// </summary>
      /// <param name="value">The pointer to read into.</param>
      /// <returns>The length of the array written. -1 means null value. </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe int ReadByteArray(byte* value) {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          int writtenLen = Base64Binary.Read(len, AtEnd, value);
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          ValueOffset += len;
          return writtenLen;
      }

      /// <summary>
      /// Reads the next byte array value from the tuple into the given byte array.
      /// </summary>
      /// <param name="value">The array to read into.</param>
      /// <returns>The length of the array written. -1 means null value. </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe int ReadByteArray(byte[] value) {
          fixed (byte* valuePtr = value)
              return ReadByteArray(valuePtr);
      }

      /// <summary>
      /// Reads the next byte array value and allocates memory in the heap for the returned value.
      /// </summary>
      /// <returns>The read value. </returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe byte[] ReadByteArray() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          byte[] value = Base64Binary.Read(len, AtEnd);
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          ValueOffset += len;
          return value;
      }

       /// <summary>
       /// Reads next value as Boolean from the tuple.
       /// </summary>
       /// <returns>The read Boolean value.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe Boolean ReadBoolean() {
          var val = AnyBaseBool.ReadBoolean(AtEnd);
          AtOffsetEnd += OffsetElementSize;
          AtEnd++;
          ValueOffset++;
          return val;
      }

             /// <summary>
      /// Reads next value as Nullable Boolean from the tuple.
      /// </summary>
      /// <returns>The read Nullable Boolean value.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe Boolean? ReadBooleanNullable() {
          var val = AnyBaseBool.ReadBooleanNullable(AtEnd);
          AtOffsetEnd += OffsetElementSize;
          AtEnd++;
          ValueOffset++;
          return val;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe Decimal ReadDecimalLossless() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          decimal val = Base64DecimalLossless.Read(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return val;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe Decimal ReadX6Decimal() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          decimal val = Base64X6Decimal.Read(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return val;
      }

      public unsafe Decimal? ReadDecimalLosslessNullable() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          decimal? val = Base64DecimalLossless.ReadNullable(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return val;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe Double ReadDouble() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          double val = Base64Double.Read(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return val;
      }

      public unsafe Double? ReadDoubleNullable() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          double? val = Base64Double.ReadNullable(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return val;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe Single ReadSingle() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          Single val = Base64Single.Read(len, AtEnd);
          ValueOffset += len;
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          return val;
      }

      /// <summary>
       /// Returns the length of the current value to read.
       /// </summary>
       /// <returns>The length in bytes.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe int GetValueLength() {
          int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          return len;
      }

      /// <summary>
      /// Gets the read byte count.
      /// </summary>
      /// <value>The read byte count.</value>
      public int ReadByteCount
      {
          [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
          get
         {
            return (int)(AtEnd - AtStart);
         }
      }

      /// <summary>
      /// Gets the debug string.
      /// </summary>
      /// <value>The debug string.</value>
      public string DebugString
      {
         get
         {
#if BASE32
            int len = (int)Base32Int.Read( OffsetElementSize, (IntPtr)(AtStart + 1) );
#endif
#if BASE64
            int len = (int)Base64Int.Read( OffsetElementSize, (AtStart + 1) );
#endif
#if BASE256
            int len = (int) Base256Int.Read(OffsetElementSize, (IntPtr)(AtStart + 1));
#endif
            len += TupleWriterBase64.OffsetElementSizeSize + OffsetElementSize;
            var buffer = new byte[len];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)AtStart, buffer, 0, len);

#if BASE256
            return Base256Int.FixString(buffer);
#else
         return Encoding.UTF8.GetString(buffer);
#endif
         }
      }

   }
}
