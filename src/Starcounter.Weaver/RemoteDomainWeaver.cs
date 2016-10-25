using System;
using System.Linq;
using System.Reflection;

namespace Starcounter.Weaver
{
    /// <summary>
    /// Internal weaver proxy living in a remote domain from that of the
    /// weaver initiator. 
    /// </summary>
    internal class RemoteDomainWeaver : MarshalByRefObject, IWeaver
    {
        IWeaver weaver;

        WeaverSetup IWeaver.Setup {
            get {
                return weaver.Setup;
            }
        }

        public void Setup(WeaverSetup weaverSetup, string weaverHostTypeName, string weaverHostTypeAssembly)
        {
            var hostAssembly = GetWeaverHostAssembly(weaverHostTypeAssembly);
            if (hostAssembly == null)
            {
                // Return specific error, allowing local domain to raise error to
                // the caller.
                // TODO:
            }

            var host = hostAssembly.CreateInstance(weaverHostTypeAssembly) as IWeaverHost;
            if (host == null)
            {
                // Return specific error, allowing local domain to raise error to
                // the caller.
                // TODO:
            }

            weaver = new CodeWeaver(weaverSetup, host);
        }

        void IWeaver.Execute()
        {
            weaver.Execute();
        }

        Assembly GetWeaverHostAssembly(string weaverHostTypeAssembly)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var hostAssembly = loadedAssemblies.FirstOrDefault((candidate) =>
            {
                return candidate.FullName.Equals(weaverHostTypeAssembly, StringComparison.InvariantCultureIgnoreCase);
            });
            if (hostAssembly == null)
            {
                hostAssembly = Assembly.Load(weaverHostTypeAssembly);
            }

            return hostAssembly;
        }
    }
}