// ***********************************************************************
// <copyright file="bmx.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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
        /// Struct SC_SESSION_ID
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SC_SESSION_ID
        {
            /// <summary>
            /// The low
            /// </summary>
            internal UInt64 low;
            /// <summary>
            /// The high
            /// </summary>
            internal UInt64 high;
        };

        /// <summary>
        /// Struct BMX_TASK_INFO
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BMX_TASK_INFO
        {
            /// <summary>
            /// The flags
            /// </summary>
            internal Byte flags;
            /// <summary>
            /// The scheduler_number
            /// </summary>
            internal Byte scheduler_number;
            /// <summary>
            /// The handler_id
            /// </summary>
            internal UInt16 handler_id;
            /// <summary>
            /// The fill1
            /// </summary>
            internal Byte fill1;
            /// <summary>
            /// The chunk_index
            /// </summary>
            internal UInt32 chunk_index;
            /// <summary>
            /// The transaction_handle
            /// </summary>
            internal UInt64 transaction_handle;
            /// <summary>
            /// The session_id
            /// </summary>
            internal SC_SESSION_ID session_id;
        };

        /// <summary>
        /// Delegate BMX_HANDLER_CALLBACK
        /// </summary>
        /// <param name="session_id">The session_id.</param>
        /// <param name="raw_chunk">The raw_chunk.</param>
        /// <param name="task_info">The task_info.</param>
        /// <param name="is_handled">The is_handled.</param>
        /// <returns>UInt32.</returns>
        public unsafe delegate UInt32 BMX_HANDLER_CALLBACK(
            UInt64 session_id,
            Byte* raw_chunk,
            BMX_TASK_INFO* task_info,
            Boolean* is_handled
        );

        /// <summary>
        /// Sc_init_bmx_managers this instance.
        /// </summary>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 sc_init_bmx_manager(HttpStructs.GlobalSessions.DestroyAppsSessionCallback dasc);

        /// <summary>
        /// Sc_wait_for_bmx_readies this instance.
        /// </summary>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static void sc_wait_for_bmx_ready();

        /// <summary>
        /// Sc_handle_incoming_chunkses the specified task_data.
        /// </summary>
        /// <param name="task_data">The task_data.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_handle_incoming_chunks(sccorelib.CM2_TASK_DATA* task_data);

        /// <summary>
        /// Sc_bmx_read_from_chunks the specified chunk_index.
        /// </summary>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <param name="raw_chunk">The raw_chunk.</param>
        /// <param name="length">The length.</param>
        /// <param name="dest_buffer">The dest_buffer.</param>
        /// <param name="dest_buffer_size">The dest_buffer_size.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_read_from_chunk(
            UInt32 chunk_index,
            Byte* raw_chunk,
            UInt32 length,
            Byte* dest_buffer,
            UInt32 dest_buffer_size
        );

        /// <summary>
        /// sc_bmx_copy_all_chunks the specified chunk_index.
        /// </summary>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_copy_all_chunks(
            UInt32 chunk_index,
            Byte* first_smc,
            UInt32 first_chunk_offset,
            UInt32 total_copy_bytes,
            Byte* dest_buffer,
            UInt32 dest_buffer_size
        );

        /// <summary>
        /// sc_bmx_plain_copy_and_release_chunks the specified chunk_index.
        /// </summary>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_plain_copy_and_release_chunks(
            UInt32 first_chunk_index,
            Byte* first_chunk_data,
            Byte* buffer
        );

        /// <summary>
        /// Sc_bmx_send_buffers the specified buf.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <param name="buf_len_bytes">The buf_len_bytes.</param>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <param name="chunk_memory">The chunk_memory.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_send_buffer(
            Byte* buf,
            UInt32 buf_len_bytes,
            UInt32* chunk_index,
            Byte* chunk_memory
        );

        /// <summary>
        /// sc_bmx_release_linked_chunks.
        /// </summary>
        /// <param name="chunk_index">The chunk_index.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_release_linked_chunks(UInt32 chunk_index);

        /// <summary>
        /// Sc_bmx_register_port_handlers the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="handler_id">The handler_id.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_port_handler(
            UInt16 port,
            BMX_HANDLER_CALLBACK callback,
            UInt64* handler_id
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_port(
            UInt16 port
        );

        /// <summary>
        /// Sc_bmx_register_subport_handlers the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="subport">The subport.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="handler_id">The handler_id.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_subport_handler(
            UInt16 port,
            UInt32 subport,
            BMX_HANDLER_CALLBACK callback,
            UInt64* handler_id
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_subport(
            UInt16 port,
            UInt32 subport
        );

        /// <summary>
        /// Sc_bmx_register_uri_handlers the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="url">The URL.</param>
        /// <param name="http_verb">The http_verb.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="handler_id">The handler_id.</param>
        /// <returns>UInt32.</returns>
        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_uri_handler(
            UInt16 port,
            String originalUriInfo,
            String processedUriInfo,
            Byte http_method,
            Byte* param_types,
            Byte num_params,
            BMX_HANDLER_CALLBACK callback,
            UInt64* handler_id
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_unregister_uri(
            UInt16 port,
            String originalUriInfo
        );
    }
}