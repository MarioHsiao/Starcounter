using Starcounter.Internal;
using Starcounter.Internal.Uri;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Starcounter.Rest {

    /// <summary>
    /// All related to Clang.
    /// </summary>
    public unsafe class ClangFunctions {

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 ClangCompileCodeAndGetFuntions(
            void** clang_engine,
            Boolean accumulate_old_modules,
            Boolean print_to_console,
            Boolean do_optimizations,
            Byte* code_str,
            Byte* function_names_delimited,
            IntPtr* out_func_ptrs,
            IntPtr** out_exec_module
        );

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 ClangCompileAndLoadObjectFile(
            void** clang_engine,
            Boolean print_to_console,
            Boolean do_optimizations,
            Byte* path_to_cache_dir,
            Byte* predefined_hash_str,
            Byte* input_code_str,
            Byte* function_names_delimited,
            Boolean delete_sources,
            IntPtr* out_func_ptrs,
            IntPtr** out_exec_module
        );

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static void ClangInit();

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static void ClangDestroy(void* clang_engine);

        /// <summary>
        /// Generates Clang functions.
        /// </summary>
        public static UInt32 GenerateClangFunctions(
            void** clang_engine,
            String cpp_code,
            String[] function_names,
            IntPtr[] out_functions) {

            Byte[] cpp_code_bytes = Encoding.ASCII.GetBytes(cpp_code);

            fixed (Byte* code_bytes_native = cpp_code_bytes) {

                return GenerateClangFunctions(clang_engine, code_bytes_native, function_names, out_functions);
            }
        }

        /// <summary>
        /// Generates Clang functions.
        /// </summary>
        public static UInt32 GenerateClangFunctions(
            void** clang_engine,
            Byte* cpp_code_ptr,
            String[] function_names,
            IntPtr[] out_functions) {

            String function_names_delimited = function_names[0];
            for (Int32 i = 1; i < function_names.Length; i++) {
                function_names_delimited += ";" + function_names[i];
            }

            Byte[] path_to_codegen_dir_bytes = Encoding.Unicode.GetBytes(Path.Combine(Path.GetTempPath(), "starcounter"));

            Byte[] function_names_bytes = Encoding.ASCII.GetBytes(function_names_delimited);

            fixed (Byte* function_names_bytes_native = function_names_bytes, path_to_codegen_dir_bytes_native = path_to_codegen_dir_bytes) {

                fixed (IntPtr* out_func_ptrs = out_functions) {

                    // Pointer to execution module that we don't use though.
                    IntPtr* exec_module;

                    // Compiling the given code and getting function pointer back.
                    /*UInt32 err_code = ClangFunctions.ClangCompileCodeAndGetFuntions(
                        clang_engine,
                        false,
                        false,
                        MixedCodeConstants.SCLLVM_OPT_FLAG,
                        cpp_code_ptr,
                        function_names_bytes_native,
                        out_func_ptrs,
                        &exec_module);*/

                    UInt32 err_code = ClangFunctions.ClangCompileAndLoadObjectFile(
                        clang_engine,
                        false,
                        MixedCodeConstants.SCLLVM_OPT_FLAG,
                        path_to_codegen_dir_bytes_native,
                        null,
                        cpp_code_ptr,
                        function_names_bytes_native,
                        true,
                        out_func_ptrs,
                        &exec_module);

                    if (0 != err_code) {
                        return err_code;
                    }
                }
            }

            return 0;
        }
    }

    /// <summary>
    /// All URIs registered per port.
    /// </summary>
    internal unsafe class PortUris
    {
        public delegate Int32 MatchUriDelegate(
            Byte* uri_info,
            UInt32 uri_info_len,
            MixedCodeConstants.UserDelegateParamInfo** native_params);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port"></param>
        public PortUris(UInt16 port)
        {
            port_ = port;
        }

        /// <summary>
        /// This port number.
        /// </summary>
        UInt16 port_;

        /// <summary>
        /// This port number.
        /// </summary>
        public UInt16 Port
        {
            get { return port_; }
        }

        /// <summary>
        /// Json for the generated code.
        /// </summary>
        Byte[] gen_code_string_container_ = new Byte[MixedCodeConstants.MAX_URI_MATCHING_CODE_BYTES];

        /// <summary>
        /// Pointer to create Clang engine.
        /// </summary>
        void* clang_engine_ = null;

        /// <summary>
        /// Delegate to generated match URI function.
        /// </summary>
        public volatile MatchUriDelegate matchUriAndGetHandlerIdFunc_ = null;

        /// <summary>
        /// Global init for Clang.
        /// </summary>
        public static void GlobalInit()
        {
            ClangFunctions.ClangInit();
        }

        /// <summary>
        /// Destroying this port.
        /// </summary>
        public void Destroy()
        {
            ClangFunctions.ClangDestroy(clang_engine_);
        }

        /// <summary>
        /// Generates the URI matcher for this port.
        /// </summary>
        public unsafe Boolean GenerateUriMatcher(
            UInt16 port,
            UserHandlerInfo[] allUserHandlers,
            Int32 numRegisteredHandlers)
        {
            lock (allUserHandlers)
            {
                // Checking again in case if delegate was already constructed.
                if (matchUriAndGetHandlerIdFunc_ != null)
                    return true;

                List<MixedCodeConstants.RegisteredUriManaged> registered_uri_infos = new List<MixedCodeConstants.RegisteredUriManaged>();

                try {

                    // Creating list of registered uri infos.
                    for (Int32 i = 0; i < numRegisteredHandlers; i++) {

                        // Checking if handler is not empty.
                        if (!allUserHandlers[i].IsEmpty()) {

                            // Grabbing URIs for this port only.
                            if (port == allUserHandlers[i].UriInfo.port_) {
                                registered_uri_infos.Add(allUserHandlers[i].UriInfo.GetRegisteredUriManaged());
                            }
                        }
                    }

                    // Checking if there are any URIs.
                    if (registered_uri_infos.Count <= 0)
                        return false;

                    MixedCodeConstants.RegisteredUriManaged[] registered_uri_infos_array = registered_uri_infos.ToArray();

                    // Name of the root function.
                    String root_function_name = "MatchUriForPort" + port;

                    fixed (Byte* gen_code_string_container_native = gen_code_string_container_)
                    {
                        fixed (MixedCodeConstants.RegisteredUriManaged* reg_uri_infos_array = registered_uri_infos_array)
                        {
                            UInt32 num_code_bytes = MixedCodeConstants.MAX_URI_MATCHING_CODE_BYTES - 1;

                            UInt32 err_code = UriMatcherBuilder.GenerateNativeUriMatcherManaged(
                                (UInt64)MixedCodeConstants.INVALID_SERVER_LOG_HANDLE,
                                root_function_name,
                                (IntPtr)reg_uri_infos_array,
                                (UInt32)registered_uri_infos_array.Length,
                                (IntPtr)gen_code_string_container_native,
                                ref num_code_bytes);

                            if (err_code != 0) {
                                throw ErrorCode.ToException(err_code, "Internal URI matcher code generation error: " + err_code);
                            }
                        }

                        IntPtr[] out_functions = new IntPtr[1];

                        fixed (void** clang_engine = &clang_engine_)
                        {

                            UInt32 errCode = ClangFunctions.GenerateClangFunctions(
                                clang_engine,
                                gen_code_string_container_native,
                                new String[] { root_function_name },
                                out_functions);

                            if (0 != errCode) {
                                throw new ArgumentException("GenerateClangFunctions returned error during URI matcher code generation: " + errCode);
                            }
                        }

                        // Ensuring that generated function is not null.
                        if (IntPtr.Zero == out_functions[0]) {
                            throw new ArgumentException("GenerateClangFunctions returned a NULL generated function pointer.");
                        }

                        // Getting the managed.
                        matchUriAndGetHandlerIdFunc_ = (MatchUriDelegate)Marshal.GetDelegateForFunctionPointer(out_functions[0], typeof(MatchUriDelegate));
                    }

                } finally {

                    for (Int32 i = 0; i < registered_uri_infos.Count; i++) {

                        MixedCodeConstants.RegisteredUriManaged r = registered_uri_infos[i];

                        if (r.method_space_uri != IntPtr.Zero) {
                            BitsAndBytes.Free(r.method_space_uri);
                            r.method_space_uri = IntPtr.Zero;
                        }
                    }
                }
            }

            return true;
        }
    }
}