
using Starcounter.Advanced;
using Starcounter.Internal.Web;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {

    /// <summary>
    /// Provides handlers for REST calls targeting the admin server API
    /// root resource (i.e. the principal API REST entrypoint).
    /// </summary>
    internal static partial class RootHandler {
        /// <summary>
        /// Provides all handler classes with a single URI set and makes
        /// sure it's only accessible from one place.
        /// </summary>
        /// <remarks>
        /// This way, we can start the admin server with a dependency
        /// injection pattern with what URI set to use for the API, using
        /// different sets in tests (like test doubles) and experiment
        /// with a new set for a newer version of the API if we like.
        /// </remarks>
        public static AdminAPI API { get; private set; }

        /// <summary>
        /// Provides a set of references to the currently running
        /// admin server host. Used and shared by handlers when fullfilling
        /// REST requests.
        /// </summary>
        public static class Host {
            public static ServerEngine Engine { get; private set; }
            public static IServerRuntime Runtime { get; private set; }
            public static string ServerHost { get; private set; }
            public static int ServerPort { get; private set; }

            public static void Setup(
                string serverHost,
                int serverPort,
                ServerEngine engine,
                IServerRuntime runtime) {
                Engine = engine;
                Runtime = runtime;
                ServerHost = serverHost;
                ServerPort = serverPort;
            }
        }

        /// <summary>
        /// Sets up the root API handler.
        /// </summary>
        /// <param name="adminAPI">The API to be used by all
        /// fellow handlers.</param>
        public static void Setup(AdminAPI adminAPI) {
            API = adminAPI;
        }

        /// <summary>
        /// Registers a handler that returns 405 (Method Not Allowed) for
        /// a given URI and the set of standard verbs/methods that it don't
        /// explicitly provide. The handler confirms to HTTP/1.1 in that it
        /// will return a response with the Allow header set, containing
        /// all methods supported.
        /// </summary>
        /// <param name="uri">The URI to register handler(s) for.</param>
        /// <param name="methodsSupported">The methods supported by the
        /// resource reprsented by the given URI.</param>
        /// <param name="allowExtensionsBeyondPatch">Tells the method to
        /// relax and don't check the set of supported methods against the
        /// set of known ones.</param>
        public static void Register405OnAllUnsupported(string uri, string[] methodsSupported, bool allowExtensionsBeyondPatch = false) {
            var restHandler = Handle._REST;
            var verbs = new string[] { "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", "PATCH" };

            var allows = string.Empty;
            foreach (var allowedMethod in methodsSupported) {
                if (!allowExtensionsBeyondPatch) {
                    if (!verbs.Contains(allowedMethod)) {
                        throw new ArgumentOutOfRangeException("methodsSupported", string.Format("HTTP method {0} not recognized", allowedMethod));
                    }
                }
                allows += " " + allowedMethod + ",";
            }
            allows = allows.TrimEnd(',');

            var headers = new NameValueCollection();
            headers.Add("Allow", allows);

            Func<Request, Response> return405 = (Request request) => {
                return new Response {
                    Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent(405, headers, null)
                };
            };

            foreach (var verb in verbs) {
                if (methodsSupported.Contains(verb))
                    continue;

                restHandler.RegisterHandler((ushort)Host.ServerPort, verb + " " + uri, return405);
            }
        }
    }
}