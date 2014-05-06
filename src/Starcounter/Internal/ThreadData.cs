// ***********************************************************************
// <copyright file="ThreadData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

#define TRY_KEEP_DOWN_ENUM_NODE_COUNT

using Starcounter.Internal;
using System;

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
        /// Holds the pointer to any implicit transaction created during the current
        /// task.
        /// </summary>
        internal ulong _handle;
        internal ulong _verify;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadData" /> class.
        /// </summary>
        /// <param name="schedulerNumber">The scheduler number.</param>
        /// <param name="pStateShare">The p state share.</param>
        public unsafe ThreadData(Byte schedulerNumber, UInt32* pStateShare)
        {
            Scheduler = Scheduler.GetInstance(schedulerNumber);
            _pStateShare = pStateShare;
            _handle = 0;
            _verify = 0;
        }
    }
}
