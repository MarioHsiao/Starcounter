using Starcounter.Hosting;
using System;

namespace Starcounter.Bootstrap.RuntimeHosts.SelfHosted
{
    class ExceptionManager : IExceptionManager
    {
        public bool HandleUnhandledException(Exception ex)
        {
            return false;
        }
    }
}
