// ***********************************************************************
// <copyright file="bmx.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    /// <summary>
    /// Class bmx
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class bmx
    {
        /// <summary>
        /// Struct BMX_TASK_INFO
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BMX_TASK_INFO
        {
            internal UInt64 handler_info;
            internal UInt32 chunk_index;
            internal Byte flags;
            internal Byte scheduler_number;
            internal Byte client_worker_id;            
        };

        /// <summary>
        /// A callback from BMX layer.
        /// </summary>
        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        public unsafe delegate UInt32 BMX_HANDLER_CALLBACK(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            BMX_TASK_INFO* task_info,
            Boolean* is_handled
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_copy_all_chunks(
            UInt32 chunk_index,
            Byte* first_smc,
            UInt32 first_chunk_offset,
            UInt32 total_copy_bytes,
            Byte* dest_buffer,
            UInt32 dest_buffer_size
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_plain_copy_and_release_chunks(
            UInt32 first_chunk_index,
            Byte* first_chunk_data,
            Byte* buffer,
            Int32 num_available_bytes
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_obtain_new_chunk(
            UInt32* new_chunk_index,
            Byte** new_chunk_mem
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_send_buffer(
            Byte gw_worker_id,
            Byte* buf,
            Int32 buf_len_bytes,
            UInt32* the_chunk_index,
            UInt32 conn_flags
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_release_linked_chunks(UInt32 chunk_index);

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_port_handler(
            UInt16 port,
            String app_name,
            IntPtr managed_callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_subport_handler(
            UInt16 port,
            String app_name,
            UInt32 subport,
            IntPtr managed_callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_uri_handler(
            UInt16 port,
            String app_name,
            String originalUriInfo,
            String processedUriInfo,
            Byte* param_types,
            Byte num_params,
            IntPtr managed_callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo);

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_ws_handler(
            UInt16 port,
            String app_name,
            String channel_name,
            UInt32 channel_id,
            IntPtr managed_callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_port(
            UInt16 port
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_subport(
            UInt16 port,
            UInt32 subport
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_uri(
            UInt16 port,
            String originalUriInfo
        );

        /// <summary>
        /// Managed callback to handle errors.
        /// </summary>
        /// <param name="err_code"></param>
        /// <param name="err_string"></param>
        internal unsafe delegate void ErrorHandlingCallback(
            UInt32 err_code,
            Char* err_string,
            Int32 err_string_len
            );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal extern static UInt32 sc_init_bmx_manager(
            IntPtr destroy_apps_session_callback,
            IntPtr create_new_apps_session_callback,
            IntPtr error_handling_callback
            );


        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal extern static void sc_init_profilers(
            Byte numSchedulers
            );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal extern static void sc_profiler_reset(
            Byte schedId);

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal unsafe extern static UInt32 sc_profiler_get_results_in_json(
            Byte schedId,
            Byte* buf,
            Int32 bufMaxLen);
    }
}