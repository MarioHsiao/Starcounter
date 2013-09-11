// ***********************************************************************
// <copyright file="InterceptThread.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Threading;

namespace Starcounter.Internal.Weaver {

    /// <summary>
    /// Class InterceptThread
    /// </summary>
    /// <remarks>
    /// If we reintroduce weaving of Systtem.Thread calls in user code, we
    /// must move this class to the Starcounter assembly. For more information,
    /// see the remarks in the ScEnhanceThreadingTask class summary.
    /// </remarks>
    internal static class InterceptThread {

        /// <summary>
        /// Set_s the priority.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="value">The value.</param>
        public static void set_Priority(Thread self, ThreadPriority value) {
            // We don't allow setting priority on threads. Any attempt to do so
            // we simply ignore.
            //
            // The reason why we don't allow setting priority is because it has
            // a tendency to interfere with spin-locks, which is used in
            // abundence in the kernel.
            //
            // Note that non-worker threads also can access kernel objects
            // protected by spin-locks (like channels and memory pools) so we
            // can't allow setting priority on them either.
        }

        /// <summary>
        /// Sleeps the specified milliseconds timeout.
        /// </summary>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">millisecondsTimeout</exception>
        public static void Sleep(Int32 millisecondsTimeout) {
            if (millisecondsTimeout < -1) {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }
            InternalSleep(millisecondsTimeout);
        }

        /// <summary>
        /// Sleeps the specified timeout.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">timeout</exception>
        public static void Sleep(TimeSpan timeout) {
            Double d;
            Int32 i;
            d = timeout.TotalMilliseconds;
            if (d > Int32.MaxValue) {
                throw new ArgumentOutOfRangeException("timeout");
            }
            i = (Int32)d;
            if (i < -1) {
                throw new ArgumentOutOfRangeException("timeout");
            }
            InternalSleep(i);
        }

        /// <summary>
        /// Internals the sleep.
        /// </summary>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        private static void InternalSleep(Int32 millisecondsTimeout) {
            UInt32 ec;

            // There is no cm3_sleep present. What to do?
            // TODO:

            // ec = sccorelib.cm3_sleep((IntPtr)0, millisecondsTimeout);
            ec = Error.SCERRUNSPECIFIED;
            if (ec == 0) {
                return;
            }
            if (ec == Error.SCERRNOTAWORKERTHREAD) {
                goto sleep_dt;
            }
            if (ec == Error.SCERRTHREADNOTATTACHED) {
                goto sleep_dt;
            }
            throw ErrorCode.ToException(ec);
        sleep_dt:
            Thread.Sleep(millisecondsTimeout);
        }
    }
}
