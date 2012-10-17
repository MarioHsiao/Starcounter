
using Starcounter;
using Starcounter.Internal;
using System;

namespace StarcounterInternal.Hosting
{
    
    public static class ExceptionManager
    {

        public static bool HandleUnhandledException(Exception ex)
        {
            string message = Starcounter.Logging.ExceptionFormatter.ExceptionToString(ex);

            sccoreapp.sccoreapp_log_critical_message(message);

            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine(message);
            }

            uint e;
            if (!ErrorCode.TryGetCode(ex, out e)) e = 1;
            Kernel32.ExitProcess(e);

            return true;
        }
    }
}
