// ***********************************************************************
// <copyright file="orange.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System.Runtime.InteropServices;

namespace StarcounterInternal.Hosting
{

    /// <summary>
    /// Class orange
    /// </summary>
    public static class orange
    {

        /// <summary>
        /// The hmenv_
        /// </summary>
        private static ulong hmenv_;

        /// <summary>
        /// Orange_setups the specified hmenv.
        /// </summary>
        /// <param name="hmenv">The hmenv.</param>
        public static void orange_setup(ulong hmenv)
        {
            hmenv_ = hmenv;
        }

#if false
        private static unsafe sccorelib.THREAD_ENTER th_enter = new sccorelib.THREAD_ENTER(orange_thread_enter);
        private static unsafe sccorelib.THREAD_LEAVE th_leave = new sccorelib.THREAD_LEAVE(orange_thread_leave);
#endif
        /// <summary>
        /// The th_start
        /// </summary>
        private static unsafe sccorelib.THREAD_START th_start = new sccorelib.THREAD_START(orange_thread_start);
#if false
        private static unsafe sccorelib.THREAD_RESET th_reset = new sccorelib.THREAD_RESET(orange_thread_reset);
#endif
        /// <summary>
        /// The th_yield
        /// </summary>
        private static unsafe sccorelib.THREAD_YIELD th_yield = new sccorelib.THREAD_YIELD(orange_thread_yield);
#if false
        private static unsafe sccorelib.VPROC_BGTASK vp_bgtask = new sccorelib.VPROC_BGTASK(orange_vproc_bgtask);
        private static unsafe sccorelib.VPROC_CTICK vp_ctick = new sccorelib.VPROC_CTICK(orange_vproc_ctick);
        private static unsafe sccorelib.VPROC_IDLE vp_idle = new sccorelib.VPROC_IDLE(orange_vproc_idle);
        private static unsafe sccorelib.VPROC_WAIT vp_wait = new sccorelib.VPROC_WAIT(orange_vproc_wait);
#endif
        /// <summary>
        /// The al_stall
        /// </summary>
        private static unsafe sccorelib.ALERT_STALL al_stall = new sccorelib.ALERT_STALL(orange_alert_stall);
#if false
        private static unsafe sccorelib.ALERT_LOWMEM al_lowmem = new sccorelib.ALERT_LOWMEM(orange_alert_lowmem);
#endif

        /// <summary>
        /// Sccoredbh_inits the specified hmenv.
        /// </summary>
        /// <param name="hmenv">The hmenv.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredbh.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint sccoredbh_init(ulong hmenv);

        /// <summary>
        /// Orange_configure_scheduler_callbackses the specified setup.
        /// </summary>
        /// <param name="setup">The setup.</param>
        public static unsafe void orange_configure_scheduler_callbacks(ref sccorelib.CM2_SETUP setup)
        {
            sccoredbh_init(hmenv_);

            void* hModule = Kernel32.LoadLibraryA("sccoredbh.dll");
            setup.th_enter = Kernel32.GetProcAddress(hModule, "sccoredbh_thread_enter");
            setup.th_leave = Kernel32.GetProcAddress(hModule, "sccoredbh_thread_leave");
            setup.th_reset = Kernel32.GetProcAddress(hModule, "sccoredbh_thread_reset");
            setup.vp_bgtask = Kernel32.GetProcAddress(hModule, "sccoredbh_vproc_bgtask");
            setup.vp_ctick = Kernel32.GetProcAddress(hModule, "sccoredbh_vproc_ctick");
            setup.vp_idle = Kernel32.GetProcAddress(hModule, "sccoredbh_vproc_idle");
            setup.vp_wait = Kernel32.GetProcAddress(hModule, "sccoredbh_vproc_wait");
            setup.al_lowmem = Kernel32.GetProcAddress(hModule, "sccoredbh_alert_lowmem");

#if false
            setup.th_enter = (void*)Marshal.GetFunctionPointerForDelegate(th_enter);
            setup.th_leave = (void*)Marshal.GetFunctionPointerForDelegate(th_leave);
#endif
            setup.th_start = (void*)Marshal.GetFunctionPointerForDelegate(th_start);
#if false
            setup.th_reset = (void*)Marshal.GetFunctionPointerForDelegate(th_reset);
#endif
            setup.th_yield = (void*)Marshal.GetFunctionPointerForDelegate(th_yield);
#if false
            setup.vp_bgtask = (void*)Marshal.GetFunctionPointerForDelegate(vp_bgtask);
            setup.vp_ctick = (void*)Marshal.GetFunctionPointerForDelegate(vp_ctick);
            setup.vp_idle = (void*)Marshal.GetFunctionPointerForDelegate(vp_idle);
            setup.vp_wait = (void*)Marshal.GetFunctionPointerForDelegate(vp_wait);
#endif
            setup.al_stall = (void*)Marshal.GetFunctionPointerForDelegate(al_stall);
#if false
            setup.al_lowmem = (void*)Marshal.GetFunctionPointerForDelegate(al_lowmem);
#endif
            //setup.pex_ctxt = null;

        }

