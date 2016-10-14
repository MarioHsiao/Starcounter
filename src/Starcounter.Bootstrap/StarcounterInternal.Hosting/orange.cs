// ***********************************************************************
// <copyright file="orange.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StarcounterInternal.Hosting
{
    /// <summary>
    /// Class orange
    /// </summary>
    public static class orange
    {

        private static unsafe sccorelib.THREAD_ENTER th_enter = new sccorelib.THREAD_ENTER(orange_thread_enter);
        private static unsafe sccorelib.THREAD_LEAVE th_leave = new sccorelib.THREAD_LEAVE(orange_thread_leave);

        /// <summary>
        /// The th_start
        /// </summary>
        private static unsafe sccorelib.THREAD_START th_start = new sccorelib.THREAD_START(orange_thread_start);
        private static unsafe sccorelib.THREAD_RESET th_reset = new sccorelib.THREAD_RESET(orange_thread_reset);
        /// <summary>
        /// The th_yield
        /// </summary>
        private static unsafe sccorelib.THREAD_YIELD th_yield = new sccorelib.THREAD_YIELD(orange_thread_yield);
        private static unsafe sccorelib.VPROC_IDLE vp_idle = new sccorelib.VPROC_IDLE(orange_vproc_idle);

        public static void GetRuntimeImageSymbols(out uint imageVersion, out uint magic) {
            throw new System.NotSupportedException();
        }

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
            setup.vp_idle = (void*)Marshal.GetFunctionPointerForDelegate(vp_idle);
            //setup.pex_ctxt = null;
        }

        /// <summary>
        /// </summary>
        private static sccoredb.ON_INDEX_UPDATED orange_on_index_updated = new sccoredb.ON_INDEX_UPDATED(on_index_updated);

        /// <summary>
        /// </summary>
        public static unsafe void orange_configure_database_callbacks(ref sccoredb.sccoredb_callbacks callbacks)
        {
            callbacks.on_index_updated = (void*)Marshal.GetFunctionPointerForDelegate(orange_on_index_updated);
        }

        /// <summary>
        /// Called when [thread start].
        /// </summary>
        /// <param name="sf">The sf.</param>
        private static void OnThreadStart(uint sf) {
            if ((sf & sccorelib.CM5_START_FLAG_FIRST_THREAD) != 0) {
                // First thread started by scheduler.
            }
        }

        /// <summary>
        /// </summary>
        private static unsafe void orange_thread_enter(void* hsched, byte cpun, void* p, int init) {
            Debug.Assert(ThreadData.contextHandle_ == 0); // Only called on a detached thread.
            ulong contextHandle;
            uint r = sccoredb.star_get_context(cpun, &contextHandle);
            if (r == 0) {
                ThreadData.contextHandle_ = contextHandle;

                ulong storedTransactionHandle = ThreadData.storedTransactionHandle_;
                ThreadData.storedTransactionHandle_ = 0;
                sccoredb.star_context_set_transaction( // Can not fail.
                    contextHandle, storedTransactionHandle
                    );
                return;
            }
            orange_fatal_error(r);
        }

        /// <summary>
        /// </summary>
        private static unsafe void orange_thread_leave(void* hsched, byte cpun, void* p, uint yr) {
            ulong contextHandle = ThreadData.contextHandle_;
            Debug.Assert(contextHandle != 0); // Only called on an attached thread.
            ThreadData.contextHandle_ = 0;

            ulong currentTransactionHandle;
            sccoredb.star_context_get_transaction( // Can not fail.
                contextHandle, out currentTransactionHandle
                );
            if (currentTransactionHandle == 0) return;
            ThreadData.storedTransactionHandle_ = currentTransactionHandle;
            sccoredb.star_context_set_transaction(contextHandle, 0); // Can not fail.
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

        private static unsafe void orange_thread_reset(void* hsched, byte cpun, void* p) {
            Debug.Assert(ThreadData.storedTransactionHandle_ == 0);
            ulong contextHandle = ThreadData.contextHandle_;
            Debug.Assert(contextHandle != 0); // Only called on an attached thread.
            sccoredb.star_context_set_transaction(contextHandle, 0); // Can not fail.
        }

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

        private static unsafe int orange_vproc_idle(void* hsched, byte cpun, void* p) { return 0; }

        private static void SetYieldBlock() {
            uint r = sccorelib.cm3_set_yblk((System.IntPtr)0);
            if (r == 0) return;
            orange_fatal_error(r);
        }

        private static void ReleaseYieldBlock() {
            uint r = sccorelib.cm3_rel_yblk((System.IntPtr)0);
            if (r == 0) return;
            orange_fatal_error(r);
        }

        /// <summary>
        /// </summary>
        private static void on_index_updated(uint context_index, ulong generation) {
            // Thread is not allowed to yield while executing callback.

            SetYieldBlock();
            try {
                try {
                    Starcounter.ThreadData.Current.Scheduler.InvalidateCache(generation);
                }
                catch (System.Exception ex) {
                    if (!ExceptionManager.HandleUnhandledException(ex)) throw;
                }
            }
            finally {
                ReleaseYieldBlock();
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
        /// </summary>
        private static unsafe sccorelib.THREAD_ENTER th_enter = new sccorelib.THREAD_ENTER(orange_thread_enter);
        /// <summary>
        /// </summary>
        private static unsafe sccorelib.THREAD_LEAVE th_leave = new sccorelib.THREAD_LEAVE(orange_thread_leave);
        /// <summary>
        /// </summary>
        private static unsafe sccorelib.THREAD_START th_start = new sccorelib.THREAD_START(orange_thread_start);
        /// <summary>
        /// </summary>
        private static unsafe sccorelib.THREAD_RESET th_reset = new sccorelib.THREAD_RESET(orange_thread_reset);
        /// <summary>
        /// </summary>
        private static unsafe sccorelib.THREAD_YIELD th_yield = new sccorelib.THREAD_YIELD(orange_thread_yield);
        /// <summary>
        /// </summary>
        private static unsafe sccorelib.VPROC_IDLE vp_idle = new sccorelib.VPROC_IDLE(orange_vproc_idle);

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
            setup.vp_idle = (void*)Marshal.GetFunctionPointerForDelegate(vp_idle);
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
