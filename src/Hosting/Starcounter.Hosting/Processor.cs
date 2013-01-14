// ***********************************************************************
// <copyright file="Processor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Hosting;
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;

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
            new CaptureGC();
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

        static void RunTask(IntPtr hTask) {
            var gcHandle = (GCHandle)hTask;
            var task = (ITask)gcHandle.Target;
            gcHandle.Free();
            task.Run();
        }
    }
}
