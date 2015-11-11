// ***********************************************************************
// <copyright file="ThreadData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

#define TRY_KEEP_DOWN_ENUM_NODE_COUNT

using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace Starcounter
{
    /// <summary>
    /// This class represents the context of the thread that is
    /// scheduled for execution by a virtual processor (VPContext).
    /// Every thread holds an independent instance of this class.
    /// </summary>
    public sealed class ThreadData : Object
    {

#if TRY_KEEP_DOWN_ENUM_NODE_COUNT
        private const int _DEFAULT_ENUM_NODE_COUNT_GC_THRESHOLD = 8;
#endif

        /// <summary>
        /// The current
        /// </summary>
        [ThreadStatic]
        public static ThreadData Current;

        [ThreadStatic]
        internal static ulong contextHandle_ = 0;

        [ThreadStatic]
        internal static ulong storedTransactionHandle_ = 0;

        /// <summary>
        /// Indicates if the thread is in a transaction scope.
        /// </summary>
        /// <remarks>
        /// While transaction is in a transaction scope then application code is
        /// not allowed to switch current transaction on the thread.
        /// </remarks>
        [ThreadStatic]
        internal static int inTransactionScope_ = 0;

        [ThreadStatic]
        internal static uint objectVerify_;

        private static ulong GetContextHandleExcept() {
            // Thread not attached. There could be a number of reasons for this,
            // the thread might not be a Starcounter worker thread for example,
            // but if it the reason is that the thread has been implicitly
            // detached by block detection we should implicitly attach it again.
            //
            // So we try ending auto detach. If detached for some other reason,
            // or not a worker thread, operation will fail, in which case we
            // raise an exception. Otherwise it will wait until scheduler
            // attaches the thread again and we will have ownership of the
            // context once more.

            uint r = sccorelib.cm3_eautodet(IntPtr.Zero);
            if (r == 0) {
                Debug.Assert(contextHandle_ != 0);
                return contextHandle_;
            }
            throw ErrorCode.ToException(Error.SCERRTHREADNOTATTACHED);
        }

        internal static ulong ContextHandle {
            get {
                if (contextHandle_ != 0) return contextHandle_;
                return GetContextHandleExcept();
            }
        }

        internal static ulong ObjectVerify { get { return objectVerify_; } }

        /// <summary>
        /// Gets the current if attached and reattach if auto detached.
        /// </summary>
        /// <returns>ThreadData.</returns>
        internal static ThreadData GetCurrentIfAttachedAndReattachIfAutoDetached()
        {
            ThreadData current;
            UInt32 ec;
            current = Current;
            if (current == null)
            {
                return null;
            }
            unsafe
            {
                if (*current._pStateShare == 1)
                {
                    return current;
                }
            }
            ec = sccorelib.cm3_eautodet((IntPtr)0);
            if (ec == 0)
            {
                return current;
            }
            if (ec == Error.SCERRTHREADNOTAUTODETACHED)
            {
                // Ending auto detached failed because thread has been manually
                // detached.
                return null;
            }
            throw ErrorCode.ToException(ec);
        }

        //
        // Since an instance of this class only is tied to a single thread and
        // only can be accessed from the thread isn't tied to none of the
        // methods supported by this class need be thread-safe.
        //

        /// <summary>
        /// The scheduler
        /// </summary>
        public readonly Scheduler Scheduler;

        /// <summary>
        /// The _p state share
        /// </summary>
        private readonly unsafe UInt32* _pStateShare;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadData" /> class.
        /// </summary>
        /// <param name="schedulerNumber">The scheduler number.</param>
        /// <param name="pStateShare">The p state share.</param>
        public unsafe ThreadData(Byte schedulerNumber, UInt32* pStateShare)
        {
            Scheduler = Scheduler.GetInstance(schedulerNumber);
            _pStateShare = pStateShare;
        }
    }
}
