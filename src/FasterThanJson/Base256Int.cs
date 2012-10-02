


using System;
using System.Text;

namespace Starcounter.Internal
{

   public class Base256Int
   {

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe uint Write(IntPtr ptr, UInt64 value)
      {
         var buffer = (byte*)ptr;
         if ((value & 0xFFFFFF00) == 0)
         {
            *buffer = (byte)value;
            return 1;
         }
         else if ((value & 0xFFFF0000) == 0)
         {
            *((UInt16*) (buffer)) = (UInt16)value;
            return 2;
         }
         else if ((value & 0xFFFFFFFF00000000L) == 0) {
             *((UInt32*)(buffer)) = (UInt32)value;
             return 4;
         }
         throw new Exception("TODO!");
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe uint MeasureNeededSize(UInt32 value)
      {
         if ((value & 0xFFFFFF00) != 0)
            return (uint) ((value & 0xFFFF0000) != 0 ? 4 : 2);
         return 1;
      }


      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 Read(int size, IntPtr ptr  )
      {
         var buffer = (byte*)ptr;
         switch (size)
         {
            case 0:
               return 0;
            case 1:
               return *buffer;
            case 2:
               return *((UInt16*)buffer);
            case 4:
               return *((UInt32*)buffer);
         }
         throw new Exception("Size not supported");
      }

      public static string FixString( byte[] buffer )
      {
         var sb = new StringBuilder();
//         char[] buffer = str.ToCharArray();
         var len = buffer.Length;
         for (int t=0;t<len;t++)
         {
            var c = (char)buffer[t];
            if ( c < 32 || c >= 127 )
            {
               sb.Append("{" + ((int)c).ToString() + "}");
            }
            else
            {
               sb.Append(c);
            }
         }
         return sb.ToString();
      }


   }
}