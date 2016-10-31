using Starcounter.Internal;
using System;

namespace Starcounter.Weaver
{
    /// <summary>
    /// Expose factory methods used to instantiate weavers.
    /// </summary>
    public static class WeaverFactory
    {
        /// <summary>
        /// Creates a new weaver that will run in the calling domain.
        /// </summary>
        /// <param name="setup">Setup traits of the weaver.</param>
        /// <param name="host">Instance of a weaver host.</param>
        /// <returns>A <c>IWeaver</c> to execute.</returns>
        public static IWeaver CreateWeaver(WeaverSetup setup, IWeaverHost host)
        {
            GuardSetup(setup);
            Guard.NotNull(host, nameof(host));

            return new CodeWeaver(setup, host);
        }

        /// <summary>
        /// Creates a new weaver that will run in a remote app domain.
        /// </summary>
        /// <param name="setup">Setup traits of the weaver.</param>
        /// <param name="weaverHostType">Type that implement <c>IWeaverHost</c>.
        /// Will be instantiated by the runtime in the remote domain.</param>
        /// <param name="domain">An optional, pre-created <c>AppDomain</c>. If
        /// no domain is given, one will be created on demand.</param>
        /// <returns>A <c>IWeaver</c> to execute.</returns>
        public static IWeaver CreateWeaver(WeaverSetup setup, Type weaverHostType, AppDomain domain = null)
        {
            GuardSetup(setup);
            Guard.IsAssignableFrom(weaverHostType, typeof(IWeaverHost), nameof(weaverHostType));
            Guard.IsNotAbstract(weaverHostType, nameof(weaverHostType));
            Guard.HasPublicDefaultConstructor(weaverHostType, nameof(weaverHostType));

            var unloadDomainAfterExecution = false;
            if (domain == null)
            {
                unloadDomainAfterExecution = true;

                var current = AppDomain.CurrentDomain;

                var domainSetup = new AppDomainSetup();
                domainSetup.ApplicationBase = current.BaseDirectory;

                domain = AppDomain.CreateDomain("CodeWeaverDomain", null, info: domainSetup);
            }

            // When do we create the remote instance in the domain? Not
            // until it's executed?
            // TODO:

            var proxy = new CallingDomainWeaverProxy(setup, domain, weaverHostType);
            proxy.UnloadDomainWhenExecuted = unloadDomainAfterExecution;

            return proxy;
        }

        static void GuardSetup(WeaverSetup setup)
        {
            Guard.NotNull(setup, nameof(setup));
            Guard.DirectoryExists(setup.InputDirectory, nameof(setup.InputDirectory));
            Guard.FileExistsInDirectory(setup.AssemblyFile, setup.InputDirectory, nameof(setup.AssemblyFile));
            Guard.DirectoryExists(setup.OutputDirectory, nameof(setup.OutputDirectory));
            Guard.DirectoryExists(setup.CacheDirectory, nameof(setup.CacheDirectory));
            Guard.DirectoryExists(setup.WeaverRuntimeDirectory, nameof(setup.WeaverRuntimeDirectory));
        }
    }
}
