
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{

    [SuppressUnmanagedCodeSecurity]
    internal static class sccorelib
    {

        internal unsafe delegate void THREAD_ENTER(void *hsched, byte cpun, void *p, int init);

        internal unsafe delegate void THREAD_LEAVE(void *hsched, byte cpun, void *p, uint yr);

        internal unsafe delegate void THREAD_START(void *hsched, byte cpun, void *p, uint ignore);

        internal unsafe delegate void THREAD_RESET(void *hsched, byte cpun, void *p);

        internal unsafe delegate int THREAD_YIELD(void *hsched, byte cpun, void *p, uint yr);

        internal unsafe delegate void VPROC_BGTASK(void *hsched, byte cpun, void *p);

        internal unsafe delegate void VPROC_CTICK(void *hsched, byte cpun, uint psec);

        internal unsafe delegate int VPROC_IDLE(void *hsched, byte cpun, void *p);

        internal unsafe delegate void VPROC_WAIT(void *hsched, byte cpun, void *p);

        internal const uint CM5_STALL_REASON_UNRESPOSIVE_THREAD = 1;

        internal const uint CM5_STALL_REASON_THREADS_BLOCKED = 2;

        internal const uint CM5_STALL_REASON_UNYIELDING_THREAD = 3;

        internal unsafe delegate void ALERT_STALL(void *hsched, void *p, byte cpun, uint sr, uint sc);

        internal const uint CM5_LOWMEM_REASON_PHYSICAL_MEMORY = 1;

        internal const uint CM5_LOWMEM_REASON_ADDRESS_SPACE = 2;

        internal unsafe delegate void ALERT_LOWMEM(void *hsched, void *p, uint lr);

        internal const uint CM5_YIELD_REASON_TIMES_UP = 1;

        internal const uint CM5_YIELD_REASON_USER_INITIATED = 2;

        internal const uint CM5_YIELD_REASON_INTERRUPTED = 3;

        internal const uint CM5_YIELD_REASON_DETACHED = 4;

        internal const uint CM5_YIELD_REASON_BLOCKED = 5;

        internal const uint CM5_YIELD_REASON_SUSPENDED = 6;

        internal const uint CM5_YIELD_REASON_RELEASED = 7;


        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct CM2_SETUP
        {
            internal char* name;
            internal char* db_data_dir_path;
            internal char* server_name;
            internal void* mem;
            internal uint mem_size;
            internal uint num_shm_chunks;
            internal byte cpuc;
            internal ulong hmenv;
            internal int is_system;
            internal void* th_enter;
            internal void* th_leave;
            internal void* th_start;
            internal void* th_reset;
            internal void* th_yield;
            internal void* vp_bgtask;
            internal void* vp_ctick;
            internal void* vp_idle;
            internal void* vp_wait;
            internal void* al_stall;
            internal void* al_lowmem;
            internal void* pex_ctxt;
        }


        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_setup(CM2_SETUP *psetup, void **phsched);


        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_start(void* hsched);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_stop(void* hsched, int wait);

#if false
        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_get_cpun(void *hshed, byte *pcpun);
#endif

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm3_get_cpun(void *ignore, byte *pcpun);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_schedule(
	        void *hsched,
	        byte cpun,
            ushort type,
	        ushort prio,
	        uint output1,
            ulong output2,
	        ulong output3
        	);

        internal const ushort CM2_TYPE_RELEASE = 0x0000;

        internal const ushort CM2_TYPE_CLOCK = 0x0001;

        internal const ushort CM2_TYPE_REQUEST = 0x0002;

        internal const ushort CM2_TYPE_CALLBACK = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        internal struct CM2_TASK_DATA
        {
            public ushort Type;
            public ushort Prio;
            public uint Output1;
            public ulong Output2;
            public ulong Output3;
        };

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_standby(void* hsched, CM2_TASK_DATA* ptask_data);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        private unsafe extern static UInt32 cm2_get_cpuc(IntPtr h, Byte* pcpuc);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        private unsafe extern static UInt32 cm2_get_cpun(IntPtr h, Byte* pcpun);

#if false
        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 cm3_bdetach(IntPtr h_opt);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 cm3_edetach(IntPtr h_opt);
#endif

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 cm3_eautodet(IntPtr h_opt);

#if false
        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 cm3_set_yblk(IntPtr h_opt);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static UInt32 cm3_rel_yblk(IntPtr h_opt);
#endif

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static unsafe UInt32 cm3_get_stash(void *ignore, UInt32** ppstash);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe ulong mh4_menv_create(void* mem128, uint slabs);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void mh4_menv_trim_cache(ulong hmenv, int periodic);

        internal unsafe static Byte GetCpuCount(IntPtr handle)
        {
            byte cpuc;
            uint e = sccorelib.cm2_get_cpuc(handle, &cpuc);
            if (e == 0) return cpuc;
            throw ErrorCode.ToException(e);
        }

        internal unsafe static byte GetCpuNumber()
        {
            byte cpun;
            uint e = sccorelib.cm3_get_cpun(null, &cpun);
            if (e == 0) return cpun;
            throw ErrorCode.ToException(e);
        }

        internal unsafe static uint* GetStateShare()
        {
            uint* pstash;
            uint e = sccorelib.cm3_get_stash(null, &pstash);
            if (e == 0) return pstash;
            throw ErrorCode.ToException(e);
        }
    };

    internal static class sccorelib_ext
    {

        internal const ushort TYPE_PROCESS_PACKAGE = 0x0100;
    }
}
