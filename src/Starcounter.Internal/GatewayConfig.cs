using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Starcounter.Internal {

    /// <summary>
    /// Network Gateway.
    /// </summary>
    public class NetworkGateway {

        static String DescriptionOrig =
    @"
    WorkersNumber: Number of worker threads (default: 2);
    MaxConnectionsPerWorker: Maximum number of connections (default: 10000);
    MaximumReceiveContentLength: Maximum receive content length size in bytes (default: 1000000);
    InactiveConnectionTimeout: Inactive connections life time in seconds (default: 1200);
    AggregationPort: Gateway traffic aggregation port (default: 9191);
    InternalSystemPort: Gateway system internal port (default: 8181);
    ReverseProxies: Information on reverse proxies if any (default: none);
    UriAliases: Information on URI aliases if any (default: none);
    ";

        public String Description = DescriptionOrig;

        public int WorkersNumber = 2;

        public int MaxConnectionsPerWorker = 10000;

        public int MaximumReceiveContentLength = 1000000;

        public int InactiveConnectionTimeout = 1200;

        public ushort AggregationPort = 9191;

        public ushort InternalSystemPort = 8181;

        public List<ReverseProxy> ReverseProxies = new List<ReverseProxy>();

        public List<UriAlias> UriAliases = new List<UriAlias>();

        public Boolean AddOrReplaceUriAlias(UriAlias newAlias) {

            newAlias.HttpMethod = newAlias.HttpMethod.ToUpperInvariant();

            for (int i = 0; i < UriAliases.Count; i++) {

                UriAlias a = UriAliases[i];

                if (a.HttpMethod == newAlias.HttpMethod &&
                    a.Port == newAlias.Port &&
                    a.FromUri.ToUpperInvariant() == newAlias.FromUri.ToUpperInvariant()) {

                    UriAliases[i] = newAlias;
                    return true;
                }
            }

            UriAliases.Add(newAlias);

            return false;
        }

        public UriAlias GetUriAlias(string httpMethod, ushort port, string fromUri) {

            for (int i = 0; i < UriAliases.Count; i++) {

                UriAlias uriAlias = UriAliases[i];

                if (uriAlias.HttpMethod == httpMethod &&
                    uriAlias.Port == port &&
                    uriAlias.FromUri.ToUpperInvariant() == fromUri.ToUpperInvariant()) {

                    return uriAlias;
                }
            }
            return null;
        }

        public Boolean RemoveUriAlias(UriAlias newAlias) {

            newAlias.HttpMethod = newAlias.HttpMethod.ToUpperInvariant();

            for (int i = 0; i < UriAliases.Count; i++) {

                UriAlias a = UriAliases[i];

                if (a.HttpMethod == newAlias.HttpMethod &&
                    a.Port == newAlias.Port &&
                    a.FromUri.ToUpperInvariant() == newAlias.FromUri.ToUpperInvariant()) {

                    UriAliases.Remove(a);
                    return true;
                }
            }

            return false;
        }

        public ReverseProxy GetReverseProxy(string matchingHost, ushort starcounterProxyPort) {

            for (int i = 0; i < ReverseProxies.Count; i++) {

                ReverseProxy r = ReverseProxies[i];

                if (r.StarcounterProxyPort == starcounterProxyPort && r.MatchingHost.ToUpperInvariant() == matchingHost.ToUpperInvariant()) {
                    return r;
                }
            }
            return null;
        }
        public Boolean AddOrReplaceReverseProxy(ReverseProxy newRevProxy) {

            for (int i = 0; i < ReverseProxies.Count; i++) {

                ReverseProxy r = ReverseProxies[i];

                if (r.StarcounterProxyPort == newRevProxy.StarcounterProxyPort &&
                    r.MatchingHost.ToUpperInvariant() == newRevProxy.MatchingHost.ToUpperInvariant()) {

                    ReverseProxies[i] = newRevProxy;
                    return true;
                }
            }

            ReverseProxies.Add(newRevProxy);

            return false;
        }

        public Boolean RemoveReverseProxy(ReverseProxy newRevProxy) {

            for (int i = 0; i < ReverseProxies.Count; i++) {

                ReverseProxy r = ReverseProxies[i];

                if (r.StarcounterProxyPort == newRevProxy.StarcounterProxyPort &&
                    r.MatchingHost.ToUpperInvariant() == newRevProxy.MatchingHost.ToUpperInvariant()) {

                    ReverseProxies.Remove(r);
                    return true;
                }
            }

            return false;
        }

        void Serialize(String gatewayXml) {

            XmlSerializer x = new XmlSerializer(typeof(NetworkGateway));

            using (TextWriter writer = new StreamWriter(gatewayXml)) {
                x.Serialize(writer, this);
            }
        }

        public static NetworkGateway Deserealize() {

            XmlSerializer mySerializer = new XmlSerializer(typeof(NetworkGateway));

            using (FileStream myFileStream = new FileStream(StarcounterEnvironment.Gateway.PathToGatewayConfig, FileMode.Open)) {

                NetworkGateway ng = (NetworkGateway)mySerializer.Deserialize(myFileStream);
                ng.Description = DescriptionOrig;

                return ng;
            }
        }

        public Boolean UpdateConfiguration() {

            String gatewayXml = StarcounterEnvironment.Gateway.PathToGatewayConfig;
            String backupFile = gatewayXml + ".bak";
            Boolean updateSuccess = false;

            try {

                // Deleting existing backup file if any.
                if (File.Exists(backupFile)) {
                    File.Delete(backupFile);
                }

                // Renaming current working gateway XML to backup.
                File.Move(gatewayXml, backupFile);

                // Serializing to the actual gateway xml.
                Serialize(gatewayXml);

                // Sending update configuration request to gateway.
                Response resp = Http.GET("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/gw/updateconf");

                if (resp.IsSuccessStatusCode) {

                    // Gateway successfully updated configuration.
                    updateSuccess = true;
                    File.Delete(backupFile);

                    return true;
                }

            }
            finally {

                // Gateway failed to update configuration.
                if (!updateSuccess) {
                    File.Delete(gatewayXml);
                    File.Move(backupFile, gatewayXml);
                }
            }

            return false;
        }

        /// <summary>
        /// Reverse Proxy.
        /// </summary>
        public class ReverseProxy {

            public string DestinationIP;

            public ushort DestinationPort;

            public ushort StarcounterProxyPort;

            public string MatchingHost;
        }

        /// <summary>
        /// Uri Alias.
        /// </summary>
        public class UriAlias {

            public string HttpMethod;

            public string FromUri;

            public string ToUri;

            public ushort Port;
        }
    }

    /*
    class Program {

        static void Main(string[] args) {

            GatewayConfig ab = new GatewayConfig();
            ab.AddOrReplaceUriAlias(new UriAlias {
                HttpMethod = "GET",
                FromUri = "/",
                ToUri = "/",
                Port = 1234

            });

            ab.Serialize("ahah.xml");
        }
    }
    */
}
