﻿
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

        [ThreadStatic]
        public static ThreadData Current;

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

        public readonly Scheduler Scheduler;

        private readonly unsafe UInt32* _pStateShare;

        public unsafe ThreadData(Byte schedulerNumber, UInt32* pStateShare)
        {
            Scheduler = Scheduler.GetInstance(schedulerNumber);
            _pStateShare = pStateShare;
        }
    }
}
