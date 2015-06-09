﻿
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;

namespace Starcounter.Hosting {

    internal class TaskSchedulerImpl : ITaskScheduler {

        unsafe void* hsched_;

        internal unsafe TaskSchedulerImpl(void* hsched) {
            hsched_ = hsched;
        }

        public static int cnt = 0;
        public static string myLock = "123";

        /// <summary>
        /// </summary>
        public void Run(ITask task, Byte schedId = Byte.MaxValue) {
            unsafe {
                IntPtr hTask = (IntPtr)GCHandle.Alloc(task, GCHandleType.Normal);

                try {

                    lock (myLock) {
                        cnt++;
                    }

                    var e = sccorelib.cm2_schedule(
                        hsched_,
                        schedId,
                        sccorelib_ext.TYPE_RUN_TASK,
                        0,
                        0,
                        0,
                        (ulong)hTask
                        );
                    if (e != 0) throw ErrorCode.ToException(e);
                }
                catch {
                    ((GCHandle)hTask).Free();
                    throw;
                }
            }
        }
    }
}
