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
   public unsafe struct TupleReader
   {
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
      /// Initializes a new instance of the <see cref="TupleReader" /> struct.
      /// </summary>
      /// <param name="start">The start.</param>
      /// <param name="valueCount">The value count.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public TupleReader(byte* start, uint valueCount)
      {
         AtStart = start;
         AtOffsetEnd = AtStart + 1;
         ValueOffset = 0;
         ValueCount = valueCount;
         OffsetElementSize = Base16Int.ReadBase16x1((Base16x1*)AtStart); // The first byte in the tuple tells the offset element size of the tuple
         AtEnd = AtStart + 1 + valueCount * OffsetElementSize;

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
          int firstValue = 1+(int)(ValueCount * OffsetElementSize);
          // Get value position
          int valueOffset;
          if (index == 0) {
              valueOffset = 0;
              valuePos = AtStart + firstValue;
          } else {
              int offsetPos = 1+(int)((index-1) * OffsetElementSize);
              byte* atOffset = AtStart + offsetPos;
              valueOffset = (int)Base64Int.Read(OffsetElementSize, (IntPtr)atOffset);
              valuePos = AtStart + firstValue + valueOffset;
          }
          // Get value length
          byte* nextOffsetPos = AtStart + 1 + index * OffsetElementSize;
          int nextOffset = (int)Base64Int.Read(OffsetElementSize, (IntPtr)nextOffsetPos);
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
         var ret = (uint)Base64Int.ReadBase64x1((IntPtr)AtEnd);
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
      public unsafe uint ReadUInt() {
#if BASE32
         int len = (int)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = (uint)Base32Int.Read(len, (IntPtr)AtEnd);
#endif
#if BASE64
         int len = (int)Base64Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
         len -= (int)ValueOffset;
         var ret = (uint)Base64Int.Read(len, (IntPtr)AtEnd);
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
      public unsafe uint ReadUInt(int index) {
#if BASE64
          byte* valuePos;
          int valueLength;
          GetAtPosition(index, out valuePos, out valueLength);
          // Read the value at the position with the length
          var ret = (uint)Base64Int.Read(valueLength, (IntPtr)valuePos);
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
         uint len = (uint)Base64Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
#if BASE256
         uint len = (uint)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
         len -= ValueOffset;
         AtOffsetEnd += OffsetElementSize;
         AtEnd += len;
         ValueOffset += len;
      }

      /// <summary>
      /// Reads the next string from the tuple
      /// </summary>
      /// <returns>System.String.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe string ReadString()
      {
         char* buffer = stackalloc char[8192];
#if BASE32
         uint len = (uint)Base32Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
#if BASE64
         uint len = (uint)Base64Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
#if BASE256
         uint len = (uint)Base256Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
#endif
         len -= ValueOffset;
         Encoding.UTF8.GetChars(AtEnd, (int)len, buffer, 8192);
         var str = new String(buffer, 0, (int)len);
         AtOffsetEnd += OffsetElementSize;
         AtEnd += len;
         ValueOffset += len;
         return str;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe string ReadString(int index) {
          char* buffer = stackalloc char[8192];
#if BASE64
          byte* valuePos;
          int valueLength;
          GetAtPosition(index, out valuePos, out valueLength);
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
#endif
          Encoding.UTF8.GetChars(valuePos, valueLength, buffer, 8192);
          var str = new String(buffer, 0, valueLength);
          return str;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe byte[] ReadByteArray() {
#if BASE64
          uint len = (uint)Base64Int.Read(OffsetElementSize, (IntPtr)AtOffsetEnd);
          len -= ValueOffset;
          uint valueLength = Base64Binary.MeasureNeededSizeToDecode(len);
          byte[] value = Base64Binary.Read(len, (IntPtr)AtEnd);
#else
          throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
#endif
          AtOffsetEnd += OffsetElementSize;
          AtEnd += len;
          ValueOffset += len;
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
            int len = (int)Base64Int.Read( OffsetElementSize, (IntPtr)(AtStart + 1) );
#endif
#if BASE256
            int len = (int) Base256Int.Read(OffsetElementSize, (IntPtr)(AtStart + 1));
#endif
            len += TupleWriter.OffsetElementSizeSize + OffsetElementSize;
            var buffer = new byte[len];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr) AtStart, buffer, 0, len);

#if BASE256
            return Base256Int.FixString(buffer);
#else
         return Encoding.UTF8.GetString(buffer);
#endif
         }
      }

   }
}
