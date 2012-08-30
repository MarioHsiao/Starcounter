
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{

    [StructLayout(LayoutKind.Sequential, Pack=8)]
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

    [SuppressUnmanagedCodeSecurity]
    internal static class sccorelib
    {

        internal const uint CM5_YIELD_REASON_RELEASED = 7;

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint cm2_setup(CM2_SETUP *psetup, void **phsched);

        [DllImport("sccorelib.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe ulong mh4_menv_create(void* mem128, uint slabs);
    };
}
