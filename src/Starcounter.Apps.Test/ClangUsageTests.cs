// ***********************************************************************
// <copyright file="TestApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using System.Diagnostics;

using TJson = Starcounter.Templates.TObject;
using System.Text;
using Starcounter.Rest;
using System.Runtime.InteropServices;

namespace Starcounter.Clang.Tests {

    /// <summary>
    /// Class TestApp
    /// </summary>
    unsafe class TestSimple {

        /// <summary>
        /// Pointer to create Clang engine.
        /// </summary>
        static IntPtr clangEngine_ = IntPtr.Zero;

        /// <summary>
        /// Test function type.
        /// </summary>
        /// <returns></returns>
        delegate Int32 TestFunctionType();

        /// <summary>
        /// Test function type.
        /// </summary>
        /// <returns></returns>
        delegate Int32 TestFunctionSum(Int32 a, Int32 b);

        /// <summary>
        /// Tests initialization.
        /// </summary>
        [SetUp]
        public void InitEverything() {

            // Initializing Clang engine components.
            ScLLVMFunctions.ScLLVMInit();
        }

        /// <summary>
        /// Tests the set get.
        /// </summary>
        [Test]
        public static void Test1() {

            IntPtr[] out_functions_ptrs = new IntPtr[1];

            UInt32 errCode = ScLLVMFunctions.GenerateClangFunctions(
                "extern \"C\" int func1() { return 124; }",
                new String[] { "func1" },
                out_functions_ptrs,
                out clangEngine_);

            Assert.IsTrue(0 == errCode);

            // Getting the managed.
            TestFunctionType testFunction = (TestFunctionType)Marshal.GetDelegateForFunctionPointer(out_functions_ptrs[0], typeof(TestFunctionType));

            // Running the test and getting the result.
            Int32 result = testFunction();
            Assert.IsTrue(124 == result);
        }

        /// <summary>
        /// Tests the set get.
        /// </summary>
        [Test]
        public static void Test2() {

            IntPtr[] out_functions_ptrs = new IntPtr[2];

            UInt32 errCode = ScLLVMFunctions.GenerateClangFunctions(
                "extern \"C\" int func1() { return 124; }\r\n"+
                "extern \"C\" int func2() { return 125; }",
                new String[] { "func1", "func2" },
                out_functions_ptrs,
                out clangEngine_);

            Assert.IsTrue(0 == errCode);

            // Getting the managed.
            TestFunctionType testFunction1 = (TestFunctionType)Marshal.GetDelegateForFunctionPointer(out_functions_ptrs[0], typeof(TestFunctionType)),
                testFunction2 = (TestFunctionType)Marshal.GetDelegateForFunctionPointer(out_functions_ptrs[1], typeof(TestFunctionType));

            // Running the test and getting the result.
            Int32 result = testFunction1();
            Assert.IsTrue(124 == result);

            result = testFunction2();
            Assert.IsTrue(125 == result);
        }

        /// <summary>
        /// Tests the set get.
        /// </summary>
        [Test]
        public static void Test3() {

            IntPtr[] out_functions_ptrs = new IntPtr[3];

            UInt32 errCode = ScLLVMFunctions.GenerateClangFunctions(
                "extern \"C\" int func1() { return 124; }\r\n" +
                "extern \"C\" int func3(int a, int b) { return a + b; }\r\n" +
                "extern \"C\" int func2() { return 125; }\r\n" +
                "extern \"C\" int func4intrinsics() { asm(\"int3\");  __builtin_unreachable(); }\r\n",
                new String[] { "func1", "func2", "func3" },
                out_functions_ptrs,
                out clangEngine_);

            Assert.IsTrue(0 == errCode);

            // Getting the managed.
            TestFunctionType testFunction1 = (TestFunctionType)Marshal.GetDelegateForFunctionPointer(out_functions_ptrs[0], typeof(TestFunctionType)),
                testFunction2 = (TestFunctionType)Marshal.GetDelegateForFunctionPointer(out_functions_ptrs[1], typeof(TestFunctionType));

            TestFunctionSum testFunction3 = (TestFunctionSum)Marshal.GetDelegateForFunctionPointer(out_functions_ptrs[2], typeof(TestFunctionSum));

            // Running the test and getting the result.
            Int32 result = testFunction1();
            Assert.IsTrue(124 == result);

            result = testFunction2();
            Assert.IsTrue(125 == result);

            result = testFunction3(123, 123);
            Assert.IsTrue(246 == result);
        }
    }
}