// ***********************************************************************
// <copyright file="Base64Int.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

///////////////////////////////////////
// URL friendly Base64 encoded integers
// TODO! Optimize
///////////////////////////////////////

using System;
using System.Data;

//using NUnit.Framework;

namespace Starcounter.Internal
{
    /// <summary>
    /// Struct Base64x11
    /// </summary>
   public struct Base64x11
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
      /// <summary>
      /// The b1
      /// </summary>
      public byte b1;
      /// <summary>
      /// The b2
      /// </summary>
      public byte b2;
      /// <summary>
      /// The b3
      /// </summary>
      public byte b3;
      /// <summary>
      /// The b4
      /// </summary>
      public byte b4;
      /// <summary>
      /// The b5
      /// </summary>
      public byte b5;
      /// <summary>
      /// The b6
      /// </summary>
      public byte b6;
      /// <summary>
      /// The b7
      /// </summary>
      public byte b7;
      /// <summary>
      /// The b8
      /// </summary>
      public byte b8;
      /// <summary>
      /// The b9
      /// </summary>
      public byte b9;
      /// <summary>
      /// The B10
      /// </summary>
      public byte b10;
   }
   /// <summary>
   /// Struct Base64x6
   /// </summary>
   public struct Base64x6
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
      /// <summary>
      /// The b1
      /// </summary>
      public byte b1;
      /// <summary>
      /// The b2
      /// </summary>
      public byte b2;
      /// <summary>
      /// The b3
      /// </summary>
      public byte b3;
      /// <summary>
      /// The b4
      /// </summary>
      public byte b4;
      /// <summary>
      /// The b5
      /// </summary>
      public byte b5;
   }
   /// <summary>
   /// Struct Base64x5
   /// </summary>
   public struct Base64x5
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
      /// <summary>
      /// The b1
      /// </summary>
      public byte b1;
      /// <summary>
      /// The b2
      /// </summary>
      public byte b2;
      /// <summary>
      /// The b3
      /// </summary>
      public byte b3;
      /// <summary>
      /// The b4
      /// </summary>
      public byte b4;
   }
   /// <summary>
   /// Struct Base64x4
   /// </summary>
   public struct Base64x4
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
      /// <summary>
      /// The b1
      /// </summary>
      public byte b1;
      /// <summary>
      /// The b2
      /// </summary>
      public byte b2;
      /// <summary>
      /// The b3
      /// </summary>
      public byte b3;
   }
   /// <summary>
   /// Struct Base64x3
   /// </summary>
   public struct Base64x3
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
      /// <summary>
      /// The b1
      /// </summary>
      public byte b1;
      /// <summary>
      /// The b2
      /// </summary>
      public byte b2;
   }
   /// <summary>
   /// Struct Base64x2
   /// </summary>
   public struct Base64x2
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
      /// <summary>
      /// The b1
      /// </summary>
      public byte b1;
   }
   /// <summary>
   /// Struct Base64x1
   /// </summary>
   public struct Base64x1
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
   }



   /// <summary>
   /// Class Base64Int
   /// </summary>
   public class Base64Int
   {
       /// <summary>
       /// The b64e
       /// </summary>
      private static byte[] b64e = new byte[]
                                      {
                                         (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5',
                                         (byte) '6', (byte) '7', (byte) '8', (byte) '9',
                                         (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E', (byte) 'F',
                                         (byte) 'G', (byte) 'H', (byte) 'I', (byte) 'J',
                                         (byte) 'K', (byte) 'L', (byte) 'M', (byte) 'N', (byte) 'O', (byte) 'P',
                                         (byte) 'Q', (byte) 'R', (byte) 'S', (byte) 'T',
                                         (byte) 'U', (byte) 'V', (byte) 'W', (byte) 'X', (byte) 'Y', (byte) 'Z',
                                         (byte) 'a', (byte) 'b', (byte) 'c', (byte) 'd', (byte) 'e', (byte) 'f',
                                         (byte) 'g', (byte) 'h', (byte) 'i', (byte) 'j',
                                         (byte) 'k', (byte) 'l', (byte) 'm', (byte) 'n', (byte) 'o', (byte) 'p',
                                         (byte) 'q', (byte) 'r', (byte) 's', (byte) 't',
                                         (byte) 'u', (byte) 'v', (byte) 'w', (byte) 'x', (byte) 'y', (byte) 'z',
                                         (byte) '-', (byte) '_'
                                      };

      /// <summary>
      /// The B64D
      /// </summary>
      private static int[] b64d = new int[]
                                     {
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000, // xx !"#$%&'
                                        000, 000, 000, 062, 000, 062, 000, 063, 000, 001, // )(*+,-./01
                                        002, 003, 004, 005, 006, 007, 008, 009, 000, 000, // 23456789:;
                                        000, 000, 000, 000, 000, 010, 011, 012, 013, 014, // <=>?@ABCDE
                                        015, 016, 017, 018, 019, 020, 021, 022, 023, 024, // FGHIJKLMNO
                                        025, 026, 027, 028, 029, 030, 031, 032, 033, 034, // PQRSTUVWXY
                                        035, 000, 000, 000, 000, 063, 000, 036, 037, 038, // Z[\]^_`abc
                                        039, 040, 041, 042, 043, 044, 045, 046, 047, 048, // defghijklm
                                        049, 050, 051, 052, 053, 054, 055, 056, 057, 058, // nopqrstuvw
                                        059, 060, 061, 000, 000, 000, 000, 000, 000, 000, // xyz{|}~
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000, 000, 000, 000, 000,
                                        000, 000, 000, 000, 000, 000
                                     };

      /// <summary>
      /// Measures the size of the needed.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>System.UInt32.</returns>
      public static unsafe uint MeasureNeededSize(UInt64 value)
      {
         if (value <= 0x3F)
         {
            //     ((UInt32*)c)[0] = 0x30303030; // Set leading bytes to '0'
            //    c->b4 = b64e[(value & 0x3F)]; // Everything fits in a byte
            return 1;
         }
         else if (value <= 0xFFF)
         {
            //     ((UInt32*)c)[0] = 0x30303030; // Set leading bytes to '0'
            //     c->b3 = b64e[(value & 0xFC0) >> 06];
            //     c->b4 = b64e[(value & 0x3F)];
            return 2;
         }
         else if (value <= 0x3FFFF)
         {
            //     ((UInt32*)c)[0] = 0x30303030; // Set leading bytes to '0'
            //     c->b2 = b64e[(value & 0x3F000) >> 12];
            //     c->b3 = b64e[(value & 0xFC0) >> 06];
            //     c->b4 = b64e[(value & 0x3F)];
            return 3;
         }
         else if (value <= 0xFFFFFF)
         {
            //   c->b0 = 0x30; // Set leading bytes to '0'
            //   c->b1 = b64e[(value & 0xFC0000) >> 18];
            //   c->b2 = b64e[(value & 0x3F000) >> 12];
            //   c->b3 = b64e[(value & 0xFC0) >> 06];
            //   c->b4 = b64e[(value & 0x3F)];
            return 4;
         }
         else if (value <= 0x3FFFFFFF)
         {
            //      c->b0 = b64e[(value & 0x3F000000) >> 24];
            //      c->b1 = b64e[(value & 0xFC0000) >> 18];
            //      c->b2 = b64e[(value & 0x3F000) >> 12];
            //      c->b3 = b64e[(value & 0xFC0) >> 06];
            //      c->b4 = b64e[(value & 0x3F)];
            return 5;
         }
         return 6;
      }

      /// <summary>
      /// Writes the base64x1.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x1(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x1*) ptr;
         c->b0 = b64e[(value & 0x3F)];
      }

      /// <summary>
      /// Writes the base64x2.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x2(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x2*) ptr;
         c->b0 = b64e[(value & 0xFC0) >> 06];
         c->b1 = b64e[(value & 0x3F)];
      }

      /// <summary>
      /// Writes the base64x3.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x3(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x3*) ptr;
         c->b0 = b64e[(value & 0x3F000) >> 12];
         c->b1 = b64e[(value & 0xFC0) >> 06];
         c->b2 = b64e[(value & 0x3F)];
      }


      /// <summary>
      /// Writes the base64x4.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x4(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x4*)ptr;
         c->b0 = b64e[(value & 0xFC0000) >> 18];
         c->b1 = b64e[(value & 0x3F000) >> 12];
         c->b2 = b64e[(value & 0xFC0) >> 06];
         c->b3 = b64e[(value & 0x3F)];
      }

      /// <summary>
      /// Writes the base64x5.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x5(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x5*)ptr;
         c->b0 = b64e[(value & 0x3F000000) >> 24];
         c->b1 = b64e[(value & 0x00FC0000) >> 18];
         c->b2 = b64e[(value & 0x0003F000) >> 12];
         c->b3 = b64e[(value & 0x00000FC0) >> 06];
         c->b4 = b64e[(value & 0x0000003F)];
      }

      /// <summary>
      /// Writes the base64x6.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x6(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x6*)ptr;
         c->b0 = b64e[(value & 0x0000000FC0000000UL) >> 30];
         c->b1 = b64e[(value & 0x000000003F000000UL) >> 24];
         c->b2 = b64e[(value & 0x0000000000FC0000UL) >> 18];
         c->b3 = b64e[(value & 0x000000000003F000UL) >> 12];
         c->b4 = b64e[(value & 0x0000000000000FC0UL) >> 06];
         c->b5 = b64e[(value & 0x000000000000003FUL)];
      }

      /// <summary>
      /// Writes the base64x11.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase64x11(UInt64 value, IntPtr ptr)
      {
         var c = (Base64x11*)ptr;
         c->b0 = b64e[(value  & 0xF000000000000000UL) >> 60];
         c->b1 = b64e[(value  & 0x0FC0000000000000UL) >> 54];
         c->b2 = b64e[(value  & 0x003F000000000000UL) >> 48];
         c->b3 = b64e[(value  & 0x0000FC0000000000UL) >> 42];
         c->b4 = b64e[(value  & 0x000003F000000000UL) >> 36];
         c->b5 = b64e[(value  & 0x0000000FC0000000UL) >> 30];
         c->b6 = b64e[(value  & 0x000000003F000000UL) >> 24];
         c->b7 = b64e[(value  & 0x0000000000FC0000UL) >> 18];
         c->b8 = b64e[(value  & 0x000000000003F000UL) >> 12];
         c->b9 = b64e[(value  & 0x0000000000000FC0UL) >> 06];
         c->b10 = b64e[(value & 0x000000000000003FUL)];
      }



      /// <summary>
      /// Writes the specified buffer.
      /// </summary>
      /// <param name="buffer">The buffer.</param>
      /// <param name="value">The value.</param>
      /// <returns>System.UInt32.</returns>
      public static unsafe uint Write(IntPtr buffer, UInt32 value)
      {
         var c = (Base64x5*) buffer;
         if ((value & 0xFFFFFFC0) == 0) // 11 111111 111111 111111 111111 000000 (NOTE: groups of SIX bits)
         {
            WriteBase64x1(value, buffer);
            return 1;
         }
         else if ((value & 0xFFFFF000) == 0) // 11 111111 111111 111111 000000 000000 (NOTE: groups of SIX bits)
         {
            WriteBase64x2(value, buffer);
            return 2;
         }
         else if ((value & (0xFFFC0000)) == 0) // 11 111111 111111 000000 000000 000000 (NOTE: groups of SIX bits)
         {
            WriteBase64x3(value, buffer);
            return 3;
         }
         else if ((value & (0xFF000000)) == 0) // 11 111111 000000 000000 000000 000000 (NOTE: groups of SIX bits)
         {
            WriteBase64x4(value, buffer);
            return 4;
         }
         else if ((value & (0xC0000000)) == 0) // 11 000000 000000 000000 000000 000000 (NOTE: groups of SIX bits)
         {
            WriteBase64x5(value, buffer);
            return 5;
         }
         WriteBase64x6(value, buffer);
         return 6;
      }




      /// <summary>
      /// Reads the specified size.
      /// </summary>
      /// <param name="size">The size.</param>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      /// <exception cref="System.Exception">Illegal size</exception>
      public static unsafe UInt64 Read( int size, IntPtr ptr)
      {
         switch (size)
         {
            case 1:
               return ReadBase64x1(ptr);
            case 2:
               return ReadBase64x2(ptr);
            case 3:
               return ReadBase64x3(ptr);
            case 4:
               return ReadBase64x4(ptr);
            case 5:
               return ReadBase64x5(ptr);
            case 6:
               return ReadBase64x6(ptr);
            case 11:
               return ReadBase64x11(ptr);
            default:
               throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Incorrect input size, "+size+", in UInt64 read of FasterThanJson.");
           
         }
		 //var c = (Base64x5*)ptr;
		 //return (UInt64)((b64d[c->b0] << 24) + (b64d[c->b1] << 18) +
		 //          (b64d[c->b2] << 12) + (b64d[c->b3] << 6) + b64d[c->b4]);
      }

      /// <summary>
      /// Reads the base64x1.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x1(IntPtr ptr)
      {
         var c = (Base64x1*)ptr;
         return (UInt64)(b64d[c->b0]);
      }

      /// <summary>
      /// Reads the base64x2.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x2(IntPtr ptr)
      {
         var c = (Base64x2*)ptr;
         return (UInt64)((b64d[c->b0] << 6) + b64d[c->b1]);
      }

      /// <summary>
      /// Reads the base64x3.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x3(IntPtr ptr)
      {
         var c = (Base64x3*)ptr;
         return (UInt64)((b64d[c->b0] << 12) + (b64d[c->b1] << 6) + b64d[c->b2]);
      }

      /// <summary>
      /// Reads the base64x4.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x4(IntPtr ptr)
      {
         var c = (Base64x4*)ptr;
         return (UInt64)((b64d[c->b0] << 18) +
                   (b64d[c->b1] << 12) + (b64d[c->b2] << 6) + b64d[c->b3]);
      }

      /// <summary>
      /// Reads the base64x5.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x5(IntPtr ptr)
      {
         var c = (Base64x5*)ptr;
         return (UInt64)((b64d[c->b0] << 24) + (b64d[c->b1] << 18) +
                   (b64d[c->b2] << 12) + (b64d[c->b3] << 6) + b64d[c->b4]);
      }

      /// <summary>
      /// Reads the base64x6.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x6(IntPtr ptr)
      {
         var c = (Base64x6*)ptr;
         return ((((UInt64)b64d[c->b0]) << 30)) + (((UInt64)b64d[c->b1] << 24)) + (((UInt64)b64d[c->b2] << 18)) +
                   (((UInt64)b64d[c->b3] << 12)) + (((UInt64)b64d[c->b4] << 6)) + ((UInt64)b64d[c->b5]);
      }

      /// <summary>
      /// Reads the base64x11.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase64x11(IntPtr ptr)
      {
         var c = (Base64x11*)ptr;
         return    (((UInt64)(b64d[c->b0])) << 60) + (((UInt64)(b64d[c->b1])) << 54) +
                   (((UInt64)b64d[c->b2]) << 48) + (((UInt64)b64d[c->b3]) << 42) + 
                   (((UInt64)b64d[c->b4]) << 36) + (((UInt64)b64d[c->b5]) << 30) + 
                   (((UInt64)b64d[c->b6]) << 24) + (((UInt64)b64d[c->b7]) << 18) +
                   (((UInt64)b64d[c->b8]) << 12) + (((UInt64)b64d[c->b9]) << 6) + ((UInt64)b64d[c->b10]);
      }
   }
}