
using Starcounter;
using Starcounter.Internal;
using System;

namespace StarcounterInternal.Hosting
{
    
    public static class Processor
    {

        public static unsafe void RunMessageLoop(void *hsched)
        {
            try
            {
                ThreadData.Current = new ThreadData(sccorelib.GetCpuNumber(), sccorelib.GetStateShare());

                for (; ; )
                {
                    ThreadData.Current.Scheduler.SqlEnumCache.InvalidateCache();
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

                            case sccorelib_ext.TYPE_PROCESS_PACKAGE:
                                Package.Process((IntPtr)task_data.Output3);
                                break;
                        };
                    }
                    else
                    {
                        throw ErrorCode.ToException(e);
                    }
                }
            }
            catch (Exception ex)
            {
                sccoreapp.sccoreapp_log_critical(ex.ToString());
                
                uint e;
                if (!ErrorCode.TryGetCode(ex, out e)) e = 1;
                Kernel32.ExitProcess(e);
            }
        }
    }
}
