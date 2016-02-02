
using Starcounter.Internal;
using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Interface representing a task. Used by the in process task scheduler to
    /// run the task.
    /// </summary>
    public interface ITask { // Internal

        /// <summary>
        /// Called to execute the task.
        /// </summary>
        void Run();

        /// <summary>
        /// Getting exception.
        /// </summary>
        /// <returns></returns>
        Exception GetException();
    }

    /// <summary>
    /// </summary>
    public interface ITaskScheduler { // Internal

        /// <summary>
        /// </summary>
        void Run(ITask task, Byte schedId = Byte.MaxValue);
    }

    /// <summary>
    /// </summary>
    public static class TaskScheduler { // Internal
        static ITaskScheduler impl_;

        /// <summary>
        /// </summary>
        public static unsafe void SetImplementation(ITaskScheduler impl) { // Internal
            impl_ = impl;
        }

        internal static void Run(ITask task, Byte schedId = Byte.MaxValue) {
            impl_.Run(task, schedId);
        }
    }
}

namespace Starcounter {

    /// <summary>
    /// Class representing a task.
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
        /// ITask interface implementation. Calls the action delegate
        /// associated with the task, intercepts any transaction and stores it
        /// with the task object.
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

        public Exception GetException() {
            return exception_;
        }
    }

    /// <summary>
    /// </summary>
    public class DbSession : IDbSession {

        /// <summary>
        /// Round robin scheduler ID for RunAsync calls.
        /// </summary>
        static Byte roundRobinSchedId_ = 0;

        /// <summary>
        /// Lock for incrementing scheduler id.
        /// </summary>
        static String roundRobinLock_ = "some value";

        /// <summary>
        /// Runs the task represented by the action delegate asynchronously.
        /// </summary>
        /// <remarks>
        /// Unless overridden by specifying a specific scheduler for the task
        /// to run on: If calling thread is attached to a Starcounter
        /// scheduler, the task runs on the same scheduler as the calling
        /// thread. If the calling thread is not a Starcounter thread, the task
        /// is scheduled on scheduler 0.
        /// </remarks>
        public void RunAsync(Action action, Byte schedId = Byte.MaxValue) {
            unsafe {
                String curAppName = StarcounterEnvironment.AppName;

                // Checking if we need to use round robin for getting scheduler id.
                if (Byte.MaxValue == schedId) {
                    lock (roundRobinLock_) {
                        roundRobinSchedId_++;
                        if (roundRobinSchedId_ >= StarcounterEnvironment.SchedulerCount) {
                            roundRobinSchedId_ = 0;
                        }
                        schedId = roundRobinSchedId_;
                    }
                }

                TaskScheduler.Run(new Task(
                () => {
                    // NOTE: Setting current application name, since StarcounterEnvironment.AppName is thread static.
                    StarcounterEnvironment.AppName = curAppName;
                    action();
                }, null), schedId);
            }
        }

        /// <summary>
        /// Runs the task represented by the action delegate synchronously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the calling thread is attached to a Starcounter scheduler, the
        /// thread simply calls the action delegate representing the task. If
        /// the calling thread is not a Starcounter thread, the task is
        /// scheduled on a Starcounter thread and the current thread blocks
        /// waiting for the task to complete.
        /// </para>
        /// <para>
        /// On unhandled exception in the action delegate representing the task,
        /// the exception is intercepted and an exception referencing that
        /// exception, as an inner exception, is thrown by RunSync.
        /// </para>
        /// </remarks>
        public void RunSync(Action action, Byte schedId = Byte.MaxValue) {
            unsafe {
                uint r;

                byte schedulerNumber;
                r = sccorelib.cm3_get_cpun(null, &schedulerNumber);
                if ((r != 0) || (schedId != Byte.MaxValue && schedulerNumber != schedId)) {
                    void* hEvent;
                    r = sccorelib.cm3_mevt_new(null, 0, &hEvent);
                    if (r == 0) {
                        try {
                            String curAppName = StarcounterEnvironment.AppName;

                            var task = new Task(
                            () => {
                                // NOTE: Setting current application name, since StarcounterEnvironment.AppName is thread static.
                                StarcounterEnvironment.AppName = curAppName;
                                action();
                            }, hEvent);

                            TaskScheduler.Run(task, schedId);

                            r = sccorelib.cm3_mevt_wait(hEvent, UInt32.MaxValue, 0);
                            if (r != 0) throw ErrorCode.ToException(r);

                            var e = task.GetException();
                            if (e != null) {
                                var dbe = (e as DbException);
                                if (dbe != null) {
                                    throw ErrorCode.ToException(dbe.ErrorCode, dbe);
                                }
                                else {
                                    throw ErrorCode.ToException(Error.SCERRUNHANDLEDEXCEPTION, e);
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
