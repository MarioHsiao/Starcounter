
using Starcounter.Bootstrap.Management.Representations.JSON;
using Starcounter.Hosting;
using System;
using System.Diagnostics;

namespace Starcounter.Bootstrap.RuntimeHosts.SelfHosted
{
    public class SelfHostingRuntimeHost : RuntimeHost
    {
        /// <summary>
        /// Callback invoked by the self-hosting host when ready for service.
        /// </summary>
        /// <remarks>
        /// The self-hosting host will invoke this callback and block the
        /// bootstrapping thread on it. When the application main loop return,
        /// the host will shut down and dispose.
        /// </remarks>
        internal Action ApplicationMainLoop { get; set; }

        protected override ILifetimeService CreateLifetimeService()
        {
            return new LifetimeService(this);
        }

        protected override IExceptionManager CreateExceptionManager()
        {
            return new ExceptionManager();
        }

        protected override IAssemblyResolver CreateAssemblyResolver()
        {
            return new AssemblyResolver();
        }

        protected override void RunLifetimeService(ILifetimeService lifetimeService)
        {
            var hostJson = new CodeHost();
            hostJson.DatabaseName = configuration.Name;
            hostJson.HostedApplicationName = Process.GetCurrentProcess().ProcessName;
            hostJson.ProcessId = Process.GetCurrentProcess().Id;

            var uri = $"http://localhost:{configuration.DefaultSystemHttpPort}/api/engines/{configuration.Name}/host";
            var response = Http.PUT(uri, hostJson.ToJson(), null);

            // Invoke base implementation, blocking on the service
            base.RunLifetimeService(lifetimeService);
        }
    }
}
