
using Starcounter.Apps.Bootstrap;

namespace Starcounter {

    /// <summary>
    /// The base Apps class representing a logical App.
    /// </summary>
    public class AppNew
    {
        static AppNew()
        {
            AppProcess.AssertInDatabaseOrSendStartRequest();
        }
    }
}