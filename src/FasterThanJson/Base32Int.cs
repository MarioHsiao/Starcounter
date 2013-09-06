// ***********************************************************************
// <copyright file="Base32Int.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// TODO! Incomplete implementation

using System;

namespace Starcounter.Internal
{
    /// <summary>
    /// Struct Base32x13
    /// </summary>
   public struct Base32x13
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
      /// <summary>
      /// The B11
      /// </summary>
      public byte b11;
      /// <summary>
      /// The B12
      /// </summary>
      public byte b12;
   }
//   public struct Base32x8
//   {
//      public byte b0;
//      public byte b1;
//      public byte b2;
//      public byte b3;
//      public byte b4;
//      public byte b5;
//      public byte b6;
//      public byte b7;
//   }
   /// <summary>
   /// Struct Base32x7
   /// </summary>
   public struct Base32x7
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
   }
   /// <summary>
   /// Struct Base32x6
   /// </summary>
   public struct Base32x6
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
//   public struct Base32x5
//   {
//      public byte b0;
//      public byte b1;
//      public byte b2;
//      public byte b3;
//      public byte b4;
//   }
   /// <summary>
   /// Struct Base32x4
   /// </summary>
   public struct Base32x4
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
   /// Struct Base32x3
   /// </summary>
   public struct Base32x3
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
   /// Struct Base32x2
   /// </summary>
   public struct Base32x2
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
   /// Struct Base32x1
   /// </summary>
   public struct Base32x1
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
   }

   // <summary>
   // Fast printable Base32 (5 bit) integer encoding. No tables needed as the number is encoded/decoded using the 0x1F mask (00011111).
   // It looses one bit per byte compared to Base64, but is faster to read and write.
   // Each byte are within the printable ASCII range. Base32 is a good compromise between performance and size.
   // It is ideal to provide integer representation in ASCII/UTF8 environments. 
   //
   // Supported sizes are:
   // 1 byte (5 bits)
   // 2 bytes (10 bits)
   // 3 bytes (15 bits)
   // 4 bytes (20 bits)
   // 6 bytes (30 bits)
   // 7 bytes (35 bits)
   // 13 bytes (65 bits)
   // </summary>
   // <remarks>
   //  @ = 0
   //  A = 1
   //  B = 1
   //  C = 2
   //  D = 3
   //  E = 4
   //  F = 5
   //  G = 6
   //  H = 7
   //  I = 8
   //  J = 9
   //  K = 10
   //  L = 11
   //  M = 12
   //  N = 13
   //  O = 14
   //  P = 15
   //  Q = 16
   //  R = 17
   //  S = 18
   //  T = 19
   //  U = 20
   //  V = 21
   //  W = 22
   //  X = 23
   //  Y = 24
   //  Z = 25
   //  [ = 26
   //  \ = 27
   //  ] = 28
   //  ^ = 29
   //  _ = 30
   //  ` = 31
   //  a = 32
   // </remarks>
   /// <summary>
   /// Class Base32Int
   /// </summary>
   public class Base32Int
   {

       /// <summary>
       /// Writes the specified buffer.
       /// </summary>
       /// <param name="buffer">The buffer.</param>
       /// <param name="value">The value.</param>
       /// <returns>System.UInt32.</returns>
      public static unsafe uint Write(byte* buffer, UInt32 value )
      {
         var c = (Base32x6*)buffer; 
         if ( ( value & 0xFFFFFFE0 ) == 0 ) // 11 11111 11111 11111 11111 11111 00000 (NOTE: groups of FIVE bits)
         {
            WriteBase32x1(value, buffer);
            return 1;
         }
         else if ((value & 0xFFFFFC00) == 0) // 11 11111 11111 11111 11111 00000 00000 (NOTE: groups of FIVE bits)
         {
            WriteBase32x2(value, buffer);
            return 2;
         }
         else if ((value & (0xFFFF8000)) == 0) // 11 11111 11111 11111 00000 00000 00000 (NOTE: groups of FIVE bits)
         {
            WriteBase32x3(value, buffer);
            return 3;
         }
         else if ((value & ( 0xFFF00000 )) == 0) // 11 11111 11111 00000 00000 00000 00000 (NOTE: groups of FIVE bits)
         {
            WriteBase32x4(value, buffer);
            return 4;
         }
//         else if ((value & ( 0xFE000000 )) == 0) // 11 11111 00000 00000 00000 00000 00000 (NOTE: groups of FIVE bits)
//         {
//            WriteBase32x5(value, buffer);
//            return 5;
//         }
         else if ((value & ( 0xC0000000 )) == 0) // 11 00000 00000 00000 00000 00000 00000 (NOTE: groups of FIVE bits)
         {
            WriteBase32x6(value, buffer);
            return 6;
         }
         WriteBase32x7(value, buffer);
         return 7;
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Measures the size of the needed.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>System.UInt32.</returns>
      /// <exception cref="System.Exception">TODO!</exception>
      public static unsafe uint MeasureNeededSize(UInt64 value)
      {
         if ((value & 0xFFFFFFFFFFFFFFE0) == 0) // 11 11111 11111 11111 11111 11111 00000 (NOTE: groups of FIVE bits)
         {
            return 1;
         }
         else if ((value & 0xFFFFFFFFFFFFFC00) == 0) // 11 11111 11111 11111 11111 00000 00000 (NOTE: groups of FIVE bits)
         {
            return 2;
         }
         else if ((value & (0xFFFFFFFFFFFF8000)) == 0) // 11 11111 11111 11111 00000 00000 00000 (NOTE: groups of FIVE bits)
         {
            return 3;
         }
         else if ((value & (0xFFFFFFFFFFF00000)) == 0) // 11 11111 11111 00000 00000 00000 00000 (NOTE: groups of FIVE bits)
         {
            return 4;
         }
//         else if ((value & (0xFE000000)) == 0) // 11 11111 00000 00000 00000 00000 00000 (NOTE: groups of FIVE bits)
//         {
//            return 5;
//         }
         else if ((value & (0xFFFFFFFFC0000000)) == 0) // 11 00000 00000 00000 00000 00000 00000 (NOTE: groups of FIVE bits)
         {
            return 6;
         }
         throw new Exception("TODO!");
//         return 13;
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x6.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase32x6(UInt64 value, byte* ptr )
      {
         var c = (Base32x6*) ptr;
         c->b0 = (byte)((value & 0x3E000000) >> 25);
         c->b1 = (byte)((value & 0x1F00000) >> 20);
         c->b2 = (byte)((value & 0xF8000) >> 15);
         c->b3 = (byte)((value & 0x7C00) >> 10);
         c->b4 = (byte)((value & 0x3E0) >> 05);
         c->b5 = (byte)(value & 0x1F);
         ((UInt16*)&(c->b4))[0] |= 0x4040;
         ((UInt32*)c)[0] |= 0x40404040; // Add 64 to each byte as '@'represents zero.
      }


      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x7.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase32x7(UInt64 value, byte* ptr)
      {
         var c = (Base32x7*)ptr;
         c->b0 = (byte)((value & 0x00000007C0000000) >> 30);
         c->b1 = (byte)((value & 0x000000003E000000) >> 25);
         c->b2 = (byte)((value & 0x0000000001F00000) >> 20);
         c->b3 = (byte)((value & 0x00000000000F8000) >> 15);
         c->b4 = (byte)((value & 0x0000000000007C00) >> 10);
         c->b5 = (byte)((value & 0x00000000000003E0) >> 05);
         c->b6 = (byte)((value & 0x000000000000001F) | 0x40);
         ((UInt16*)&(c->b4))[0] |= 0x4040;
         ((UInt32*)c)[0] |= 0x40404040; // Add 64 to each byte as '@'represents zero.
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x13.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase32x13(UInt64 value, byte* ptr)
      {
         var c = (Base32x13*)ptr;
         c->b0 = (byte)((value  & 0xF000000000000000) >> 60);
         c->b1 = (byte)((value  & 0x0F80000000000000) >> 55);
         c->b2 = (byte)((value  & 0x007C000000000000) >> 50);
         c->b3 = (byte)((value  & 0x0003E00000000000) >> 45);
         c->b4 = (byte)((value  & 0x00001F0000000000) >> 40);
         c->b5 = (byte)((value  & 0x000000F800000000) >> 35);
         c->b6 = (byte)((value  & 0x00000007C0000000) >> 30);
         c->b7 = (byte)((value  & 0x000000003E000000) >> 25);
         c->b8 = (byte)((value  & 0x0000000001F00000) >> 20);
         c->b9 = (byte)((value  & 0x00000000000F8000) >> 15);
         c->b10 = (byte)((value & 0x0000000000007C00) >> 10);
         c->b11 = (byte)((value & 0x00000000000003E0) >> 05);
         c->b12 = (byte)((value & 0x000000000000001F) | 0x40);
         ((UInt32*)&(c->b8))[0] |= 0x4040;
         ((UInt32*)&(c->b4))[0] |= 0x4040;
         ((UInt32*)c)[0] |= 0x40404040; // Add 64 to each byte as '@'represents zero.
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x1.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="c">The c.</param>
      public static unsafe void WriteBase32x1(UInt64 value, byte* c)
      {
         *(c) = (byte)( ( value & 0x1F ) | 0x40 );
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x2.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase32x2(UInt64 value, byte* ptr)
      {
         var c = (Base32x2*)ptr;
           ((UInt16*)(c))[0] = 0x4040;// Set leading bytes to '@'representing zero.
           c->b0 |= (byte)((value & 0x3E0) >> 5);
           c->b1 |= (byte)(value & 0x1F);
//           c->b0 = (byte)(((value & 0x3E0) >> 5)|0x40);
//           c->b1 = (byte)((value & 0x1F)|0x40);
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x3.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase32x3(UInt64 value, byte* ptr)
      {
         var c = (Base32x3*)ptr;
         ((UInt16*)(c))[0] = 0x4040;// Set leading bytes to '@'representing zero.
         c->b0 |= (byte)((value & 0x7C00) >> 10);
         c->b1 |= (byte)((value & 0x3E0) >> 5);
         c->b2 = (byte)((value & 0x1F) | 0x40);
      }
      
      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Writes the base32x4.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      public static unsafe void WriteBase32x4(UInt64 value, byte* ptr)
      {
         var c = (Base32x4*)ptr;
         ((UInt32*)(c))[0] = 0x40404040;// Set leading bytes to '@'representing zero.
         c->b0 |= (byte)((value & 0xF8000) >> 15);
         c->b1 |= (byte)((value & 0x7C00) >> 10);
         c->b2 |= (byte)((value & 0x3E0) >> 5);
         c->b3 |= (byte)((value & 0x1F));
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the specified size.
      /// </summary>
      /// <param name="size">The size.</param>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      /// <exception cref="System.Exception"></exception>
      public static unsafe UInt64 Read(int size, byte* ptr )
      {
         switch (size)
         {
            case 0:
               return 0;
            case 1:
               return ReadBase32x1(ptr);
            case 2:
               return ReadBase32x2(ptr);
            case 3:
               return ReadBase32x3(ptr);
            case 4:
               return ReadBase32x4(ptr);
            case 6:
               return ReadBase32x6(ptr);
            case 7:
               return ReadBase32x7(ptr);
            case 13:
               return ReadBase32x13(ptr);
         }
         throw new Exception( String.Format("Size {0} is not supported",size) );
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x1.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x1(byte* ptr)
      {
         var c = (Base32x1*)ptr;
         return (UInt64)((c->b0 & 0x1F));
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x2.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x2(byte* ptr)
      {
         var c = (Base32x2*)ptr;
         return (UInt64) (((c->b0 & 0x1F ) << 5) | (c->b1 & 0x1F) );
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x4.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x4(byte* ptr)
      {
         var c = (Base32x4*)ptr;
         return (UInt64)(((c->b0 & 0x1F) << 15) | ((c->b1 & 0x1F) << 10) | ((c->b2 & 0x1F) << 5) | (c->b3 & 0x1F));
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x3.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x3(byte* ptr)
      {
         var c = (Base32x3*)ptr;
         return (UInt64)(((c->b0 & 0x1F) << 10) | ((c->b1 & 0x1F) << 5) | (c->b2 & 0x1F));
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x6.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x6(byte* ptr)
      {
         var c = (Base32x6*)ptr;
         return (UInt64)(((c->b0 & 0x1F) << 25) | ((c->b1 & 0x1F) << 20) | ((c->b2 & 0x1F) << 15) |
          ((c->b3 & 0x1F) << 10) | ((c->b4 & 0x1F) << 5) | (c->b5 & 0x1F));
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x7.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x7(byte* ptr)
      {
         var c = (Base32x7*)ptr;
         return (UInt64)(((c->b0 & 0x1FLU) << 30) | ((c->b1 & 0x1FLU) << 25) | ((c->b2 & 0x1FLU) << 20) | ((c->b3 & 0x1FLU) << 15) |
          ((c->b4 & 0x1FLU) << 10) | ((c->b5 & 0x1FLU) << 5) | (c->b6 & 0x1FLU));
      }

      //[MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base32x13.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      public static unsafe UInt64 ReadBase32x13(byte* ptr)
      {
         var c = (Base32x13*)ptr;
         return (UInt64)(((c->b0 & 0x1FLU) << 60) | ((c->b1 & 0x1FLU) << 55) |
            ((c->b2 & 0x1FLU) << 50) | ((c->b3 & 0x1FLU) << 45) | ((c->b4 & 0x1FLU) << 40) |
            ((c->b5 & 0x1FLU) << 35) | ((c->b6 & 0x1FLU) << 30) | ((c->b7 & 0x1FLU) << 25) |
            ((c->b8 & 0x1FLU) << 20) | ((c->b9 & 0x1FLU) << 15) |
            ((c->b10 & 0x1FLU) << 10) | ((c->b11 & 0x1FLU) << 5) | (c->b12 & 0x1FLU));
      }


   }
}