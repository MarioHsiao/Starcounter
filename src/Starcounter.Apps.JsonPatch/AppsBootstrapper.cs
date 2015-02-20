using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Starcounter.Advanced;
using Starcounter.Internal.Web;
using Starcounter.Logging;
using Starcounter.Rest;

namespace Starcounter.Internal {

    /// <summary>
    /// Sets up the REST, Resources and Apps modules
    /// </summary>
    /// <remarks>
    /// A common dependency injection pattern for all bootstrapping in Starcounter should be
    /// considered for a future version. This includes Apps, SQL and other modules.
    /// </remarks>
    public static class AppsBootstrapper {

        private static AppRestServer AppServer_;
        internal static AppRestServer AppServer
        {
            get { return AppServer_; }
        }

        // NOTE: Timer should be static, otherwise its garbage collected.
        static Timer sessionCleanupTimer_;
        static Timer dateUpdateTimer_;

        // Node error log source.
        static LogSource NodeErrorLogSource = new LogSource("Node");

        // private static StaticWebServer fileServer;

        /// <summary>
        /// Initializes AppsBootstrapper.
        /// </summary>
        /// <param name="defaultPort"></param>
        internal static void InitAppsBootstrapper(
            Byte numSchedulers,
            UInt16 defaultUserHttpPort,
            UInt16 defaultSystemHttpPort,
            UInt32 sessionTimeoutMinutes,
            String dbName,
            Boolean noNetworkGateway)
        {
            // Setting some configuration settings.
            StarcounterEnvironment.Default.UserHttpPort = defaultUserHttpPort;
            StarcounterEnvironment.Default.SystemHttpPort = defaultSystemHttpPort;
            StarcounterEnvironment.Default.SessionTimeoutMinutes = sessionTimeoutMinutes;

            StarcounterEnvironment.IsAdministratorApp = (0 == String.Compare(dbName, MixedCodeConstants.AdministratorAppName, true));

            // // Allow reading of JSON-by-example files at runtime
            // Starcounter_XSON_JsonByExample.Initialize();

            // Dependency injection for db and transaction calls.
            StarcounterBase._DB = new DbImpl();
            DbSession dbs = new DbSession();
            ScSessionClass.SetDbSessionImplementation(dbs);

            // Dependency injection for converting puppets to html
            Starcounter.Internal.XSON.Modules.Starcounter_XSON.Injections.JsonMimeConverter = new JsonMimeConverter();

            // Giving REST needed delegates.
            unsafe {
                UriManagedHandlersCodegen.Setup(
                    GatewayHandlers.RegisterUriHandlerNative,
                    GatewayHandlers.RegisterTcpSocketHandler,
                    GatewayHandlers.RegisterUdpSocketHandler,
                    ProcessExternalRequest,
                    AppServer_.RunDelegateAndProcessResponse);

                AllWsChannels.WsManager.InitWebSockets(GatewayHandlers.RegisterWsChannelHandlerNative);
            }

            // Injecting required hosted Node functionality.
            Node.InjectHostedImpl(
                UriManagedHandlersCodegen.RunUriMatcherAndCallHandler,
                NodeErrorLogSource.LogException);

            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(numSchedulers);

            SchedulerResources.Init(numSchedulers);

            // Registering JSON patch handlers on default user port.
            if (!noNetworkGateway) {
                PuppetRestHandler.RegisterJsonPatchHandlers(defaultUserHttpPort);
            }

            // Starting a timer that will schedule a job for the session-cleanup on each scheduler.
            DbSession dbSession = new DbSession();
            int interval = 1000 * 60;
            sessionCleanupTimer_ = new Timer((state) => {
                    // Schedule a job to check once for inactive sessions on each scheduler.
                    for (Byte i = 0; i < numSchedulers; i++)
                    {
                        // Getting sessions for current scheduler.
                        SchedulerSessions schedSessions = GlobalSessions.AllGlobalSessions.GetSchedulerSessions(i);
                        dbSession.RunAsync(() => schedSessions.InactiveSessionsCleanupRoutine(), i);
                    }                                
                }, 
                null, interval, interval);
            
            // Starting procedure to update current date header for every response.
            dateUpdateTimer_ = new Timer(Response.HttpDateUpdateProcedure, null, 1000, 1000);
        }

        /// <summary>
        /// 
        /// </summary>
        static AppsBootstrapper() {
            Dictionary<UInt16, StaticWebServer> fileServer = new Dictionary<UInt16, StaticWebServer>();
            AppServer_ = new AppRestServer(fileServer);
        }

