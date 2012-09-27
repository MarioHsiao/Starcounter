
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    [SuppressUnmanagedCodeSecurity]
    public static class bmx
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SC_SESSION_ID
        {
            internal UInt64 low;
            internal UInt64 high;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct BMX_TASK_INFO
        {
            internal Byte flags;
            internal Byte scheduler_number;
            internal UInt16 handler_id;
            internal Byte fill1;
            internal UInt32 chunk_index;
            internal UInt64 transaction_handle;
            internal SC_SESSION_ID session_id;
        };

        public unsafe delegate UInt32 BMX_HANDLER_CALLBACK(
            UInt64 session_id,
            Byte* raw_chunk,
            BMX_TASK_INFO* task_info,
            Boolean* is_handled
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public extern static UInt32 sc_init_bmx_manager();

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_handle_incoming_chunks(sccorelib.CM2_TASK_DATA* task_data);

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_read_from_chunk(
            UInt32 chunk_index,
            Byte* raw_chunk,
            UInt32 length,
            Byte* dest_buffer,
            UInt32 dest_buffer_size
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_write_to_chunk(
            Byte* source_buffer,
            UInt32 length,
            UInt32* chunk_index,
            UInt32 offset
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_port_handler(
            UInt16 port,
            BMX_HANDLER_CALLBACK callback,
            UInt16* handler_id
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_subport_handler(
            UInt16 port,
            UInt32 subport,
            BMX_HANDLER_CALLBACK callback,
            UInt16* handler_id
        );

        [DllImport("bmx.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static UInt32 sc_bmx_register_uri_handler(
            UInt16 port,
            String url,
            Byte http_verb,
            BMX_HANDLER_CALLBACK callback,
            UInt16* handler_id
        );
    }
}