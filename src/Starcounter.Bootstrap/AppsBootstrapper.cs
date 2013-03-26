
using HttpStructs;
using Starcounter.Advanced;
using Starcounter.Apps.Bootstrap;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;
using System;
using System.Diagnostics;
using System.IO;
namespace Starcounter.Internal {

    /// <summary>
    /// Sets up the REST, Hypermedia and Apps modules
    /// </summary>
    /// <remarks>
    /// A common dependency injection pattern for all bootstrapping in Starcounter should be
    /// considered for a future version. This includes Apps, SQL and other modules.
    /// </remarks>
    public static class AppsBootstrapper {

        private static HttpAppServer appServer;
        // private static StaticWebServer fileServer;

        /// <summary>
        /// Initializes AppsBootstrapper.
        /// </summary>
        /// <param name="defaultPort"></param>
        public static void InitAppsBootstrapper(
            Byte numSchedulers,
            UInt16 defaultUserHttpPort,
            UInt16 defaultSystemHttpPort,
            String dbName)
        {
            // Setting some configuration settings.
            NewConfig.Default.UserHttpPort = defaultUserHttpPort;
            NewConfig.Default.SystemHttpPort = defaultSystemHttpPort;

            NewConfig.IsAdministratorApp = (0 == String.Compare(dbName, MixedCodeConstants.AdministratorAppName, true));
            Node.ThisStarcounterNode = new Node("127.0.0.1", NewConfig.Default.SystemHttpPort);

            // Dependencyinjection for db and transaction calls.
            StarcounterBase._DB = new DbImpl();

            // Setting the response handler.
            Node.SetHandleResponse(appServer.HandleResponse);

            // Giving REST needed delegates.
            UserHandlerCodegen.Setup(
                GatewayHandlers.RegisterUriHandler,
                OnHttpMessageRoot);

            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(numSchedulers);

            // Explicitly starting inactive sessions cleanup.
            for (Byte i = 0; i < numSchedulers; i++)
            {
                // Getting sessions for current scheduler.
                SchedulerSessions schedSessions = GlobalSessions.AllGlobalSessions.GetSchedulerSessions(i);

                // Starting inactive sessions cleanup for this scheduler.
                DbSession dbs = new DbSession();
                dbs.RunAsync(() => schedSessions.InactiveSessionsCleanupRoutine(), i);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        static AppsBootstrapper() {
            var fileServer = new StaticWebServer();
            appServer = new HttpAppServer(fileServer);
            StarcounterBase.Fileserver = fileServer;

            // Checking if we are inside the database worker process.
            AppProcess.AssertInDatabaseOrSendStartRequest();
        }

        /// <summary>
        /// Adds a directory to the list of directories used by the web server to
        /// resolve GET requests for static content.
        /// </summary>
        /// <param name="path">The directory to include</param>
        public static void AddFileServingDirectory(string path) {
            StarcounterBase.Fileserver.UserAddedLocalFileDirectoryWithStaticContent(path);
        }

        /// <summary>
        /// Function that registers a default handler in the gateway and handles incoming requests
        /// and dispatch them to Apps. Also registers internal handlers for jsonpatch.
        /// </summary>
        /// <param name="port">Listens for http traffic on the given port. </param>
        /// <param name="resourceResolvePath">Adds a directory path to the list of paths used when resolving a request for a static REST (web) resource</param>
        public static void Bootstrap(
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            string resourceResolvePath = null)
        {
            // Checking for the port.
            if (port == StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort)
                port = StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort;

            // Setting default user port.
            NewConfig.Default.UserHttpPort = port;

            if (resourceResolvePath != null)
            {
                AddFileServingDirectory(resourceResolvePath);

                // Registering static content directory with Administrator.

                // Getting bytes from string.
                //byte[] staticContentDirBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(resourceResolvePath);

                // Converting path string to base64 string.
                //string staticContentDirBase64 = System.Convert.ToBase64String(staticContentDirBytes);

                // Checking if this is not administrator.
                if (!NewConfig.IsAdministratorApp)
                {
                    // Putting port and full path to resources directory.
                    String content = port + StarcounterConstants.NetworkConstants.CRLF + Path.GetFullPath(resourceResolvePath);

                    // Sending REST POST request to Administrator to register static resources directory.
                    Node.ThisStarcounterNode.POST("/addstaticcontentdir", content, null, (Response resp) =>
                    {
                        String respString = resp.GetContentStringUtf8_Slow();

                        if ("Success!" != respString)
                            throw new Exception("Could not register static resources directory with administrator!");

                        return "Success!";
                    });
                }
            }
        }

        /// <summary>
        /// Entry-point for all incoming http requests from the Network Gateway.
        /// </summary>
        /// <param name="request">The http request</param>
        /// <returns>Returns true if the request was handled</returns>
        private static Boolean OnHttpMessageRoot(Request request) {
            var result = (Response)appServer.Handle(request);
            request.SendResponse(result.Uncompressed, 0, result.Uncompressed.Length);
            return true;
        }
    }
}
