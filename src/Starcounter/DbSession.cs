
using Starcounter.Internal;
using System;

namespace Starcounter.Internal {

    /// <summary>
    /// </summary>
    public interface ITask { // Internal

        /// <summary>
        /// </summary>
        void Run();
    }

    /// <summary>
    /// </summary>
    public interface ITaskScheduler { // Internal

        /// <summary>
        /// </summary>
        void Run(ITask task);
    }

    /// <summary>
    /// </summary>
    public static class TaskScheduler { // Internal

        static ITaskScheduler impl_;

        /// <summary>
        /// </summary>
        public static void SetImplementation(ITaskScheduler impl) { // Internal
            impl_ = impl;
        }

        internal static void Run(ITask task) {
            impl_.Run(task);
        }
    }
}

namespace Starcounter {

    /// <summary>
    /// </summary>
    internal class Task : ITask {

        Action action_;
        unsafe void* hEvent_;

        /// <summary>
        /// </summary>
        public unsafe Task(Action action, void* hEvent) {
            action_ = action;
            hEvent_ = hEvent;
        }

        /// <summary>
        /// </summary>
        public void Run() {
            try {
                action_();
            }
            finally {
                unsafe {
                    if (hEvent_ != null) {
                        var r = sccorelib.cm3_mevt_set(hEvent_);
                        if (r != 0) ExceptionManager.HandleInternalFatalError(r);
                    }
                }
            }
        }

    }

    /// <summary>
    /// </summary>
    public class DbSession {

        /// <summary>
        /// </summary>
        public void RunAsync(Action action) {
            unsafe {
                TaskScheduler.Run(new Task(action, null));
            }
        }

        /// <summary>
        /// </summary>
        public void RunSync(Action action) {
            unsafe {
                uint r;
                void* hEvent;
                r = sccorelib.cm3_mevt_new(null, 0, &hEvent);
                if (r == 0) {
                    try {
                        TaskScheduler.Run(new Task(action, hEvent));
                        r = sccorelib.cm3_mevt_wait(hEvent, UInt32.MaxValue, 0);
                        if (r != 0) throw ErrorCode.ToException(r);
                    }
                    finally {
                        r = sccorelib.cm3_mevt_rel(hEvent);
                        if (r != 0) ExceptionManager.HandleInternalFatalError(r);
                    }
                }
            }
        }
    }
}
