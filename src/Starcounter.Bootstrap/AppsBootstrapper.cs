
using Starcounter.Internal.Application;
using Starcounter.Internal.REST;
using Starcounter.Internal.Web;
using System;
using System.Diagnostics;
namespace Starcounter.Internal {

    /// <summary>
    /// Wires together the components needed to make Starcounter Apps work.
    /// </summary>
    /// <remarks>
    /// This is the composition root (bootstrapper) of the Starcounter Apps subsystem.
    /// </remarks>
    public static class AppsBootstrapper {
        /// <summary>
        /// Wires together the internal components of Starcounter Apps.
        /// </summary>
        public static void Bootstrap() {
            Console.WriteLine("Bootstrapping Apps...");

            var i = new Injector();

            i.Register<StaticWebServer, StaticWebServer>();
            i.Register<SessionDictionary, SessionDictionary>();
            i.Register<HttpAppServer>(injector => new HttpAppServer(
                injector.Resolve<StaticWebServer>(),
                injector.Resolve<SessionDictionary>()
                ));
        }
    }
}
