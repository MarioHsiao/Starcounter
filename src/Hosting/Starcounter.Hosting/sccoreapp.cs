
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace StarcounterInternal.Hosting
{

    [SuppressUnmanagedCodeSecurity]
    public static class sccoreapp
    {

        [DllImport("sccoreapp.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint sccoreapp_init(void* hlogs);

        [DllImport("sccoreapp.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint sccoreapp_standby(void* hsched, sccorelib.CM2_TASK_DATA* ptask_data);
    }
}
