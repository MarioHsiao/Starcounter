using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;

namespace scllvm_tests_managed {

    /// <summary>
    /// All related to Clang.
    /// </summary>
    public class ScLLVMFunctions {
        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 ScLLVMProduceModule(
            [MarshalAs(UnmanagedType.LPWStr)]String path_to_cache_dir,
            String predefined_hash_str,
            String input_code_str,
            String function_names_delimited,
            String ext_libraries_names_delimited,
            Boolean delete_sources,
            String predefined_clang_params,
            StringBuilder out_hash_65bytes,
            out float out_time_seconds,
            IntPtr[] out_func_ptrs,
            out IntPtr out_exec_module,
            out IntPtr out_codegen_engine
        );

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static void ScLLVMInit();

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static void ScLLVMDestroy(IntPtr codegen_engine);

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static bool ScLLVMIsModuleCached(
            [MarshalAs(UnmanagedType.LPWStr)]String path_to_cache_dir,
            String predefined_hash_str);

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static bool ScLLVMDeleteCachedModule(
            [MarshalAs(UnmanagedType.LPWStr)]String path_to_cache_dir,
            String predefined_hash_str);
    }

    [TestFixture]
    public class scllvm_tests_managed {

        /// <summary>
        /// Test function type.
        /// </summary>
        /// <returns></returns>
        delegate Int32 TestFunctionType(Int32 p);

        [Test]
        public unsafe void TestBasicLLVMUsage() {

            bool found_in_cache = ScLLVMFunctions.ScLLVMDeleteCachedModule("C:\\Хорошо!\\Очень!", "234253453456456");
            Assert.False(found_in_cache, "Not existing cached module is found!");

            String cur_dir = Directory.GetCurrentDirectory();
            found_in_cache = ScLLVMFunctions.ScLLVMDeleteCachedModule(cur_dir, "234253453456456");
            Assert.False(found_in_cache, "Not existing cached module is found!");

            IntPtr codegen_engine = IntPtr.Zero;
            IntPtr[] out_functions = new IntPtr[1];
            float time_took_sec = 0;
            StringBuilder out_hash_65bytes = new StringBuilder(65);
            IntPtr exec_module;
            UInt32 err_code;

            err_code = ScLLVMFunctions.ScLLVMProduceModule(
                cur_dir,
                null,
                "extern \"C\" int Func1(int x) { return 8459649 + x; }",
                "Func1",
                null,
                true,
                null,
                out_hash_65bytes,
                out time_took_sec,
                out_functions,
                out exec_module,
                out codegen_engine);

            Assert.AreEqual(err_code, 0, "Could not build simplest LLVM module!");
            Assert.AreNotEqual(exec_module, IntPtr.Zero, "Execution module is null!");
            Assert.AreNotEqual(out_functions[0], IntPtr.Zero, "No function pointer is returned from produced LLVM module!");

            TestFunctionType testFunction = (TestFunctionType)Marshal.GetDelegateForFunctionPointer(out_functions[0], typeof(TestFunctionType));
            Int32 result = testFunction(123);
            Assert.AreEqual(result, 8459772, "Wrong result returned from built function!");

            // Now checking that corresponding module is cached.
            String mod_name = out_hash_65bytes.ToString();

            found_in_cache = ScLLVMFunctions.ScLLVMIsModuleCached(cur_dir, mod_name);
            Assert.True(found_in_cache, "Cached module is not found!");

            // Deleting cached module.
            bool deleted = ScLLVMFunctions.ScLLVMDeleteCachedModule(cur_dir, mod_name);
            Assert.True(deleted, "Can't delete cached module!");

            // Trying to find again.
            found_in_cache = ScLLVMFunctions.ScLLVMIsModuleCached(cur_dir, mod_name);
            Assert.False(found_in_cache, "Deleted cached module is found again!");
        }

        [SetUp]
        public void LLVMSetup() {

            // Initializing globally LLVM first.
            ScLLVMFunctions.ScLLVMInit();
        }

        static void Main(string[] args) {

            // Basically running the same test.
            scllvm_tests_managed t = new scllvm_tests_managed();
            t.LLVMSetup();
            t.TestBasicLLVMUsage();
        }
    }
}
