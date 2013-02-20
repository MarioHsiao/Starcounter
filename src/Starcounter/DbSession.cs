
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Threading;

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
        static IntPtr sched_;

        /// <summary>
        /// </summary>
        public static unsafe void SetImplementation(ITaskScheduler impl, void* hsched) { // Internal
            impl_ = impl;
            sched_ = (IntPtr)hsched;
        }

        internal static void Run(ITask task) {
            impl_.Run(task);
        }

        private sealed class JobState
        {
            internal readonly WaitCallback Callback;
            internal readonly Object State;

            internal JobState(WaitCallback callback, Object state)
                : base()
            {
                Callback = callback;
                State = state;
            }
        }

        internal static void ScheduleJob(WaitCallback callback, Object state, Byte cpun, UInt16 prio)
        {
            JobState jobState;
            GCHandle gch;
            UInt32 ec;

            jobState = new JobState(callback, state);
            try
            {
                gch = GCHandle.Alloc(jobState);
                try
                {
                    unsafe
                    {
                        ec = sccorelib.cm2_schedule(
                            (void *)sched_,
                            cpun,
                            sccorelib_ext.SC_JOBT_EXECUTE_UJOB,
                            prio,
                            0,
                            (UInt64)GCHandle.ToIntPtr(gch),
                            0);
                    }

                    if (ec == 0)
                    {
                        return;
                    }
                    throw ErrorCode.ToException(ec);
                }
                catch
                {
                    gch.Free();
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }

        internal static void ProcessJob(UInt64 handle)
        {
            GCHandle gch;
            JobState jobState;

            gch = GCHandle.FromIntPtr((IntPtr)handle);
            jobState = (JobState)gch.Target;
            gch.Free();
            jobState.Callback(jobState.State);
        }

        public static Boolean QueueUserWorkItem(WaitCallback callBack)
        {
            TaskScheduler.ScheduleJob(callBack, null, 0xFF, 0);
            return true;
        }

        public static Boolean QueueUserWorkItem(WaitCallback callBack, Byte cpun)
        {
            TaskScheduler.ScheduleJob(callBack, null, cpun, 0);
            return true;
        }

        public static Boolean QueueUserWorkItem(WaitCallback callBack, Object state)
        {
            TaskScheduler.ScheduleJob(callBack, state, 0xFF, 0);
            return true;
        }

        public static Boolean QueueUserWorkItem(WaitCallback callBack, Object state, Byte cpun)
        {
            TaskScheduler.ScheduleJob(callBack, state, cpun, 0);
            return true;
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
