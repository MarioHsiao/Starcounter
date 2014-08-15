#define MEMORY_LEAK_CHECK
using System;
using System.Runtime.CompilerServices;

using System.Reflection.Emit;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Starcounter.Internal {
    public static unsafe class BitsAndBytes {

        /// <summary>
        /// Safe to use with early versions of Roslyn
        /// </summary>
        static public void SlowMemCopy( IntPtr destination, byte[] source, uint bytes) {
            unsafe {
                byte* des = (byte*)destination;
                fixed (byte* src = source) {
                    for (int i = 0; i < bytes; i++) {
                        des[i] = src[i];
                    }
                }
            }
        }

        /*
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5        
        public static bool MemCompare(byte[] strA, byte[] strB, int offsetInA, int offsetInB, int bytesToCompare) {
            unsafe {
                fixed (byte* _stra = strA, _strb = strB) {
                    byte* stra = _stra + offsetInA;
                    byte* strb = _strb + offsetInB;
                    int t = 0;
                    while (bytesToCompare > 0) {
                        if (stra[t] != strb[t])
                            return false;
                        t++;
                        bytesToCompare--;
                    }
                }
            }
            return true;
        }
*/
        /// <summary>
        /// Fast memory compare function
        /// </summary>
        /// <remarks>
        /// Add nunit tests
        /// </remarks>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <param name="count">The number of bytes to compare</param>
        /// <returns>True if the memory contains the same byte values</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5        
        static unsafe bool CompareMemory(byte[] array1, byte[] array2, int count) {
            fixed (byte* fa = array1, fb = array2) {
                byte* pa = fa, pb = fb;
                int longsToCompare = count >> 3; // count / 8
                for (int i = 0; i < longsToCompare; i++, pa += 8, pb += 8) {
                    if (*((long*)pa) != *((long*)pb))
                        return false;
                }
                if ((count & 4) != 0) {
                    // Compare (some of) the trailing bytes using 32 bits
                    if (*((int*)pa) != *((int*)pb))
                        return false;
                    pa += 4;
                    pb += 4;
                }
                if ((count & 2) != 0) {
                    // Compare (some of) the trailing bytes using 16 bits
                    if (*((short*)pa) != *((short*)pb))
                        return false;
                    pa += 2;
                    pb += 2;
                }
                if ((count & 1) != 0) {
                    // Compare the last trailing odd byte using 8 bits
                    return (*((byte*)pa) == *((byte*)pb));
                }
                return true;
            }
        }

        /// <summary>
        /// Fast memory compare function
        /// </summary>
        /// <remarks>
        /// Add nunit tests
        /// </remarks>
        /// <param name="ptr1">The pointer to the first piece of memory</param>
        /// <param name="ptr2">The pointer to the second piece of memory</param>
        /// <param name="count">The number of bytes to compare</param>
        /// <returns>True if the memory contains the same byte values</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5        
        static unsafe bool CompareMemory(byte* ptr1, byte* ptr2, int count) {
            byte* pa = ptr1, pb = ptr2;
            int longsToCompare = count >> 3; // count / 8
            for (int i = 0; i < longsToCompare; i++, pa += 8, pb += 8) {
                if (*((long*)pa) != *((long*)pb))
                    return false;
            }
            if ((count & 4) != 0) {
                // Compare (some of) the trailing bytes using 32 bits
                if (*((int*)pa) != *((int*)pb))
                    return false;
                pa += 4;
                pb += 4;
            }
            if ((count & 2) != 0)
            {
                // Compare (some of) the trailing bytes using 16 bits
                if (*((short*)pa) != *((short*)pb))
                    return false;
                pa += 2;
                pb += 2;
            }
            if ((count & 1) != 0) {
                // Compare the last trailing odd byte using 8 bits
                return (*((byte*)pa) == *((byte*)pb));
            }
            return true;
        }

#if MEMORY_LEAK_CHECK
        public static Int32 NumNativeAllocations = 0;
#endif

      public static IntPtr Alloc(int size) {
#if MEMORY_LEAK_CHECK
          Interlocked.Increment(ref NumNativeAllocations);
#endif

          return System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
      }

      public static void Free(IntPtr prevAllocMemory) {
#if MEMORY_LEAK_CHECK
          Interlocked.Decrement(ref NumNativeAllocations);
#endif

          System.Runtime.InteropServices.Marshal.FreeHGlobal(prevAllocMemory);
      }

      public delegate void MemCpyFunction(byte* des, byte* src, uint bytes);

      public static readonly MemCpyFunction MemCpy;

      static BitsAndBytes()
      {
         var dynamicMethod = new DynamicMethod
         (
             "MemCpy",
             typeof(void),
             new[] { typeof(byte*), typeof(byte*), typeof(uint) },
             typeof(BitsAndBytes)
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