        /// <summary>
        /// Adds a directory to the list of directories used by the web server to
        /// resolve GET requests for static content.
        /// </summary>
        /// <param name="path">The directory to include</param>
        internal static void AddFileServingDirectory(UInt16 port, String path) {
            AppServer_.UserAddedLocalFileDirectoryWithStaticContent(port, path);
        }

        /// <summary>
        /// Gets a list of directories used by the web server to
        /// resolve GET requests for static content.
        /// </summary>
        internal static Dictionary<UInt16, IList<string>> GetFileServingDirectories() {
            return AppServer_.GetWorkingDirectories();
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
                port = StarcounterEnvironment.Default.UserHttpPort;
            }
            else
            {
                // Setting default user port.
                StarcounterEnvironment.Default.UserHttpPort = port;
            }

            if (resourceResolvePath != null)
            {
                // Administrator registers itself.
                AddFileServingDirectory(port, Path.GetFullPath(resourceResolvePath));

                // Checking if this is not administrator.
                if (!StarcounterEnvironment.IsAdministratorApp)
                {
                    // Putting port and full path to resources directory.
                    String body = port + StarcounterConstants.NetworkConstants.CRLF + Path.GetFullPath(resourceResolvePath);

                    // Sending REST POST request to Administrator to register static resources directory.
                    Node.LocalhostSystemPortNode.POST("/addstaticcontentdir", body, null, null, (Response resp, Object userObject) => {

                        if ("Success!" != resp.Body)
                            throw new Exception("Could not register static resources directory with administrator!");
                    });

                    // Checking if its a Polyjuice edition and then adding Polyjuice specific static files directory.
                    String polyjuiceStatic = Path.Combine(StarcounterEnvironment.InstallationDirectory, "Polyjuice\\StaticFiles");

                    // The following directory exists only in Polyjuice edition.
                    if (Directory.Exists(polyjuiceStatic)) {

                        body = port + "\r\n" + polyjuiceStatic;

                        Node.LocalhostSystemPortNode.POST("/addstaticcontentdir", body, null, null, (Response resp, Object userObject) => {

                            if ("Success!" != resp.Body) {
                                throw new Exception(string.Format("Failed to register the static resources directory ({0}).", body));
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Default size for static serialization buffer.
        /// </summary>
        const Int32 DefaultResponseSerializationBufferSize = 4096;

        [ThreadStatic]
        static Byte[] responseSerializationBuffer_;

        /// <summary>
        /// Entry-point for all external HTTP requests from the Network Gateway.
        /// </summary>
        private static Boolean ProcessExternalRequest(Request req) {

            Response resp = null;

            try {

                // Getting appropriate handler level manager.
                UriHandlersManager uhm = UriHandlersManager.GetUriHandlersManager(HandlerOptions.HandlerLevels.FilteringLevel);

                // Checking if there are any registered handlers.
                if (uhm.NumRegisteredHandlers > 0) {

                    req.PortNumber = UriHandlersManager.GetPortNumber(req,
                        new HandlerOptions());

                    resp = Node.FilterRequest(req);
                }

                // Checking if filter level did allow this request.
                if (null == resp) {

                    // Handling request on initial level.
                    resp = AppServer_.RunDelegateAndProcessResponse(
                        req.GetRawMethodAndUri(),
                        req.GetRawParametersInfo(),
                        req,
                        new HandlerOptions());
                }

            } catch (ResponseException exc) {

                resp = exc.ResponseObject;
                resp.ConnFlags = Response.ConnectionFlags.DisconnectAfterSend;

            } catch (UriInjectMethods.IncorrectSessionException) {

                resp = Response.FromStatusCode(400);
                resp["Connection"] = "close";

            } catch (Exception exc) {

                // Logging the exception to server log.
                LogSources.Hosting.LogException(exc);
                resp = Response.FromStatusCode(500);
                resp.Body = AppRestServer.GetExceptionString(exc);
                resp.ContentType = "text/plain";
            } 

            // Checking if response was handled.
            if (resp == null)
                return false;

            // Determining what we should do with response.
            switch (resp.HandlingStatus) {

                case HandlerStatusInternal.Done: {

                    // Creating response serialization buffer.
                    if (responseSerializationBuffer_ == null) {
                        responseSerializationBuffer_ = new Byte[DefaultResponseSerializationBufferSize];
                    }

                    // Standard response send.
                    req.SendResponse(resp, responseSerializationBuffer_);

                    break;
                }

                default: {
                    req.CreateFinalizer();
                    break;
                }
            }

            // Checking if a new session was created during handler call.
            if ((null != Session.Current) && (req.IsExternal))
                Session.End();

            Session.InitialRequest = null;

            return true;
        }
    }
}
