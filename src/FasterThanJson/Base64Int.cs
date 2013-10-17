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
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
   public struct Base64x8 {
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
       public byte b6;
       public byte b7;
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
                                         (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5',
                                         (byte) '6', (byte) '7', (byte) '8', (byte) '9',
                                         (byte) '-', (byte) '_'
                                      };

      /// <summary>
      /// The B64D
      /// </summary>
      private static byte[] b64d = new byte[]
                                     {
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // xx !"#$%&'
                                        255, 255, 255, 062, 255, 062, 255, 063, 052, 053, // )(*+,-./01
                                        054, 055, 056, 057, 058, 059, 060, 061, 255, 255, // 23456789:;
                                        255, 255, 255, 255, 255, 000, 001, 002, 003, 004, // <=>?@ABCDE
                                        005, 006, 007, 008, 009, 010, 011, 012, 013, 014, // FGHIJKLMNO
                                        015, 016, 017, 018, 019, 020, 021, 022, 023, 024, // PQRSTUVWXY
                                        025, 255, 255, 255, 255, 063, 255, 026, 027, 028, // Z[\]^_`abc
                                        029, 030, 031, 032, 033, 034, 035, 036, 037, 038, // defghijklm
                                        039, 040, 041, 042, 043, 044, 045, 046, 047, 048, // nopqrstuvw
                                        049, 050, 051, 255, 255, 255, 255, 255, 255, 255, // xyz{|}~
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                                        255, 255, 255, 255, 255, 255
                                     };

      /// <summary>
      /// Measures the size of the needed.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>System.UInt32.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe int MeasureNeededSize(UInt64 value) {
          if (value <= 0x3F) {
              //     ((UInt32*)c)[0] = 0x30303030; // Set leading bytes to '0'
              //    c->b4 = b64e[(value & 0x3F)]; // Everything fits in a byte
              return 1;
          } else if (value <= 0xFFF) {
              //     ((UInt32*)c)[0] = 0x30303030; // Set leading bytes to '0'
              //     c->b3 = b64e[(value & 0xFC0) >> 06];
              //     c->b4 = b64e[(value & 0x3F)];
              return 2;
          } else if (value <= 0x3FFFF) {
              //     ((UInt32*)c)[0] = 0x30303030; // Set leading bytes to '0'
              //     c->b2 = b64e[(value & 0x3F000) >> 12];
              //     c->b3 = b64e[(value & 0xFC0) >> 06];
              //     c->b4 = b64e[(value & 0x3F)];
              return 3;
          } else if (value <= 0xFFFFFF) {
              //   c->b0 = 0x30; // Set leading bytes to '0'
              //   c->b1 = b64e[(value & 0xFC0000) >> 18];
              //   c->b2 = b64e[(value & 0x3F000) >> 12];
              //   c->b3 = b64e[(value & 0xFC0) >> 06];
              //   c->b4 = b64e[(value & 0x3F)];
              return 4;
          } else if (value <= 0x3FFFFFFF) {
              //      c->b0 = b64e[(value & 0x3F000000) >> 24];
              //      c->b1 = b64e[(value & 0xFC0000) >> 18];
              //      c->b2 = b64e[(value & 0x3F000) >> 12];
              //      c->b3 = b64e[(value & 0xFC0) >> 06];
              //      c->b4 = b64e[(value & 0x3F)];
              return 5;
          } else if (value <= 0xFFFFFFFFF)
              return 6;
          else if (value <= 0xFFFFFFFFFFFF)
              return 8;
          return 11;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe int MeasureNeededSizeNullable(UInt64? value) {
          if (value == null)
              return 0;
          return MeasureNeededSize((UInt64)value);
      }

      /// <summary>
      /// Writes the base64x1.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x1(UInt64 value, byte* ptr)
      {
          var c = (Base64x1*)ptr;
         c->b0 = b64e[(value & 0x3F)];
      }

      /// <summary>
      /// Writes the base64x2.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x2(UInt64 value, byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x3(UInt64 value, byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x4(UInt64 value, byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x5(UInt64 value, byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x6(UInt64 value, byte* ptr)
      {
         var c = (Base64x6*)ptr;
         c->b0 = b64e[(value & 0x0000000FC0000000UL) >> 30];
         c->b1 = b64e[(value & 0x000000003F000000UL) >> 24];
         c->b2 = b64e[(value & 0x0000000000FC0000UL) >> 18];
         c->b3 = b64e[(value & 0x000000000003F000UL) >> 12];
         c->b4 = b64e[(value & 0x0000000000000FC0UL) >> 06];
         c->b5 = b64e[(value & 0x000000000000003FUL)];
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x8(UInt64 value, byte* ptr) {
          var c = (Base64x8*)ptr;
          c->b0 = b64e[(value & 0x0000FC0000000000UL) >> 42];
          c->b1 = b64e[(value & 0x000003F000000000UL) >> 36];
          c->b2 = b64e[(value & 0x0000000FC0000000UL) >> 30];
          c->b3 = b64e[(value & 0x000000003F000000UL) >> 24];
          c->b4 = b64e[(value & 0x0000000000FC0000UL) >> 18];
          c->b5 = b64e[(value & 0x000000000003F000UL) >> 12];
          c->b6 = b64e[(value & 0x0000000000000FC0UL) >> 06];
          c->b7 = b64e[(value & 0x000000000000003FUL)];
      }

       /// <summary>
      /// Writes the base64x11.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="ptr">The PTR.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase64x11(UInt64 value, byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe int Write(byte* buffer, UInt64 value)
      {
         var c = (Base64x5*) buffer;
         if ((value & 0xFFFFFFFFFFFFFFC0) == 0) {// 11 111111 111111 111111 111111 000000 (NOTE: groups of SIX bits)
             WriteBase64x1(value, buffer);
            return 1;
         } else if ((value & 0xFFFFFFFFFFFFF000) == 0) {// 11 111111 111111 111111 000000 000000 (NOTE: groups of SIX bits)
             WriteBase64x2(value, buffer);
            return 2;
         } else if ((value & (0xFFFFFFFFFFFC0000)) == 0) {// 11 111111 111111 000000 000000 000000 (NOTE: groups of SIX bits)
             WriteBase64x3(value, buffer);
            return 3;
         } else if ((value & (0xFFFFFFFFFF000000)) == 0) {// 11 111111 000000 000000 000000 000000 (NOTE: groups of SIX bits)
             WriteBase64x4(value, buffer);
            return 4;
         } else if ((value & (0xFFFFFFFFC0000000)) == 0) { // 11 000000 000000 000000 000000 000000 (NOTE: groups of SIX bits) 
             WriteBase64x5(value, buffer);
            return 5;
         } else if ((value & (0xFFFFFFF000000000)) == 0) {
             WriteBase64x6(value, buffer);
             return 6;
         } else if ((value & 0xFFFF000000000000) == 0) {
             WriteBase64x8(value, buffer);
             return 8;
         }
         WriteBase64x11(value, buffer);
         return 11;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe int WriteNullable(byte* buffer, UInt64? valueN) {
          if (valueN == null)
              return 0;
          return Write(buffer, (UInt64)valueN);
      }


      public static bool IsValidLength(int size) {
          return (size >= 1 && size <= 6 || size == 8 || size == 11);
      }

      /// <summary>
      /// Reads the specified size.
      /// </summary>
      /// <param name="size">The size.</param>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      /// <exception cref="System.Exception">Illegal size</exception>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 Read(int size, byte* ptr) {
          Debug.Assert(IsValidLength(size));
          ulong val;
          if (size == 1)
              val = ReadBase64x1(ptr);
          else if (size == 2)
              val = ReadBase64x2(ptr);
          else if (size == 3)
              val = ReadBase64x3(ptr);
          else if (size == 4)
              val = ReadBase64x4(ptr);
          else if (size == 5)
              val = ReadBase64x5(ptr);
          else if (size == 6)
              val = ReadBase64x6(ptr);
              else if (size == 8)
              val = ReadBase64x8(ptr);
          else //if (size == 11)
              val = ReadBase64x11(ptr);
          //else
          //    throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Incorrect input size, " + size + ", in UInt64 read of FasterThanJson.");
          return val;
          //var c = (Base64x5*)ptr;
          //return (UInt64)((b64d[c->b0] << 24) + (b64d[c->b1] << 18) +
          //          (b64d[c->b2] << 12) + (b64d[c->b3] << 6) + b64d[c->b4]);
      }

       /// <summary>
       /// Checks if the size of value to read is valid and then calls basic read function.
       /// </summary>
       /// <param name="size">The size of the value to read</param>
       /// <param name="ptr">Pointer where to read</param>
       /// <returns>The read value.</returns>
      public static unsafe UInt64 ReadSafe(int size, byte* ptr) {
          if (!IsValidLength(size))
              throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, "Incorrect input size, " + size + ", in UInt64 read of FasterThanJson.");
          else
              return Read(size, ptr);
      }

       /// <summary>
       /// Reads nullable unsigned long integer of the given size from the given pointer.
       /// If size is invalid, an exception is thrown.
       /// </summary>
       /// <param name="size">The size of the value to read.</param>
       /// <param name="ptr">The pointer to the place where to read.</param>
       /// <returns>The read value.</returns>
      public static unsafe UInt64? ReadNullable(int size, byte* ptr) {
          if (size == 0)
              return null;
          return ReadSafe(size, ptr);
      }

       /// <summary>
      /// Reads the base64x1.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x1(byte* ptr)
      {
         var c = (Base64x1*)ptr;
         return b64d[c->b0];
      }

      /// <summary>
      /// Reads the base64x2.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x2(byte* ptr)
      {
         var c = (Base64x2*)ptr;
         return (UInt64)((b64d[c->b0] << 6) + b64d[c->b1]);
      }

      /// <summary>
      /// Reads the base64x3.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x3(byte* ptr)
      {
         var c = (Base64x3*)ptr;
         return (UInt64)((b64d[c->b0] << 12) + (b64d[c->b1] << 6) + b64d[c->b2]);
      }

      /// <summary>
      /// Reads the base64x4.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x4(byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x5(byte* ptr)
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
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x6(byte* ptr)
      {
         var c = (Base64x6*)ptr;
         return ((((UInt64)b64d[c->b0]) << 30)) + (((UInt64)b64d[c->b1] << 24)) + (((UInt64)b64d[c->b2] << 18)) +
                   (((UInt64)b64d[c->b3] << 12)) + (((UInt64)b64d[c->b4] << 6)) + ((UInt64)b64d[c->b5]);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x8(byte* ptr) {
          var c = (Base64x11*)ptr;
          return (((UInt64)b64d[c->b0]) << 42) + (((UInt64)b64d[c->b1]) << 36) + (((UInt64)b64d[c->b2]) << 30) +
                    (((UInt64)b64d[c->b3]) << 24) + (((UInt64)b64d[c->b4]) << 18) +
                    (((UInt64)b64d[c->b5]) << 12) + (((UInt64)b64d[c->b6]) << 6) + ((UInt64)b64d[c->b7]);
      }

       /// <summary>
      /// Reads the base64x11.
      /// </summary>
      /// <param name="ptr">The PTR.</param>
      /// <returns>UInt64.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe UInt64 ReadBase64x11(byte* ptr)
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