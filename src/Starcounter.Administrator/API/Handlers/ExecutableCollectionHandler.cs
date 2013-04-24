
using Starcounter.Advanced;
using Starcounter.Server.Rest;

namespace Starcounter.Administrator.API.Handlers {

    /// <summary>
    /// Excapsulates the admin server functionality acting on the resource
    /// "executables hosted in a database". 
    /// </summary>
    /// <remarks>
    /// This resource is a collection of executables currently running
    /// inside a named database (under a particular server). The resource
    /// should support retreival using GET, execution of a new application
    /// using POST, stopping of an application using DELETE, and possibly
    /// patching the set of running executables using PATCH and maybe even
    /// assure a set of running executables using PUT.
    /// </remarks>
    internal static partial class ExecutableCollectionHandler {
        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Executables;
            
            Handle.POST<string, Request>(uri, OnPOST);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "POST" });
        }
    }
}