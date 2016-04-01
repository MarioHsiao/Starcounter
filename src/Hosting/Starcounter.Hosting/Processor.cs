// ***********************************************************************
// <copyright file="Processor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using Starcounter;
using Starcounter.Hosting;
using Starcounter.Internal;

namespace StarcounterInternal.Hosting
{

    /// <summary>
    /// Class Processor
    /// </summary>
    public static class Processor
    {

        /// <summary>
        /// </summary>
        public unsafe static void Setup(void* hsched) {
            TaskScheduler.SetImplementation(new TaskSchedulerImpl(hsched));
            ScrapHeap.Setup(hsched);
        }

        /// <summary>
        /// Runs the message loop.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        public static unsafe void RunMessageLoop(void *hsched)
        {
            try
            {
                ThreadData.Current = new ThreadData(sccorelib.GetCpuNumber(), sccorelib.GetStateShare());

                for (; ; )
                {
                    sccorelib.CM2_TASK_DATA task_data;
                    uint e = sccoreapp.sccoreapp_standby(hsched, &task_data);
                    if (e == 0)
                    {
                        switch (task_data.Type)
                        {
                            case sccorelib.CM2_TYPE_RELEASE:
                                // The application host is shutting down and releasing the
                                // primary threads. Just exit the thread procedure shutting
                                // down the thread.

                                return;

                            case sccorelib.CM2_TYPE_REQUEST:
                                break;

                            case sccorelib_ext.TYPE_RECYCLE_SCRAP:
                                ScrapHeap.RecycleScrap();
                                break;

                            case sccorelib_ext.TYPE_RUN_TASK:
                                RunTask((IntPtr)task_data.Output3);
                                break;

                            case sccorelib_ext.TYPE_PROCESS_PACKAGE:
                                Package.Process((IntPtr)task_data.Output3);
                                break;
                        };

                        TaskHelper.Reset();
                    }
                    else
                    {
                        throw ErrorCode.ToException(e);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }

        static void RunTaskNoScheduling(Action userAction, String appName) {

            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe
            {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

            try {
                // No need to keep track of this since it will be cleaned up anyways in the end.
                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                // Setting current application name.
                StarcounterEnvironment.AppName = appName;

                Task task;

                unsafe
                {
                    // Creating a task containing user action.
                    task = new Task(userAction, null);

                    // Executing the task and getting the exception, if any.
                    task.Run();
                }

                // Checking if any exception was thrown.
                var e = task.GetException();
                if (e != null) {
                    throw ErrorCode.ToException(Starcounter.Error.SCERRUNHANDLEDEXCEPTION, e);
                }
            } finally {
                TransactionManager.Cleanup();
            }
        }

        static void RunTask(IntPtr hTask) {

            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

            try {
                var gcHandle = (GCHandle)hTask;
                var task = (ITask)gcHandle.Target;
                gcHandle.Free();

                // Checking if its async action.
                if (Task.AsyncTaskAction == task.UserAction) {

                    // Immediately cleaning the transaction.
                    TransactionManager.Cleanup();

                    // Getting and running tasks from scheduler queue and common queue.
#pragma warning disable 0618
                    DbSession.GetAndExecuteQueuedTasks(RunTaskNoScheduling);
#pragma warning restore 0618

                    return;
                }

                // No need to keep track of this since it will be cleaned up anyways in the end.
                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                task.Run();

                // Checking if any exception was thrown.
                var e = task.GetException();
                if (e != null) {
                    throw ErrorCode.ToException(Starcounter.Error.SCERRUNHANDLEDEXCEPTION, e);
                }
            } finally {
                TransactionManager.Cleanup();
            }
        }
    }
}
