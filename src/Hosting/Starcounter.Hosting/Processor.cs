﻿// ***********************************************************************
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

        private static unsafe void StartMessageLoop() {
            byte schedulerNumber = sccorelib.GetCpuNumber();
            // TODO EOH: Prefix needed because iterator verify 0 is treated as invalid.
            ThreadData.objectVerify_ = (1U << 8) | schedulerNumber;
            ThreadData.Current = new ThreadData(schedulerNumber, sccorelib.GetStateShare());
        }

        /// <summary>
        /// Runs the message loop.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        public static unsafe void RunMessageLoop(void *hsched)
        {
            try
            {
                StartMessageLoop();

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

                                lock (TaskSchedulerImpl.myLock) {
                                    TaskSchedulerImpl.cnt--;
                                }

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

                // No need to keep track of this since it will be cleaned up anyways in the end.
                if (Db.Environment.HasDatabase)
                    TransactionManager.CreateImplicitAndSetCurrent(true);

                task.Run();

                // Checking if any exception was thrown.
                var e = task.GetException();
                if (e != null) {
                    throw e;
                }
            } finally {
                TransactionManager.Cleanup();
            }
        }
    }
}
