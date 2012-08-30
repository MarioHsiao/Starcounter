
namespace Starcounter.Internal
{
    
    internal static class Process
    {

        internal static unsafe void RunMessageLoop(void *hsched)
        {
            for (; ; )
            {
                sccorelib.CM2_TASK_DATA task_data;
                uint e = sccorelib.cm2_standby(hsched, &task_data);
                if (e == 0)
                {
                    switch (task_data.Type)
                    {
                    case sccorelib.CM2_TYPE_RELEASE:
                        // The application host is shutting down and releasing the
                        // primary threads. Just exit the thread procedure shutting
                        // down the thread.

                        return;

                    case sccorelib.CM2_TYPE_CLOCK:
                        break;

                    case sccorelib.CM2_TYPE_REQUEST:
                        break;
                    };
                }
                else
                {
                    throw sccoreerr.TranslateErrorCode(e);
                }
            }
        }
    }
}
