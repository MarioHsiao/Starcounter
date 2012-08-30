
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    
    [SuppressUnmanagedCodeSecurity]
    internal static class sccoredb
    {

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern uint SCConfigSetValue(string key, string value);
 
        internal const uint SCCOREDB_LOAD_DATABASE = 0x00100000;

        internal const uint SCCOREDB_COMPLETE_INIT = 0x00200000;

        internal const uint SCCOREDB_ENABLE_CHECK_FILE_ON_LOAD = 0x00010000;

        internal const uint SCCOREDB_ENABLE_CHECK_FILE_ON_CHECKP = 0x00020000;

        internal const uint SCCOREDB_ENABLE_CHECK_FILE_ON_BACKUP = 0x00040000;

        internal const uint SCCOREDB_ENABLE_CHECK_MEMORY_ON_CHECKP = 0x00080000;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint sccoredb_connect(uint flags, void* hsched, ulong hmenv, ulong hlogs, int* pempty);

        internal const uint SCCOREDB_UNLOAD_DATABASE = 0x00200000;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint sccoredb_disconnect(uint flags);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCEndCreateDatabase();

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCEndInitializeDatabase();

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCAttachThread(byte scheduler_number, int init);

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint SCDetachThread(uint yield_reason);
    }
}