        /// <summary>
        /// The on_new_schema
        /// </summary>
        private static sccoredb.ON_NEW_SCHEMA on_new_schema = new sccoredb.ON_NEW_SCHEMA(orange_on_new_schema);

        private static sccoredb.ON_NO_TRANSACTION on_new_transaction = new sccoredb.ON_NO_TRANSACTION(orange_on_no_transaction);

        /// <summary>
        /// </summary>
        public static unsafe void orange_configure_database_callbacks(ref sccoredb.sccoredb_callbacks callbacks)
        {
            callbacks.on_new_schema = (void*)Marshal.GetFunctionPointerForDelegate(on_new_schema);
            callbacks.on_no_transaction = (void*)Marshal.GetFunctionPointerForDelegate(on_new_transaction);
        }

#if false
        private static unsafe void orange_thread_enter(void* hsched, byte cpun, void* p, int init)
        {
            uint r;
            r = sccoredb.SCAttachThread(cpun, init);
            if (r == 0) return;
            orange_fatal_error(r);
        }

        private static unsafe void orange_thread_leave(void* hsched, byte cpun, void* p, uint yr)
        {
            uint e = sccoredb.SCDetachThread(yr);
            if (e == 0) return;
            orange_fatal_error(e);
        }
#endif

        /// <summary>
        /// Called when [thread start].
        /// </summary>
        /// <param name="sf">The sf.</param>
        private static void OnThreadStart(uint sf)
        {
            if ((sf & sccorelib.CM5_START_FLAG_FIRST_THREAD) != 0)
            {
                uint r = sccoredb.SCConfigureVP();
                if (r == 0) return;
                orange_fatal_error(r);
            }
        }

        /// <summary>
        /// Orange_thread_starts the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <param name="sf">The sf.</param>
        private static unsafe void orange_thread_start(void* hsched, byte cpun, void* p, uint sf)
        {
            OnThreadStart(sf);
            Processor.RunMessageLoop(hsched);
        }

#if false
        private static unsafe void orange_thread_reset(void* hsched, byte cpun, void* p)
        {
            uint e = sccoredb.SCResetThread();
            if (e == 0)
            {
                return;
            }
            orange_fatal_error(e);
        }
#endif

        /// <summary>
        /// Orange_thread_yields the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <param name="yr">The yr.</param>
        /// <returns>System.Int32.</returns>
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

#if false
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
#endif

        /// <summary>
        /// Orange_alert_stalls the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="p">The p.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="sr">The sr.</param>
        /// <param name="sc">The sc.</param>
        private static unsafe void orange_alert_stall(void* hsched, void* p, byte cpun, uint sr, uint sc)
        {
            // We have a stalling scheduler....
        }

#if false
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
#endif

