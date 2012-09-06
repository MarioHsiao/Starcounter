
using System.Runtime.InteropServices;

namespace Starcounter.Internal
{
    
    internal static class orange // TODO: Move appropriate scheduler callback implementations to managed code.
    {

        private static ulong hmenv_;

        internal static void orange_setup(ulong hmenv)
        {
            hmenv_ = hmenv;
        }

        private static unsafe sccorelib.THREAD_ENTER th_enter = new sccorelib.THREAD_ENTER(orange_thread_enter);
        private static unsafe sccorelib.THREAD_LEAVE th_leave = new sccorelib.THREAD_LEAVE(orange_thread_leave);
        private static unsafe sccorelib.THREAD_START th_start = new sccorelib.THREAD_START(orange_thread_start);
        private static unsafe sccorelib.THREAD_RESET th_reset = new sccorelib.THREAD_RESET(orange_thread_reset);
        private static unsafe sccorelib.THREAD_YIELD th_yield = new sccorelib.THREAD_YIELD(orange_thread_yield);
        private static unsafe sccorelib.VPROC_BGTASK vp_bgtask = new sccorelib.VPROC_BGTASK(orange_vproc_bgtask);
        private static unsafe sccorelib.VPROC_CTICK vp_ctick = new sccorelib.VPROC_CTICK(orange_vproc_ctick);
        private static unsafe sccorelib.VPROC_IDLE vp_idle = new sccorelib.VPROC_IDLE(orange_vproc_idle);
        private static unsafe sccorelib.VPROC_WAIT vp_wait = new sccorelib.VPROC_WAIT(orange_vproc_wait);
        private static unsafe sccorelib.ALERT_STALL al_stall = new sccorelib.ALERT_STALL(orange_alert_stall);
        private static unsafe sccorelib.ALERT_LOWMEM al_lowmem = new sccorelib.ALERT_LOWMEM(orange_alert_lowmem);

        internal static unsafe void orange_configure_scheduler_callbacks(ref sccorelib.CM2_SETUP setup)
        {
            setup.th_enter = (void*)Marshal.GetFunctionPointerForDelegate(th_enter);
            setup.th_leave = (void*)Marshal.GetFunctionPointerForDelegate(th_leave);
            setup.th_start = (void*)Marshal.GetFunctionPointerForDelegate(th_start);
            setup.th_reset = (void*)Marshal.GetFunctionPointerForDelegate(th_reset);
            setup.th_yield = (void*)Marshal.GetFunctionPointerForDelegate(th_yield);
            setup.vp_bgtask = (void*)Marshal.GetFunctionPointerForDelegate(vp_bgtask);
            setup.vp_ctick = (void*)Marshal.GetFunctionPointerForDelegate(vp_ctick);
            setup.vp_idle = (void*)Marshal.GetFunctionPointerForDelegate(vp_idle);
            setup.vp_wait = (void*)Marshal.GetFunctionPointerForDelegate(vp_wait);
            setup.al_stall = (void*)Marshal.GetFunctionPointerForDelegate(al_stall);
            setup.al_lowmem = (void*)Marshal.GetFunctionPointerForDelegate(al_lowmem);
//            setup.pex_ctxt = null;

        }

        private static unsafe void orange_thread_enter(void* hsched, byte cpun, void* p, int init)
        {
            uint e = sccoredb.SCAttachThread(cpun, init);
            if (e == 0) return;
            orange_fatal_error(e);
        }

        private static unsafe void orange_thread_leave(void* hsched, byte cpun, void* p, uint yr)
        {
            uint e = sccoredb.SCDetachThread(yr);
            if (e == 0) return;
            orange_fatal_error(e);
        }

        private static unsafe void orange_thread_start(void* hsched, byte cpun, void* p, uint ignore)
        {
            Processor.RunMessageLoop(hsched);
        }

        private static unsafe void orange_thread_reset(void* hsched, byte cpun, void* p)
        {
            uint e = sccoredb.SCResetThread();
            if (e == 0)
            {
                sccorelog.SCNewActivity();
                return;
            }
            orange_fatal_error(e);
        }

        private static unsafe int orange_thread_yield(void* hsched, byte cpun, void* p, uint yr)
        {
            // Disallow yield because timesup or manual yields if managed
            // debugger is attached.

            switch (yr)
            {
            case sccorelib.CM5_YIELD_REASON_TIMES_UP:
            case sccorelib.CM5_YIELD_REASON_USER_INITIATED:
                return System.Diagnostics.Debugger.IsAttached ? 0 : 1;
            default:
                return 1;
            }
        }

        private static unsafe void orange_vproc_bgtask(void* hsched, byte cpun, void* p)
        {
            uint e = sccoredb.SCBackgroundTask();
            if (e == 0) return;
            orange_fatal_error(e);
        }

        private static unsafe void orange_vproc_ctick(void* hsched, byte cpun, uint psec)
        {
            sccoredb.sccoredb_advance_clock(cpun);

            // TODO: Here be session clock advance.

            if (cpun == 0)
            {
                sccorelib.mh4_menv_trim_cache(hmenv_, 1);
            }
        }

        private static unsafe int orange_vproc_idle(void* hsched, byte cpun, void* p)
        {
            int callAgainIfStillIdle;
            uint e = sccoredb.SCIdleTask(&callAgainIfStillIdle);
            if (e == 0) return callAgainIfStillIdle;
            orange_fatal_error(e);
            return 0;
        }

        private static unsafe void orange_vproc_wait(void* hsched, byte cpun, void* p) { }

        private static unsafe void orange_alert_stall(void* hsched, void* p, byte cpun, uint sr, uint sc)
        {
            // We have a stalling scheduler....
        }

        private static unsafe void orange_alert_lowmem(void* hsched, void* p, uint lr)
        {
            uint e;
            
            e = sccoredb.SCLowMemoryAlert(lr);
            if (e == 0)
            {
                byte cpun;
                e = sccorelib.cm3_get_cpun(null, &cpun);

                if (e == 0)
                {
                    // This is a worker thread.

                    return;
                }
                else
                {
                    // This is the monitor thread.

                    if (lr == sccorelib.CM5_LOWMEM_REASON_PHYSICAL_MEMORY)
                    {
                        sccorelib.mh4_menv_trim_cache(hmenv_, 0);
                    }
                }

                return;
            }

            orange_fatal_error(e);
        }

        private static void orange_fatal_error(uint e)
        {
            System.Environment.FailFast(e.ToString()); // TODO:
        }
    }
}
