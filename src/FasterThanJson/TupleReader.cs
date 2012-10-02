
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Starcounter.Internal
{
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

      public int OffsetElementSize;

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
      /// Reads an unsigned 4 bit integer
      /// </summary>
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


      public int ReadByteCount
      {
          [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
          get
         {
            return (int)(AtEnd - AtStart);
         }
      }

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
