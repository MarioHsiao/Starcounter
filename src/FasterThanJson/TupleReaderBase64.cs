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
       internal const int OffsetElementSizeSize = 1; // The tuple begins with an integer telling the size. The size of this integer is always 1 byte.
       
       /// <summary>
       /// Offset integer pointing to the end of the tuple with 0 being the beginning of the value count
       /// Kept to speed up writing of offsets into the offset list
       /// </summary>
      public UInt32 ValueOffset;

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
         OffsetElementSize = Base16Int.ReadBase16x1((Base16x1*)AtStart); // The first byte in the tuple tells the offset element size of the tuple
         AtEnd = AtStart + OffsetElementSizeSize + valueCount * OffsetElementSize;

      }

       /// <summary>
       /// Gets pointer to and lenght of the value at the given position
       /// </summary>
       /// <param name="index">Position of the value in the tuple</param>
       /// <param name="valuePos">The pointer to the value</param>
       /// <param name="valueLength">The length of the value</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      private unsafe void GetAtPosition(int index, out byte* valuePos, out int valueLength) {
#if BASE64
          if (index >= ValueCount)
              throw ErrorCode.ToException(Error.SCERRTUPLEOUTOFRANGE, "Cannot read value since the index is out of range");
          int firstValue = OffsetElementSizeSize + (int)(ValueCount * OffsetElementSize);
          // Get value position
          int valueOffset;
          if (index == 0) {
              valueOffset = 0;
              valuePos = AtStart + firstValue;
          } else {
              int offsetPos = OffsetElementSizeSize + (int)((index - 1) * OffsetElementSize);
              byte* atOffset = AtStart + offsetPos;
              valueOffset = (int)Base64Int.Read(OffsetElementSize, atOffset);
              valuePos = AtStart + firstValue + valueOffset;
          }
          // Get value length
          byte* nextOffsetPos = AtStart + OffsetElementSizeSize + index * OffsetElementSize;
          int nextOffset = (int)Base64Int.Read(OffsetElementSize, nextOffsetPos);
          valueLength = nextOffset - valueOffset;
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
#endif
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
      /// Reads the U int.
      /// </summary>
      /// <returns>System.UInt32.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available from .NET framework version 4.5
      public unsafe ulong ReadUInt() {
#if BASE32
         int len = (int)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = (uint)Base32Int.Read(len, (IntPtr)AtEnd);
#endif
#if BASE64
         int len = (int)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = Base64Int.Read(len, AtEnd);
#endif
#if BASE256
         int len = (int)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = (uint)Base256Int.Read(len, (IntPtr)AtEnd);
#endif
         ValueOffset += (uint)len;
         AtOffsetEnd += OffsetElementSize;
         AtEnd += len;
         return ret;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available from .NET framework version 4.5
      public unsafe ulong ReadUInt(int index) {
#if BASE64
          byte* valuePos;
          int valueLength;
          GetAtPosition(index, out valuePos, out valueLength);
          // Read the value at the position with the length
          var ret = Base64Int.Read(valueLength, valuePos);
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
#endif
          return ret;
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
         uint len = (uint)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
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
      public unsafe string ReadString(int valueLength, byte* start) {
          Debug.Assert(valueLength >= 0);
          if (valueLength > 1) {
              char* buffer = stackalloc char[(int)valueLength];
              int stringLen = SessionBlobProxy.Utf8Decode.GetChars(start + 1, valueLength - 1, buffer, valueLength, true);
              return new String(buffer, 0, stringLen);
          }
          if (valueLength == 0)
              return "";
          // if (valueLength == 1)
          return null;
      }

      /// <summary>
      /// Reads the next string from the tuple
      /// </summary>
      /// <returns>System.String.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe string ReadString()
      {
#if BASE32
         uint len = (uint)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
#if BASE64
         uint valueLength = (uint)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
#endif
#if BASE256
         uint len = (uint)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
         valueLength -= ValueOffset;
         String str = ReadString((int)valueLength, AtEnd);
         AtEnd += valueLength;
         ValueOffset += valueLength;
         AtOffsetEnd += OffsetElementSize;
         return str;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe string ReadString(int index) {
#if BASE64
          byte* valuePos;
          int valueLength;
          GetAtPosition(index, out valuePos, out valueLength);
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
#endif
          String str = ReadString(valueLength, valuePos);
          return str;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe byte[] ReadByteArray() {
#if BASE64
          uint len = (uint)Base64Int.Read(OffsetElementSize, AtOffsetEnd);
          len -= ValueOffset;
          //uint valueLength = Base64Binary.MeasureNeededSizeToDecode(len);
          byte[] value = Base64Binary.Read(len, AtEnd);
#else
          throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
#endif
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          ValueOffset += len;
          return value;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe byte[] ReadByteArray(int index) {
#if BASE64
          byte* valuePos;
          int len;
          GetAtPosition(index, out valuePos, out len);
          //uint valueLength = Base64Binary.MeasureNeededSizeToDecode((uint)len);
          byte[] value = Base64Binary.Read((uint)len, valuePos);
#else
          throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
#endif
          return value;
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
