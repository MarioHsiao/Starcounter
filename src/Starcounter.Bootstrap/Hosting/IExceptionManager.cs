
using System;

namespace Starcounter.Hosting
{
    public interface IExceptionManager
    {
        bool HandleUnhandledException(Exception ex);
    }
}
