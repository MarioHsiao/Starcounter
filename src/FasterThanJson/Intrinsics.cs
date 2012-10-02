

using System.Reflection.Emit;

namespace Starcounter.Internal
{
   public unsafe class Intrinsics
   {
      public delegate void MemCpyFunction(void* des, void* src, uint bytes);

      public static readonly MemCpyFunction MemCpy;

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