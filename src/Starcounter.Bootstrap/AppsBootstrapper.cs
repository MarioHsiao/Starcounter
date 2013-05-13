
using HttpStructs;
using Starcounter.Advanced;
using Starcounter.Apps.Bootstrap;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
namespace Starcounter.Internal {

    /// <summary>
    /// Sets up the REST, Hypermedia and Apps modules
    /// </summary>
    /// <remarks>
    /// A common dependency injection pattern for all bootstrapping in Starcounter should be
    /// considered for a future version. This includes Apps, SQL and other modules.
    /// </remarks>
    public static class AppsBootstrapper {

        private static HttpAppServer AppServer_;
        public static HttpAppServer AppServer
        {
            get { return AppServer_; }
        }

        // NOTE: Timer should be static, otherwise its garbage collected.
        private static Timer sessionCleanupTimer;

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

            // Dependency injection for db and transaction calls.
            StarcounterBase._DB = new DbImpl();

            // Setting the response handler.
            Node.SetHandleResponse(AppServer_.HandleResponse);

            // Giving REST needed delegates.
            UserHandlerCodegen.Setup(
                GatewayHandlers.RegisterUriHandler,
                OnHttpMessageRoot,
                AppServer_.HandleRequest);

            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(numSchedulers);

            // Starting a timer that will schedule a job for the session-cleanup on each scheduler.
            DbSession dbSession = new DbSession();
            int interval = 1000 * 60 * SchedulerSessions.DefaultSessionTimeoutMinutes;
            sessionCleanupTimer = new Timer((state) => {
                    // Schedule a job to check once for inactive sessions on each scheduler.
                    for (Byte i = 0; i < numSchedulers; i++)
                    {
                        // Getting sessions for current scheduler.
                        SchedulerSessions schedSessions = GlobalSessions.AllGlobalSessions.GetSchedulerSessions(i);
                        dbSession.RunAsync(() => schedSessions.InactiveSessionsCleanupRoutine(), i);
                    }                                
                }, 
                null, interval, interval);
        }

        /// <summary>
        /// 
        /// </summary>
        static AppsBootstrapper() {
            Dictionary<UInt16, StaticWebServer> fileServer = new Dictionary<UInt16, StaticWebServer>();
            AppServer_ = new HttpAppServer(fileServer);

            // Checking if we are inside the database worker process.
            AppProcess.AssertInDatabaseOrSendStartRequest();
        }

        /// <summary>
        /// Adds a directory to the list of directories used by the web server to
        /// resolve GET requests for static content.
        /// </summary>
        /// <param name="path">The directory to include</param>
        public static void AddFileServingDirectory(UInt16 port, String path) {
            AppServer_.UserAddedLocalFileDirectoryWithStaticContent(port, path);
        }

        /// <summary>
        /// Function that registers a default handler in the gateway and handles incoming requests
        /// and dispatch them to Apps. Also registers internal handlers for jsonpatch.
        /// </summary>
        /// <param name="port">Listens for http traffic on the given port. </param>
        /// <param name="resourceResolvePath">Adds a directory path to the list of paths used when resolving a request for a static REST (web) resource</param>
        public static void Bootstrap(
            string resourceResolvePath = null,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort
            )
        {
            // Checking for the port.
            if (port == StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort)
            {
                port = NewConfig.Default.UserHttpPort;
            }
            else
            {
                // Setting default user port.
                NewConfig.Default.UserHttpPort = port;
            }

            if (resourceResolvePath != null)
            {
                // Registering static content directory with Administrator.

                // Getting bytes from string.
                //byte[] staticContentDirBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(resourceResolvePath);

                // Converting path string to base64 string.
                //string staticContentDirBase64 = System.Convert.ToBase64String(staticContentDirBytes);

                // Checking if this is not administrator.
                if (!NewConfig.IsAdministratorApp)
                {
                    // Putting port and full path to resources directory.
                    String body = port + StarcounterConstants.NetworkConstants.CRLF + Path.GetFullPath(resourceResolvePath);

                    // Sending REST POST request to Administrator to register static resources directory.
                    Node.LocalhostSystemPortNode.POST("/addstaticcontentdir", body, null, null, (Response resp) =>
                    {
                        String respString = resp.GetBodyStringUtf8_Slow();

                        if ("Success!" != respString)
                            throw new Exception("Could not register static resources directory with administrator!");

                        return "Success!";
                    });
                }
                else
                {
                    // Administrator registers itself.
                    AddFileServingDirectory(port, resourceResolvePath);
                }
            }
        }

        /// <summary>
        /// Entry-point for all incoming http requests from the Network Gateway.
        /// </summary>
        /// <param name="request">The http request</param>
        /// <returns>Returns true if the request was handled</returns>
        private static Boolean OnHttpMessageRoot(Request request)
        {
            Response response = AppServer_.HandleRequest(request);

            if (response != null)
                request.SendResponse(response.Uncompressed, 0, response.Uncompressed.Length);

            request.Destroy();

            return true;
        }
    }
}
