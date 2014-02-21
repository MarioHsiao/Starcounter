﻿// ***********************************************************************
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
            internal Byte flags;
            internal Byte scheduler_number;
            internal UInt16 handler_id;
            internal Byte client_worker_id;
            internal UInt32 chunk_index;
        };

        /// <summary>
        /// A callback from BMX layer.
        /// </summary>
        public unsafe delegate UInt32 BMX_HANDLER_CALLBACK(
            UInt16 managed_handler_id,
            Byte* raw_chunk,
            BMX_TASK_INFO* task_info,
            Boolean* is_handled
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 sc_init_bmx_manager(
            GlobalSessions.DestroyAppsSessionCallback destroy_apps_session_callback,
            GlobalSessions.CreateNewAppsSessionCallback create_new_apps_session_callback,
            Diagnostics.ErrorHandlingCallback error_handling_callback
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
            Byte* buffer
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
            UInt32 buf_len_bytes,
            UInt32* the_chunk_index,
            UInt32 conn_flags
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_release_linked_chunks(UInt32 chunk_index);

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_port_handler(
            UInt16 port,
            BMX_HANDLER_CALLBACK callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_port(
            UInt16 port
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_subport_handler(
            UInt16 port,
            UInt32 subport,
            BMX_HANDLER_CALLBACK callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_subport(
            UInt16 port,
            UInt32 subport
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_uri_handler(
            UInt16 port,
            String originalUriInfo,
            String processedUriInfo,
            Byte* param_types,
            Byte num_params,
            BMX_HANDLER_CALLBACK callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo);

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_ws_handler(
            UInt16 port,
            String channel_name,
            UInt32 channel_id,
            BMX_HANDLER_CALLBACK callback,
            UInt16 managed_handler_id,
            out UInt64 handlerInfo
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_uri(
            UInt16 port,
            String originalUriInfo
        );
    }
}