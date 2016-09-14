
using System;
using System.Threading;

namespace Starcounter.Internal {

    /// <summary>
    /// Provides a set of utility methods for hosted threads, i.e.
    /// threads that execute in the Starcounter code host process.
    /// </summary>
    /// <remarks>
    /// The methods of this class are primary intended for the Starcounter
    /// tools and the runtime host.
    /// </remarks>
    public static class HostedThread {

        /// <summary>
        /// Sets the thread priority of <paramref name="self"/> to the
        /// given <paramref name="value"/>.
        /// </summary>
        /// <param name="self">The thread whose priority to set.</param>
        /// <param name="value">The new priority.</param>
        /// <remarks>
        /// The Starcounter runtime will investigate all hosted code and
        /// replace all assignments to <see cref="System.Thread.Priority"/>
        /// with a call to this method.
        /// </remarks>
        public static void SetPriority(Thread self, ThreadPriority value) {
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
        /// <param name="millisecondsTimeout">The milliseconds timeout to sleep.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">millisecondsTimeout</exception>
        /// <remarks>
        /// The Starcounter runtime will investigate all hosted code and
        /// replace all calls to <see cref="System.Thread.Sleep(int)"/>
        /// with a call to this method.
        /// </remarks>
        public static void Sleep(Int32 millisecondsTimeout) {
            if (millisecondsTimeout < -1) {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            InternalSleep(millisecondsTimeout);
        }

        /// <summary>
        /// Sleeps the specified timeout.
        /// </summary>
        /// <param name="timeout">The timeout to sleep</param>
        /// <exception cref="System.ArgumentOutOfRangeException">timeout</exception>
        /// <remarks>
        /// The Starcounter runtime will investigate all hosted code and
        /// replace all calls to <see cref="System.Thread.Sleep(TimeSpan)"/>
        /// with a call to this method.
        /// </remarks>
        public static void Sleep(TimeSpan timeout) {
            var d = timeout.TotalMilliseconds;
            if (d > Int32.MaxValue) {
                throw new ArgumentOutOfRangeException("timeout");
            }

            var i = (Int32)d;
            if (i < -1) {
                throw new ArgumentOutOfRangeException("timeout");
            }

            InternalSleep(i);
        }

        private static void InternalSleep(Int32 millisecondsTimeout) {

            // Disabled internal sleep.
            /*
            var ec = sccorelib.cm3_sleep((IntPtr)0, (UInt32)millisecondsTimeout);
            if (ec == 0) {
                return;
            }
            if (ec != Error.SCERRNOTAWORKERTHREAD && ec != Error.SCERRTHREADNOTATTACHED) {
                throw ErrorCode.ToException(ec);
            }
            */
            // It's a detached thread. Just invoke the original
            // .NET CLR thread sleeping method.

            StarcounterEnvironment.RunDetached(() => {
                Thread.Sleep(millisecondsTimeout);
            });            
        }
    }
}
