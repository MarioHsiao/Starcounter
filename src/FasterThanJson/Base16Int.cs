using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal
{
   public struct Base16x1
   {
      public byte b0;
   }

   public class Base16Int
   {
      public const byte Const5 = (byte)'5';
      public const byte Const6 = (byte)'6';

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase16x1(UInt32 value, byte* c)
      {
         c[0] = (byte)(value |= 0x30);
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe int ReadBase16x1(Base16x1* c)
      {
         return (c->b0 & 0x0F);
      }

   }
}
