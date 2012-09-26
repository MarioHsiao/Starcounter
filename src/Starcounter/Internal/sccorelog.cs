
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{

    [SuppressUnmanagedCodeSecurity]
    public static class sccorelog
    {

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