        /// <summary>
        /// Orange_on_new_schemas the specified generation.
        /// </summary>
        /// <param name="generation">The generation.</param>
        private static void orange_on_new_schema(ulong generation)
        {
            // Thread is yield blocked. Thread is always attached.

            try
            {
                Starcounter.ThreadData.Current.Scheduler.InvalidateCache(generation);
            }
            catch (System.Exception ex)
            {
                if (!ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }

        private static uint orange_on_no_transaction() {
            try {
                Starcounter.Transaction.NewCurrent(true);
                return 0;
            }
            catch (System.OutOfMemoryException) {
                return Starcounter.Error.SCERROUTOFMEMORY;
            }
            catch (System.Exception ex) {
                if (!ExceptionManager.HandleUnhandledException(ex)) throw;
                return Starcounter.Error.SCERRUNSPECIFIED;
            }
        }

        /// <summary>
        /// Orange_fatal_errors the specified e.
        /// </summary>
        /// <param name="e">The e.</param>
        private static void orange_fatal_error(uint e)
        {
            sccoreapp.sccoreapp_log_critical_code(e);
            Kernel32.ExitProcess(e);
        }
    }

    /// <summary>
    /// Class orange_nodb
    /// </summary>
    public static class orange_nodb
    {

        /// <summary>
        /// The hmenv_
        /// </summary>
        private static ulong hmenv_;

        /// <summary>
        /// Orange_setups the specified hmenv.
        /// </summary>
        /// <param name="hmenv">The hmenv.</param>
        public static void orange_setup(ulong hmenv)
        {
            hmenv_ = hmenv;
        }

        /// <summary>
        /// The th_enter
        /// </summary>
        private static unsafe sccorelib.THREAD_ENTER th_enter = new sccorelib.THREAD_ENTER(orange_thread_enter);
        /// <summary>
        /// The th_leave
        /// </summary>
        private static unsafe sccorelib.THREAD_LEAVE th_leave = new sccorelib.THREAD_LEAVE(orange_thread_leave);
        /// <summary>
        /// The th_start
        /// </summary>
        private static unsafe sccorelib.THREAD_START th_start = new sccorelib.THREAD_START(orange_thread_start);
        /// <summary>
        /// The th_reset
        /// </summary>
        private static unsafe sccorelib.THREAD_RESET th_reset = new sccorelib.THREAD_RESET(orange_thread_reset);
        /// <summary>
        /// The th_yield
        /// </summary>
        private static unsafe sccorelib.THREAD_YIELD th_yield = new sccorelib.THREAD_YIELD(orange_thread_yield);
        /// <summary>
        /// The vp_bgtask
        /// </summary>
        private static unsafe sccorelib.VPROC_BGTASK vp_bgtask = new sccorelib.VPROC_BGTASK(orange_vproc_bgtask);
        /// <summary>
        /// The vp_ctick
        /// </summary>
        private static unsafe sccorelib.VPROC_CTICK vp_ctick = new sccorelib.VPROC_CTICK(orange_vproc_ctick);
        /// <summary>
        /// The vp_idle
        /// </summary>
        private static unsafe sccorelib.VPROC_IDLE vp_idle = new sccorelib.VPROC_IDLE(orange_vproc_idle);
        /// <summary>
        /// The vp_wait
        /// </summary>
        private static unsafe sccorelib.VPROC_WAIT vp_wait = new sccorelib.VPROC_WAIT(orange_vproc_wait);
        /// <summary>
        /// The al_stall
        /// </summary>
        private static unsafe sccorelib.ALERT_STALL al_stall = new sccorelib.ALERT_STALL(orange_alert_stall);
        /// <summary>
        /// The al_lowmem
        /// </summary>
        private static unsafe sccorelib.ALERT_LOWMEM al_lowmem = new sccorelib.ALERT_LOWMEM(orange_alert_lowmem);

        /// <summary>
        /// Orange_configure_scheduler_callbackses the specified setup.
        /// </summary>
        /// <param name="setup">The setup.</param>
        public static unsafe void orange_configure_scheduler_callbacks(ref sccorelib.CM2_SETUP setup)
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
            //setup.pex_ctxt = null;

        }

        /// <summary>
        /// Orange_thread_enters the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <param name="init">The init.</param>
        private static unsafe void orange_thread_enter(void* hsched, byte cpun, void* p, int init) { }

        /// <summary>
        /// Orange_thread_leaves the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <param name="yr">The yr.</param>
        private static unsafe void orange_thread_leave(void* hsched, byte cpun, void* p, uint yr) { }

        /// <summary>
        /// Orange_thread_starts the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <param name="ignore">The ignore.</param>
        private static unsafe void orange_thread_start(void* hsched, byte cpun, void* p, uint ignore)
        {
            Processor.RunMessageLoop(hsched);
        }

        /// <summary>
        /// Orange_thread_resets the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        private static unsafe void orange_thread_reset(void* hsched, byte cpun, void* p) { }

        /// <summary>
        /// Orange_thread_yields the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <param name="yr">The yr.</param>
        /// <returns>System.Int32.</returns>
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

        /// <summary>
        /// Orange_vproc_bgtasks the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        private static unsafe void orange_vproc_bgtask(void* hsched, byte cpun, void* p) { }

        /// <summary>
        /// Orange_vproc_cticks the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="psec">The psec.</param>
        private static unsafe void orange_vproc_ctick(void* hsched, byte cpun, uint psec)
        {
            if (cpun == 0)
            {
                sccorelib.mh4_menv_trim_cache(hmenv_, 1);
            }
        }

        /// <summary>
        /// Orange_vproc_idles the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        /// <returns>System.Int32.</returns>
        private static unsafe int orange_vproc_idle(void* hsched, byte cpun, void* p)
        {
            return 0;
        }

        /// <summary>
        /// Orange_vproc_waits the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="p">The p.</param>
        private static unsafe void orange_vproc_wait(void* hsched, byte cpun, void* p) { }

        /// <summary>
        /// Orange_alert_stalls the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="p">The p.</param>
        /// <param name="cpun">The cpun.</param>
        /// <param name="sr">The sr.</param>
        /// <param name="sc">The sc.</param>
        private static unsafe void orange_alert_stall(void* hsched, void* p, byte cpun, uint sr, uint sc) { }

        /// <summary>
        /// Orange_alert_lowmems the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="p">The p.</param>
        /// <param name="lr">The lr.</param>
        private static unsafe void orange_alert_lowmem(void* hsched, void* p, uint lr)
        {
            byte cpun;
            uint r = sccorelib.cm3_get_cpun(null, &cpun);

            if (r == 0)
            {
                // This is a worker thread.

                return;
            }
            else
            {
                // This is the monitor thread.

                if (lr == sccorelib.CM5_LOWMEM_REASON_PHYSICAL_MEMORY)
                {
                    sccorelib.mh4_menv_alert_lowmem(hmenv_);
                    sccorelib.mh4_menv_trim_cache(hmenv_, 0);
                }
            }
        }

        /// <summary>
        /// Orange_fatal_errors the specified e.
        /// </summary>
        /// <param name="e">The e.</param>
        private static void orange_fatal_error(uint e)
        {
            sccoreapp.sccoreapp_log_critical_code(e);
            Kernel32.ExitProcess(e);
        }
    }
}
