
using System;
using System.Diagnostics;
using System.Reflection;

namespace Starcounter.Weaver
{
    /// <summary>
    /// Internal weaver proxy, living in the same domain as that of the
    /// initiator of weaver setup. When executed, marshal the call into
    /// the remote domain via <see cref="RemoteDomainWeaver"/>.
    /// </summary>
    internal class CallingDomainWeaverProxy : IWeaver
    {
        readonly AppDomain remoteDomain;
        readonly WeaverSetup setup;
        readonly bool ownRemoteDomainLifetime;

        IWeaver remoteWeaver;
        
        WeaverSetup IWeaver.Setup {
            get {
                return setup;
            }
        }

        public CallingDomainWeaverProxy(WeaverSetup weaverSetup, AppDomain remoteWeaverDomain, bool unloadDomainWhenExecuted)
        {
            setup = weaverSetup;
            remoteDomain = remoteWeaverDomain;
            ownRemoteDomainLifetime = unloadDomainWhenExecuted;
        }

        public void SetupRemoteDomainWeaver(Type weaverHostType)
        {
            if (ownRemoteDomainLifetime)
            {
                // If we own the lifetime of the remote domain, we need
                // to provide some help to load assemblies for certain cases,
                // which seem to be a bug in remoting bits, where this "hack"
                // seem to resolve it.
                // Background here: http://stackoverflow.com/a/1438637/888042

                AppDomain.CurrentDomain.AssemblyResolve += TryResolveAssembly;
            }

            var weaverHandle = remoteDomain.CreateInstance(typeof(RemoteDomainWeaver).Assembly.FullName, typeof(RemoteDomainWeaver).FullName);
            var weaverRef = weaverHandle.Unwrap();

            var weaver = (RemoteDomainWeaver)weaverRef;

            var setupResult = weaver.Setup(setup, weaverHostType.FullName, weaverHostType.Assembly.FullName);
            if (setupResult != 0)
            {
                if (setupResult == RemoteDomainWeaver.ErrorLoadingHostAssembly)
                {
                    throw new ArgumentException($"Unable to find/load assembly of {weaverHostType} in remote domain.", nameof(weaverHostType));
                }
                else
                {
                    Trace.Assert(setupResult == RemoteDomainWeaver.ErrorCreatingHostType);
                    throw new ArgumentException($"Unable to instantiate {weaverHostType} in remote domain.", nameof(weaverHostType));
                }
            }

            remoteWeaver = weaver;
        }
        
        static Assembly TryResolveAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                var assembly = Assembly.Load(args.Name);
                if (assembly != null)
                    return assembly;
            }
            catch { }
            return null;
        }

        bool IWeaver.Execute()
        {
            try
            {
                return remoteWeaver.Execute();
            }
            finally
            {
                if (ownRemoteDomainLifetime)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= TryResolveAssembly;
                    AppDomain.Unload(remoteDomain);
                }
            }
        }
    }
}