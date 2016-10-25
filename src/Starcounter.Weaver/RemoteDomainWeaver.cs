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

        public const int ErrorLoadingHostAssembly = 1;
        public const int ErrorCreatingHostType = 2;

        WeaverSetup IWeaver.Setup {
            get {
                return weaver.Setup;
            }
        }

        public int Setup(WeaverSetup weaverSetup, string weaverHostTypeName, string weaverHostTypeAssembly)
        {
            var hostAssembly = GetWeaverHostAssembly(weaverHostTypeAssembly);
            if (hostAssembly == null)
            {
                return ErrorLoadingHostAssembly;
            }

            var host = hostAssembly.CreateInstance(weaverHostTypeName) as IWeaverHost;
            if (host == null)
            {
                return ErrorCreatingHostType;
            }

            weaver = new CodeWeaver(weaverSetup, host);
            return 0;
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