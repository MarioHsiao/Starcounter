
using System;
using System.Diagnostics;

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
        readonly IWeaver remoteWeaver;

        public bool UnloadDomainWhenExecuted { get; set; }

        WeaverSetup IWeaver.Setup {
            get {
                return setup;
            }
        }

        public CallingDomainWeaverProxy(WeaverSetup weaverSetup, AppDomain remoteWeaverDomain, Type weaverHostType)
        {
            var weaver = (RemoteDomainWeaver)remoteWeaverDomain.CreateInstanceAndUnwrap(
                typeof(RemoteDomainWeaver).Assembly.FullName,
                typeof(RemoteDomainWeaver).FullName);

            var setupResult = weaver.Setup(weaverSetup, weaverHostType.FullName, weaverHostType.Assembly.FullName);
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

            setup = weaverSetup;
            remoteDomain = remoteWeaverDomain;
            remoteWeaver = weaver;
        }

        bool IWeaver.Execute()
        {
            try
            {
                return remoteWeaver.Execute();
            }
            finally
            {
                if (UnloadDomainWhenExecuted)
                {
                    AppDomain.Unload(remoteDomain);
                }
            }
        }
    }
}