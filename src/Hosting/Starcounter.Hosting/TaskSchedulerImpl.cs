
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;

namespace Starcounter.Hosting {

    internal class TaskSchedulerImpl : ITaskScheduler {

        unsafe void* hsched_;

        internal unsafe TaskSchedulerImpl(void* hsched) {
            hsched_ = hsched;
        }

        /// <summary>
        /// </summary>
        public UInt32 Run(ITask task, Byte schedId = StarcounterEnvironment.InvalidSchedulerId) {
            unsafe {
                IntPtr hTask = (IntPtr)GCHandle.Alloc(task, GCHandleType.Normal);

                UInt32 e = sccorelib.cm2_schedule(
                    hsched_,
                    schedId,
                    sccorelib_ext.TYPE_RUN_TASK,
                    0,
                    0,
                    0,
                    (ulong)hTask
                    );

                // If there was an error - we need to cleanup the task.
                if (0 != e) {
                    ((GCHandle)hTask).Free();
                }

                return e;
            }
        }
    }
}
