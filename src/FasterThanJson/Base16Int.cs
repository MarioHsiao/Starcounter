// ***********************************************************************
// <copyright file="Base16Int.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Starcounter.Internal
{
    /// <summary>
    /// Struct Base16x1
    /// </summary>
   public struct Base16x1
   {
       /// <summary>
       /// The b0
       /// </summary>
      public byte b0;
   }

   /// <summary>
   /// Class Base16Int
   /// </summary>
   public class Base16Int
   {
       /// <summary>
       /// The const5
       /// </summary>
      public const byte Const5 = (byte)'5';
      /// <summary>
      /// The const6
      /// </summary>
      public const byte Const6 = (byte)'6';

      /// <summary>
      /// Writes the base16x1.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="c">The c.</param>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe void WriteBase16x1(UInt32 value, byte* c)
      {
         c[0] = (byte)(value |= 0x30);
      }

      // [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      /// <summary>
      /// Reads the base16x1.
      /// </summary>
      /// <param name="c">The c.</param>
      /// <returns>System.Int32.</returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
      public static unsafe int ReadBase16x1(Base16x1* c)
      {
         return (c->b0 & 0x0F);
      }

   }
}
