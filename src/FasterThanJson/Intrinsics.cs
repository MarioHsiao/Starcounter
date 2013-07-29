// ***********************************************************************
// <copyright file="Intrinsics.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Reflection.Emit;

namespace Starcounter.Internal
{
    /// <summary>
    /// Class Intrinsics
    /// </summary>
   internal unsafe class Intrinsics
   {
       /// <summary>
       /// Delegate MemCpyFunction
       /// </summary>
       /// <param name="des">The DES.</param>
       /// <param name="src">The SRC.</param>
       /// <param name="bytes">The bytes.</param>
      public delegate void MemCpyFunction(void* des, void* src, uint bytes);

      /// <summary>
      /// The mem cpy
      /// </summary>
      public static readonly MemCpyFunction MemCpy;

      /// <summary>
      /// Initializes static members of the <see cref="Intrinsics" /> class.
      /// </summary>
      static Intrinsics()
      {
         var dynamicMethod = new DynamicMethod
         (
             "MemCpy",
             typeof(void),
             new[] { typeof(void*), typeof(void*), typeof(uint) },
             typeof(Intrinsics)
         );

         var ilGenerator = dynamicMethod.GetILGenerator();

         ilGenerator.Emit(OpCodes.Ldarg_0);
         ilGenerator.Emit(OpCodes.Ldarg_1);
         ilGenerator.Emit(OpCodes.Ldarg_2);

         ilGenerator.Emit(OpCodes.Cpblk);
         ilGenerator.Emit(OpCodes.Ret);

         MemCpy = (MemCpyFunction)dynamicMethod
                     .CreateDelegate(typeof(MemCpyFunction));
      }
   }
}