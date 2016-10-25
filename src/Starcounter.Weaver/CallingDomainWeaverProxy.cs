
using System;

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

            weaver.Setup(weaverSetup, weaverHostType.FullName, weaverHostType.Assembly.FullName);

            setup = weaverSetup;
            remoteDomain = remoteWeaverDomain;
            remoteWeaver = weaver;
        }

        void IWeaver.Execute()
        {
            try
            {
                remoteWeaver.Execute();
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