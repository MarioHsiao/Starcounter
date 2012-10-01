
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{

    [SuppressUnmanagedCodeSecurity]
    public static class sccorelog
    {

        public const uint SC_ENTRY_DEBUG = 0;

        public const uint SC_ENTRY_SUCCESS_AUDIT = 1;

        public const uint SC_ENTRY_FAILURE_AUDIT = 2;

        public const uint SC_ENTRY_NOTICE = 3;

        public const uint SC_ENTRY_WARNING = 4;

        public const uint SC_ENTRY_ERROR = 5;

        public const uint SC_ENTRY_CRITICAL = 6;
        
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint SCInitModule_LOG(ulong hmenv);

        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet=CharSet.Unicode)]
        public static extern unsafe uint SCConnectToLogs(string serverName, void* ignore1, void* ignore2, ulong* phlogs);

        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint SCBindLogsToDir(ulong hlogs, string directory);

        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint SCNewActivity();
    }
}
