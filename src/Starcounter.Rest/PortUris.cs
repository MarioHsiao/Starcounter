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
    public class ScLLVMFunctions {

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 ScLLVMProduceModule(
            [MarshalAs(UnmanagedType.LPWStr)]String path_to_cache_dir,
            [MarshalAs(UnmanagedType.LPWStr)]String cache_sub_dir,
            String predefined_hash_str,
            String code_to_build,
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

        [DllImport("scllvm.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 ScLLVMCalculateHash(
            String code_to_build,
            StringBuilder out_hash_65bytes
        );

        /// <summary>
        /// Generates Clang functions.
        /// </summary>
        public static UInt32 GenerateClangFunctions(
            String code_to_build,
            String[] function_names,
            IntPtr[] out_functions,
            out IntPtr clang_engine) {

            String function_names_delimited = function_names[0];
            for (Int32 i = 1; i < function_names.Length; i++) {
                function_names_delimited += ";" + function_names[i];
            }

            String dbName = StarcounterEnvironment.DatabaseNameLower;

            // Checking for the case of unit tests.
            if (null == dbName) {
                dbName = "nodbname";
            }

            // Pointer to execution module that we don't use though.
            IntPtr out_exec_module;
            float time_took_sec;

            UInt32 err_code = ScLLVMFunctions.ScLLVMProduceModule(
                null,
                dbName + "\\self",
                null,
                code_to_build,
                function_names_delimited,
                null,
                true,
                "-O3 -Wall -Wno-unused-variable", // predefined_clang_params (all except -mcmodel=large even -O3 needs to be supplied)
                null,
                out time_took_sec,
                out_functions,
                out out_exec_module,
                out clang_engine);

            if (0 != err_code) {
                return err_code;
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
        IntPtr clang_engine_ = IntPtr.Zero;

        /// <summary>
        /// Delegate to generated match URI function.
        /// </summary>
        public volatile MatchUriDelegate matchUriAndGetHandlerIdFunc_ = null;

        /// <summary>
        /// Global init for Clang.
        /// </summary>
        public static void GlobalInit()
        {
            ScLLVMFunctions.ScLLVMInit();
        }

        /// <summary>
        /// Destroying this port.
        /// </summary>
        public void Destroy()
        {
            ScLLVMFunctions.ScLLVMDestroy(clang_engine_);
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

                    // Generated code to build in LLVM.
                    String code_to_build;

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

                            // Creating managed string from generated code.
                            code_to_build = Marshal.PtrToStringAnsi(new IntPtr(gen_code_string_container_native), (Int32)num_code_bytes);
                        }

                        IntPtr[] out_functions = new IntPtr[1];

                        UInt32 errCode = ScLLVMFunctions.GenerateClangFunctions(
                            code_to_build,
                            new String[] { root_function_name },
                            out_functions,
                            out clang_engine_);

                        if (0 != errCode) {
                            throw new ArgumentException("GenerateClangFunctions returned error during URI matcher code generation: " + errCode);
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