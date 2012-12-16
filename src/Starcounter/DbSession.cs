
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
        Exception exception_;

        /// <summary>
        /// </summary>
        internal unsafe Task(Action action, void* hEvent) {
            action_ = action;
            hEvent_ = hEvent;
        }

        /// <summary>
        /// </summary>
        public void Run() {
            try {
                action_();
            }
            catch (Exception e) {
                exception_ = e;
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

        internal Exception GetException() { return exception_; }
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

                byte schedulerNumber;
                r = sccorelib.cm3_get_cpun(null, &schedulerNumber);
                if (r != 0) {
                    void* hEvent;
                    r = sccorelib.cm3_mevt_new(null, 0, &hEvent);
                    if (r == 0) {
                        try {
                            var task = new Task(action, hEvent);

                            TaskScheduler.Run(task);

                            r = sccorelib.cm3_mevt_wait(hEvent, UInt32.MaxValue, 0);
                            if (r != 0) throw ErrorCode.ToException(r);

                            var e = task.GetException();
                            if (e != null) {
                                var dbe = (e as DbException);
                                if (dbe != null) {
                                    throw ErrorCode.ToException(dbe.ErrorCode, dbe);
                                }
                                else {
                                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, e);
                                }
                            }
                        }
                        finally {
                            r = sccorelib.cm3_mevt_rel(hEvent);
                            if (r != 0) ExceptionManager.HandleInternalFatalError(r);
                        }
                    }
                    else {
                        throw ErrorCode.ToException(r);
                    }
                }
                else {
                    action();
                }
            }
        }
    }
}
