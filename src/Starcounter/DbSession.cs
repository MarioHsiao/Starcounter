
using Starcounter.Internal;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Starcounter.Internal {

    /// <summary>
    /// Interface representing a task. Used by the in process task scheduler to
    /// run the task.
    /// </summary>
    public interface ITask { // Internal

        /// <summary>
        /// User action.
        /// </summary>
        Action UserAction
        {
            get;
        }

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
        UInt32 Run(ITask task, Byte schedId = StarcounterEnvironment.InvalidSchedulerId);
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

        internal static UInt32 Run(ITask task, Byte schedId = StarcounterEnvironment.InvalidSchedulerId) {
            return impl_.Run(task, schedId);
        }
    }
}

namespace Starcounter {

    /// <summary>
    /// Class representing a task.
    /// </summary>
    internal class Task : ITask {

        public static void AsyncTaskAction() {
            // Do nothing.
        }

        Action action_;
        unsafe void* hEvent_;
        Exception exception_;

        /// <summary>
        /// User action.
        /// </summary>
        public Action UserAction {
            get
            {
                return action_;
            }
        }

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
                        if (r != 0)
                            ExceptionManager.HandleInternalFatalError(r);
                    }
                }
            }
        }

        public Exception GetException() {
            return exception_;
        }
    }

    /// <summary>
    /// Saved structure containing user task.
    /// </summary>
    class UserTaskInfo {
        public String AppName;
        public Action UserTask;
    }

    /// <summary>
    /// </summary>
    [Obsolete("Please use Scheduling.ScheduleTask instead.")]
    public class DbSession : IDbSession {

        /// <summary>
        /// List of user tasks scheduled on any scheduler.
        /// </summary>
        static ConcurrentQueue<UserTaskInfo> asyncTasksAnyScheduler_ = new ConcurrentQueue<UserTaskInfo>();

        /// <summary>
        /// List of user tasks per scheduler.
        /// </summary>
        static ConcurrentQueue<UserTaskInfo>[] asyncTasksPerScheduler_;

        /// <summary>
        /// Current round robin scheduler id value.
        /// </summary>
        [ThreadStatic]
        static Byte roundRobinSchedId_ = 0;

        /// <summary>
        /// Initializes schdeduling data.
        /// </summary>
        public static void Init() {
            asyncTasksPerScheduler_ = new ConcurrentQueue<UserTaskInfo>[StarcounterEnvironment.SchedulerCount];

            for (Int32 i = 0; i < asyncTasksPerScheduler_.Length; i++) {
                asyncTasksPerScheduler_[i] = new ConcurrentQueue<UserTaskInfo>();
            }
        }

        /// <summary>
        /// Gets and executes already scheduled tasks on current scheduler.
        /// </summary>
        public static void GetAndExecuteQueuedTasks(Action<Action, String> taskExecutionMethod) {

            Byte curSchedId = StarcounterEnvironment.CurrentSchedulerId;
            UserTaskInfo task;

            // First we need to execute all tasks waiting on the same scheduler.
            while (true) {

                // Trying to dequeue.
                if (!asyncTasksPerScheduler_[curSchedId].TryDequeue(out task))
                    break;

                // Running the given task execution.
                taskExecutionMethod(task.UserTask, task.AppName);
            }

            // Now taking the task from all schedulers queue.
            while (true) {

                // Trying to dequeue.
                if (!asyncTasksAnyScheduler_.TryDequeue(out task))
                    break;

                // Running the given task execution.
                taskExecutionMethod(task.UserTask, task.AppName);
           }
        }

        /// <summary>
        /// Runs the task represented by the action delegate asynchronously.
        /// </summary>
        /// <remarks>
        /// Schedules a task on a given scheduler (even if current scheduler is the same).
        /// In case if invalid scheduler is supplied then all schedulers are tried in round robin
        /// manner. When all schedulers are tried but the queues are full - then task is put into
        /// the awaiting queue.
        /// </remarks>
        public void RunAsync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId) {

            unsafe
            {
                Boolean anyScheduler = (StarcounterEnvironment.InvalidSchedulerId == schedId);

                String curAppName = StarcounterEnvironment.AppName;

                // Creating the task to run.
                var task = new Task(Task.AsyncTaskAction, null);

                // Checking if we need to use round robin for getting scheduler id.
                if (anyScheduler) {

                    // First checking if we have any tasks in the common queue.
                    bool isEmpty = asyncTasksAnyScheduler_.IsEmpty;

                    // Adding to the queue now.
                    asyncTasksAnyScheduler_.Enqueue(new UserTaskInfo() {
                        AppName = curAppName,
                        UserTask = action
                    });

                    if (isEmpty) {

                        // Waking up all schedulers.
                        for (Byte i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {
                            TaskScheduler.Run(task, i);
                        }
                    }

                } else {

                    // First checking if we have any tasks in the common queue.
                    Boolean isEmpty = asyncTasksPerScheduler_[schedId].IsEmpty;

                    // Enqueing the task.
                    asyncTasksPerScheduler_[schedId].Enqueue(new UserTaskInfo() {
                        AppName = curAppName,
                        UserTask = action
                    });

                    if (isEmpty) {
                        TaskScheduler.Run(task, schedId);
                    }
                }
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
        public void RunSync(Action action, Byte schedId = StarcounterEnvironment.InvalidSchedulerId) {
            unsafe {
                uint r;

                // Checking if we need to use round robin for getting scheduler id.
                if (StarcounterEnvironment.InvalidSchedulerId == schedId) {

                    schedId = ++roundRobinSchedId_;

                    if (schedId >= StarcounterEnvironment.SchedulerCount) {
                        roundRobinSchedId_ = 0;
                        schedId = 0;
                    }
                }

                byte schedulerNumber;
                r = sccorelib.cm3_get_cpun(null, &schedulerNumber);

                if ((r != 0) ||
                    ((schedId != StarcounterEnvironment.InvalidSchedulerId) && (schedulerNumber != schedId))) {

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

                            while (true) {
                                
                                // Running the task and getting the result.
                                UInt32 errCode = TaskScheduler.Run(task, schedId);

                                // If success - stop trying.
                                if (0 == errCode) {
                                    break;
                                }

                                // If queue is full, sleeping a bit and then trying again.
                                if (Error.SCERRINPUTQUEUEFULL == errCode) {
                                    Thread.Sleep(1);
                                } else {
                                    // If different type of error - throwing it.
                                    throw ErrorCode.ToException(errCode);
                                }
                            }

                            r = sccorelib.cm3_mevt_wait(hEvent, UInt32.MaxValue, 0);
                            if (r != 0)
                                throw ErrorCode.ToException(r);

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
