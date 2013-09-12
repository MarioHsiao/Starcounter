// ***********************************************************************
// <copyright file="TupleReader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Starcounter.Internal
{
    /// <summary>
    /// 	<para>Multiple tuples are written to a single buffer (when used to hosts Apps, this buffer is a SessionBlob).</para>
    /// 	<para>
    /// 		<img style="WIDTH: 506px; HEIGHT: 387px" src="http://www.rebelslounge.com/res/sessiontuples/tuples.png" width="800" height="683"/>
    /// 	</para>
    /// </summary>
    /// <remarks>For each tuple you write, you create a TupleWriter. As alarming as this may sound, the generated machine code will be quite efficient in .NET (not tested on
    /// Mono, however). The TupleWriter is a small structure of integers. As it is a structure, everything will be on the stack. The .NET optimizer will make good use
    /// of register to host the structure member integers.
    /// <para>Each TupleWriter is created using the current write pointer of its parent TupleWriter.</para></remarks>
    /// <example>
    /// 	<para>
    /// The following examples assume that you have a pointer to the first byte in a buffer and another pointer to the last byte in this buffer.
    /// </para>
    /// 	<code title="Example" description="To create a tuple represening a person and three nested tuples representing three phone numbers of the person." lang="CS">
    /// // Nested tuples in JSON array notation:
    /// //
    /// // [ "Joachim",
    /// //   "Wester",
    /// //       [
    /// //       "08-54137731",
    /// //       "0702-424472"
    /// //      ],
    /// //   "United Kingdom"
    /// // ]
    ///  
    /// TupleWriter person = new TupleWriter(...);
    /// person.Write("Joachim");
    /// person.Write("Wester");
    /// TupleWriter phonenumbers = new TupleWriter(person.??);
    /// phonenumbers.Write("08-54137731");
    /// phonenumbers.Write("0702-424472");
    /// person.HaveWritten( phonenumber.Seal() );
    /// person.Write("United Kingdom");
    /// person.Seal();</code>
    /// </example>
   public unsafe struct TupleWriterBase64
   {
#if BASE32
      public const int MAXOFFSETSIZE = 6;
#endif
#if BASE64
      public const int MAXOFFSETSIZE=5;
      public const int DEFAULTOFFSETSIZE = 2;
#endif
#if BASE256
       /// <summary>
       /// MAXOFFSETSIZE
       /// </summary>
      public const int MAXOFFSETSIZE=4;
#endif
      public const int OffsetElementSizeSize = 1; // The tuple begins with an integer telling the size. The size of this integer is always 1 byte.

      /// <summary>
      /// Offset integer pointing to the end of the tuple with 0 being the begining of the value count
      /// Kept to speed up writing of offsets into the offset list
      /// </summary>
      public UInt32 ValueOffset;

      /// <summary>
      /// Counter remembering the number of values to be written
      /// </summary>
      public UInt32 ValueCount;

      /// <summary>
      /// Pointer to the start of this tuple
      /// </summary>
      public readonly byte* AtStart;

      /// <summary>
      /// Pointer to the end of the offset list of the tuple
      /// </summary>
      public byte* AtOffsetEnd;

      /// <summary>
      /// Pointer to the end of the tuple
      /// </summary>
      public byte* AtEnd;

 //     public byte* OverflowLimit;

       /// <summary>
       /// OffsetElementSize
       /// </summary>
      public uint OffsetElementSize;

       /// <summary>
       /// Maximum possible size of tuple in bytes if set by user
       /// </summary>
      public uint TupleMaxLength;

       /// <summary>
       /// The available size left (in bytes)
       /// </summary>
      public uint AvailableSize;

       /// <summary>
       /// Method
       /// </summary>
       /// <param name="start"></param>
       /// <param name="valueCount"></param>
       /// <param name="offsetElementSize"></param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public TupleWriterBase64(byte* start, uint valueCount, uint offsetElementSize) {
          AtStart = start;
          AtOffsetEnd = AtStart + OffsetElementSizeSize;
          AtEnd = AtStart + OffsetElementSizeSize + valueCount * offsetElementSize;
          OffsetElementSize = offsetElementSize;
          //   OverflowLimit = overflowLimit;
#if BASE256
         AtStart[0] = (byte)OffsetElementSize;
#else
          Base16Int.WriteBase16x1(OffsetElementSize, AtStart);
#endif
          ValueOffset = 0;
          ValueCount = valueCount;
          AvailableSize = 0;
          TupleMaxLength = 0;
      }

       /// <summary>
       /// Method
       /// </summary>
       /// <param name="start"></param>
       /// <param name="valueCount"></param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public TupleWriterBase64(byte* start, uint valueCount)
          : this(start, valueCount, TupleWriterBase64.DEFAULTOFFSETSIZE)
      {
      }

       /// <summary>
       /// Set maximum tuple length in bytes, i.e., how the length of the available memory of the pointer to write to.
       /// </summary>
       /// <param name="length">The length</param>
      public void SetTupleLength(uint length) {
          if (length >= Math.Pow(64, 5))
              throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Maximum length of a tuple cannot be bigger than 64^5.");
          if (AtEnd - AtStart >= length)
              throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Too small length of the tuple");
          TupleMaxLength = length;
          AvailableSize = length;
          AvailableSize -= (uint)(AtEnd - AtStart);
      }

      /// <summary>
      /// Writes an unsigned 4 bit integer
      /// </summary>
      /// <param name="n">The integer to write</param>
      /// <remarks>
      /// As the value size for this type is fixed, there is no offset written in offset array.
      /// </remarks>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void WriteQuartet(uint n)
      {
#if BASE32
         Base16Int.WriteBase16x1(n, AtEnd);
#endif
#if BASE64
         Base16Int.WriteBase16x1( n, AtEnd );
#endif
#if BASE256
         *AtEnd = (byte) n;
#endif
         HaveWritten(1);
      }


      /// <summary>
      /// Writes an unsigned 5 bit integer
      /// </summary>
      /// <param name="n">The integer to write</param>
      /// <remarks>
      /// As the value size for this type is fixed, there is no offset written in offset array.
      /// </remarks>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void WriteQuintet(uint n)
      {
#if BASE32
         Base32Int.WriteBase32x1(n, (IntPtr) AtEnd);
#endif
#if BASE64
          Base64Int.WriteBase64x1(n, AtEnd);
#endif
#if BASE256
         (*AtEnd) = (byte) n;
#endif
         HaveWritten(1);
      }

      /// <summary>
      /// Appends a new value after the last value in this tuple
      /// </summary>
      /// <param name="str">The strinng to add</param>
      /// <remarks>
      /// Make sure that the expected data type for the value is a string before 
      /// calling this function. For performance reasons, there is no such verification
      /// done by this method.
      /// </remarks>
      ///
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void Write(string str)
      {
          uint len = 1;
          if (str == null)
              Base64Int.WriteBase64x1(1, AtEnd); // Write null flag
          else if (str.Length > 0) {
              Base64Int.WriteBase64x1(0, AtEnd); // Write null flag
              fixed (char* pStr = str) {
                  // Write the string to the end of this tuple.
                  len += (uint)SessionBlobProxy.Utf8Encode.GetBytes(pStr, str.Length, AtEnd + 1, str.Length * 3, true);
              }
          } else
              len = 0;
          HaveWritten(len);
      }

      /// <summary>
      /// Writes an unsigned integer value to the tuple
      /// </summary>
      /// <param name="n">The value to write</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void Write(ulong n)
      {
#if BASE32
         uint len = Base32Int.Write((IntPtr) AtEnd, n);
#endif
#if BASE64
         uint len = Base64Int.Write(AtEnd, n);
#endif
#if BASE256
         uint len = Base256Int.Write((IntPtr)AtEnd, n);
#endif
         HaveWritten(len);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void Write(byte[] value) {
#if BASE64
          uint len = Base64Binary.Write(AtEnd, value);
          HaveWritten(len);
#else
          throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
#endif
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void Write(byte* value, uint length) {
#if BASE64
          uint len = Base64Binary.Write(AtEnd, value, length);
          HaveWritten(len);
#else
          throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED);
#endif
      }

      public static uint MeasureNeededSize(String str) {
          if (str == null)
              return 1; // null flag
          if (str.Length == 0)
              return 0;
          uint expectedLen;
          fixed (char* pStr = str) {
              expectedLen = (uint)SessionBlobProxy.Utf8Encode.GetByteCount(pStr, str.Length, true);
          }
          return expectedLen + 1; // + null flag
      }

      public static uint MeasureNeededSize(ulong n) {
#if BASE64
          return Base64Int.MeasureNeededSize(n);
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Support for base 32 or 256 encoding is not implement");
#endif
      }

      public static uint MeasureNeededSizeByteArray(uint length) {
#if BASE64
          return Base64Binary.MeasureNeededSizeToEncode(length);
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Support for base 32 or 256 encoding is not implement");
#endif
      }

      public static uint MeasureNeededSize(byte[] b) {
#if BASE64
          return MeasureNeededSizeByteArray((uint)b.Length);
#else
          throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Support for base 32 or 256 encoding is not implement");
#endif
      }

      /// <summary>
      /// Checks if string value fits the tuple and writes it
      /// </summary>
      /// <param name="value">String to write</param>
      private uint ValidateLength(uint expectedLen) {
          if (TupleMaxLength == 0)
              throw ErrorCode.ToException(Error.SCERRNOTUPLEWRITESAVE);
          if (ValuesWrittenSoFar() == ValueCount)
              throw ErrorCode.ToException(Error.SCERRTUPLEOUTOFRANGE, "Cannot write since the index will be out of range.");
          uint neededOffsetSize = Base64Int.MeasureNeededSize((ulong)(ValueOffset + expectedLen));
          if (OffsetElementSize < neededOffsetSize)
              expectedLen += MoveValuesRightSize(neededOffsetSize);
          if (expectedLen > AvailableSize)
              throw ErrorCode.ToException(Error.SCERRTUPLEVALUETOOBIG);
          return expectedLen;
      }

      public void WriteSafe(ulong n) {
          uint size = MeasureNeededSize(n);
          size = ValidateLength(size);
          Write(n);
          Debug.Assert(AtEnd - AtStart <= TupleMaxLength);
          AvailableSize -= size;
      }

      public void WriteSafe(String str) {
          uint size = MeasureNeededSize(str);
          uint writeSize = ValidateLength(size);
          uint len = 1;
          if (str == null)
              Base64Int.WriteBase64x1(1, AtEnd); // Write null flag
          else if (str.Length > 0) {
              Base64Int.WriteBase64x1(0, AtEnd); // Write null flag
              fixed (char* pStr = str) {
                  // Write the string to the end of this tuple.
                  len += (uint)SessionBlobProxy.Utf8Encode.GetBytes(pStr, str.Length, AtEnd + 1, (int)size - 1, true);
              }
          } else
              len = 0;
          Debug.Assert(len == size);
          HaveWritten(len);
          Debug.Assert(AtEnd - AtStart <= TupleMaxLength);
          AvailableSize -= writeSize;
      }

      public unsafe void WriteSafe(byte* b, uint length) {
          uint size = MeasureNeededSizeByteArray(length);
          size = ValidateLength(size);
          Write(b, length);
          Debug.Assert(AtEnd - AtStart <= TupleMaxLength);
          AvailableSize -= size;
      }

      public unsafe void WriteSafe(byte[] b) {
          uint size = 0;
          if (b == null)
              size = 1;
          else
          size = MeasureNeededSizeByteArray((uint)b.Length);
          size = ValidateLength(size);
          Write(b);
          Debug.Assert(AtEnd - AtStart <= TupleMaxLength);
          AvailableSize -= size;
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>When you write a nested tuple, the parent tuple (the hosting tuple) will need to advance its write pointer. When you write a string or another primitive value,
      /// this is done automatically, but when you have written data using the nested tuple, you need to call the HaveWritten method manually.</summary>
      /// <param name="len">The number of bytes that you have written in the nested tuple. This value is returned when you call the <see cref="SealTuple">SealTuple Method</see> method.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe void HaveWritten(uint len) {
          byte* oldAtOffsetEnd = AtOffsetEnd;
          ValueOffset = ValueOffset + len;
          AtEnd += len;

          AtOffsetEnd += OffsetElementSize;

          // Write the offset of the *next* value at the end of the offset list
#if BASE32
Retry:
         switch (OffsetElementSize)
         {
            case 1:
               if (Base32Int.MeasureNeededSize(ValueOffset)>1)
               {
                  this.Grow(ValueOffset);
                  oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
//                  oldAtOffsetEnd[0] = 33;
//                  oldAtOffsetEnd[1] = 33;
                  goto Retry;
               }
               else
               {
                  Base32Int.WriteBase32x1(ValueOffset, (IntPtr) oldAtOffsetEnd);
               }
               break;
            case 2:
               if (Base32Int.MeasureNeededSize(ValueOffset) > 2)
               {
                  this.Grow(ValueOffset);
                  oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
                  //                  oldAtOffsetEnd[0] = 33;
                  //                  oldAtOffsetEnd[1] = 33;
                  goto Retry;
               }
               else
               {
                  Base32Int.WriteBase32x2(ValueOffset, (IntPtr)oldAtOffsetEnd);
               }
               break;
            case 3:
               if (Base32Int.MeasureNeededSize(ValueOffset) > 3)
               {
                  this.Grow(ValueOffset);
                  oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
                  //                  oldAtOffsetEnd[0] = 33;
                  //                  oldAtOffsetEnd[1] = 33;
                  goto Retry;
               }
               else
               {
                  Base32Int.WriteBase32x3(ValueOffset, (IntPtr)oldAtOffsetEnd);
               }
               break;
            case 4:
               if (Base32Int.MeasureNeededSize(ValueOffset) > 4)
               {
                  this.Grow(ValueOffset);
                  oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
                  //                  oldAtOffsetEnd[0] = 33;
                  //                  oldAtOffsetEnd[1] = 33;
                  goto Retry;
               }
               else
               {
                  Base32Int.WriteBase32x4(ValueOffset, (IntPtr)oldAtOffsetEnd);
               }
               break;
            case 6:
               if (Base32Int.MeasureNeededSize(ValueOffset)>6)
               {
                  throw new Exception("Tuple to big");
               }
               else
               {
                  Base32Int.WriteBase32x6(ValueOffset, (IntPtr) oldAtOffsetEnd);
               }
               break;
            default:
               throw new Exception("Illegal offset element size in tuple");
         }
#endif
#if BASE64
          if (OffsetElementSize < Base64Int.MeasureNeededSize(ValueOffset)) {
              Grow(ValueOffset);
              oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
          }
          if (OffsetElementSize == 2)
              Base64Int.WriteBase64x2(ValueOffset, oldAtOffsetEnd);
          else if (OffsetElementSize == 1)
              Base64Int.WriteBase64x1(ValueOffset, oldAtOffsetEnd);
          else if (OffsetElementSize == 3)
              Base64Int.WriteBase64x3(ValueOffset, oldAtOffsetEnd);
          else if (OffsetElementSize == 4)
              Base64Int.WriteBase64x4(ValueOffset, oldAtOffsetEnd);
          else if (OffsetElementSize == 5)
              Base64Int.WriteBase64x5(ValueOffset, oldAtOffsetEnd);
          else
              throw ErrorCode.ToException(Error.SCERRTUPLETOOBIG);

#endif
#if BASE256
Retry:

         switch (OffsetElementSize)
         {
            case 1:
               *((byte*)oldAtOffsetEnd) = (byte)ValueOffset;
               if ((ValueOffset & 0xFFFFFF00) != 0)
               {
                  this.Grow(ValueOffset);
                  oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
                  goto Retry;
               }
               break;
            case 2:
               *((UInt16*)oldAtOffsetEnd) = (UInt16)ValueOffset;
               if ( ( ValueOffset & 0xFFFF0000 ) != 0)
               {
                  this.Grow(ValueOffset);
                  oldAtOffsetEnd = AtOffsetEnd - OffsetElementSize;
                  goto Retry;
               }
               break;
            case 4:
               *((UInt32*)oldAtOffsetEnd) = ValueOffset;
               break;
            default:
               throw new Exception("Illegal offset element size in tuple");
         }
#endif
      }

      public unsafe delegate UInt64 ReadBase64(byte* ptr);
      public unsafe delegate void WriteBase64(UInt64 value, byte* ptr);

       /// <summary>
       /// Calculates for how many bytes the values should be moved to the right.
       /// </summary>
       /// <param name="newOffsetSize">New offset size to fit offset value, which requires the move</param>
       /// <returns>The caclulated number of bytes</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public uint MoveValuesRightSize(uint newOffsetSize) {
          Debug.Assert(newOffsetSize - OffsetElementSize > 0);
          return ValueCount * (newOffsetSize - OffsetElementSize);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public uint ValuesWrittenSoFar() {
          return (uint)((AtOffsetEnd - (AtStart + OffsetElementSizeSize)) / OffsetElementSize);
      }
      /// <summary>
      /// This is a tricky task. We have guessed a to small size for the element offsets. We have used a to narrow size of
      /// the element size. It means that the values and the offsets needs to move.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public void Grow(uint newValueOffset) {
#if BASE32
         uint oesAfter = Base32Int.MeasureNeededSize(newValueOffset);
#endif
#if BASE64
          uint oesAfter = Base64Int.MeasureNeededSize(newValueOffset);
#endif
#if BASE256
         uint oesAfter = Base256Int.MeasureNeededSize(newValueOffset);
#endif
          uint oesBefore = OffsetElementSize;
          uint valuesWrittenSoFar = ValuesWrittenSoFar() - 1; // Expensive division here!
          uint needed = valuesWrittenSoFar * oesAfter;
          uint used = valuesWrittenSoFar * oesBefore;
          uint moveValuesRight = MoveValuesRightSize(oesAfter);
          // Move values to the right to have space for offset
          byte* values = AtStart + OffsetElementSizeSize + ValueCount * oesBefore;
          MemcpyUtil.Memcpy16rwd(values + moveValuesRight, values, ValueOffset);

          byte* newOffsets = AtStart + OffsetElementSizeSize;
          byte* offsets = newOffsets;
          offsets += used;
          newOffsets += needed;
          Debug.Assert(oesBefore < oesAfter);
          ReadBase64 read;
          switch (oesBefore) {
              case 1: read = Base64Int.ReadBase64x1;
                  break;
              case 2: read = Base64Int.ReadBase64x2;
                  break;
              case 3: read = Base64Int.ReadBase64x3;
                  break;
              case 4: read = Base64Int.ReadBase64x4;
                  break;
              case 5: read = Base64Int.ReadBase64x5;
                  break;
              default: throw ErrorCode.ToException(Error.SCERRUNEXPFASTERTHANJSON, "Unexpected offset size in TupleWriter.");
          }
          WriteBase64 write;
          switch (oesAfter) {
              case 2:
                  write = Base64Int.WriteBase64x2;
                  break;
              case 3:
                  write = Base64Int.WriteBase64x3;
                  break;
              case 4:
                  write = Base64Int.WriteBase64x4;
                  break;
              case 5:
                  write = Base64Int.WriteBase64x5;
                  break;
              default: throw ErrorCode.ToException(Error.SCERRTUPLETOOBIG);
          }
          for (uint t = valuesWrittenSoFar; t > 0; t--) {
              ulong offsetsValue;
              offsets -= oesBefore;
              newOffsets -= oesAfter;
              offsetsValue = read(offsets);
              write(offsetsValue, newOffsets);
          }
#if BASE256
         *AtStart = (byte)oesAfter; // The first byte in the tuple tells the offset element size of the tuple
#else
          Base16Int.WriteBase16x1(oesAfter, AtStart); // The first byte in the tuple tells the offset element size of the tuple
#endif
          AtEnd += moveValuesRight;
          OffsetElementSize = oesAfter;
          AtOffsetEnd += needed - used + OffsetElementSizeSize;
      }

       /// <summary>
       /// Method
       /// </summary>
       /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe uint SealTuple()
      {
         // Trim the tuple by using the smallest offset element size needed.
         // The default is 5 bytes and often only 1 byte is needed.
         // Update the offset in the parent tuple.
         // Parent tuple must not be sealed.
         return FastSealTuple();

#if false
#if BASE32
         uint oesAfter = Base32Int.MeasureNeededSize(this.ValueOffset);
#endif
#if BASE64
         uint oesAfter = Base64Int.MeasureNeededSize(this.ValueOffset);
#endif
#if BASE256
         uint oesAfter = Base256Int.MeasureNeededSize(this.ValueOffset);
#endif
         uint oesBefore = OffsetElementSize;
         uint moveOffsetsLeft = oesBefore - oesAfter;


         uint needed = ValueCount*oesAfter;
         byte* newOffsets = (AtStart + 1);
         uint used = ValueCount*oesBefore;
#if BASE256
   // Due to the little endianess of the Intel x64 architecture, we need to copy differently than in the text based notation
         byte* offsets = newOffsets;
#else
         byte* offsets = newOffsets + oesBefore - oesAfter;
#endif
         uint moveValuesLeft = used - needed;

         // Let's calculate if it is worth while to compact the tuple:
         // We want moveValuesLeft (equaling the bytes we would save) to be at least an eights of this values.
         if (ValueOffset >> 3 > moveValuesLeft) {
             return FastSealTuple();
         }
//#if GUESSSIZE
//         // Let's calculate if it is worth while to compact the tuple:
//         // We want moveValuesLeft (equaling the bytes we would save) to be at least an fourth of this values.
//         if (ValueOffset >> 2 > moveValuesLeft)
//         {
//            return FastSealTuple();
//         }
//#else
//         // Let's calculate if it is worth while to compact the tuple:
//         // We want moveValuesLeft (equaling the bytes we would save) to be at least an sixteenth of this values.
//         if (ValueOffset >> 4 > moveValuesLeft)
//         {
//            return FastSealTuple();
//         }
//#endif

         for (int t = 0; t < ValueCount; t++)
         {
            Intrinsics.MemCpy((void*) (newOffsets), offsets, (uint) oesAfter);
            offsets += oesBefore;
            newOffsets += oesAfter;
         }
         byte* values = ((AtStart + 1) + ValueCount*oesBefore);
         Intrinsics.MemCpy((void*) (values - moveValuesLeft), (void*) values, (uint) ValueOffset);
         //Size -= moveValuesLeft;
#if BASE256
         *AtStart = (byte)oesAfter; // The first byte in the tuple tells the offset element size of the tuple
#else
         Base16Int.WriteBase16x1(oesAfter, AtStart); // The first byte in the tuple tells the offset element size of the tuple
#endif
         AtEnd -= moveValuesLeft;
         return (uint) (AtEnd - AtStart);
         //         Base64.WriteBase64x5(this.Size, &(this.Cached_Blob.Cached_Blob->RootParentOffsetArray)); // TODO! Parent might be another tuple. Write in correct place.
#endif
      }

       /// <summary>
       /// Method
       /// </summary>
       /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public unsafe uint FastSealTuple()
      {
         return (uint) (AtEnd - AtStart);
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
       /// <summary>
       /// Length
       /// </summary>
      public int Length
      {
         get { return (int) (AtEnd - AtStart); }
      }

       /// <summary>
       /// DebugString
       /// </summary>
      public string DebugString
      {
         get
         {
            var tr = new TupleReaderBase64(AtStart, ValueCount);
            return tr.DebugString;
         }
      }
   }
}
