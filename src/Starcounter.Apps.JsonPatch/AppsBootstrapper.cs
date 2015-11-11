using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Starcounter.Advanced;
using Starcounter.Internal.Web;
using Starcounter.Logging;
using Starcounter.Rest;
using System.Collections.Concurrent;
using System.Web;

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
        internal static AppRestServer AppServer {
            get { return AppServer_; }
        }

        // NOTE: Timer should be static, otherwise its garbage collected.
        static Timer sessionCleanupTimer_;
        static Timer dateUpdateTimer_;

        // Node error log source.
        static LogSource NodeErrorLogSource = new LogSource("Node");

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
            Boolean noNetworkGateway) {
            // Setting host exception logging for internal.
            Diagnostics.SetHostLogException((Exception exc) => {
                LogSources.Hosting.LogException(exc);
            });

            // Setting the check for handler registration.
            Handle.isHandlerRegistered_ = UriManagedHandlersCodegen.IsHandlerRegistered;
            Handle.resolveStaticResource_ = AppServer_.ResolveAndPrepareFile;

            // Setting some configuration settings.
            StarcounterEnvironment.Default.UserHttpPort = defaultUserHttpPort;
            StarcounterEnvironment.Default.SystemHttpPort = defaultSystemHttpPort;
            StarcounterEnvironment.Default.SessionTimeoutMinutes = sessionTimeoutMinutes;

            StarcounterEnvironment.IsAdministratorApp = (0 == String.Compare(dbName, MixedCodeConstants.AdministratorAppName, true));

            // Allow reading of JSON-by-example files at runtime
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
                    AppServer_.RunDelegateAndProcessResponse,
                    UriManagedHandlersCodegen.RunUriMatcherAndCallHandler);

                AllWsGroups.WsManager.InitWebSockets(GatewayHandlers.RegisterWsChannelHandlerNative);
            }

            // Injecting required hosted Node functionality.
            Node.InjectHostedImpl(
                UriManagedHandlersCodegen.RunUriMatcherAndCallHandler,
                NodeErrorLogSource.LogException);

            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(numSchedulers);

            // Creating scheduler resources.
            SchedulerResources.Init(numSchedulers);

            // Getting server directory path.
            if (!StarcounterEnvironment.IsAdministratorApp && !noNetworkGateway) {
                StarcounterEnvironment.Server.ServerDir = Http.GET<String>("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/sc/conf/serverdir");
                StarcounterEnvironment.Gateway.PathToGatewayConfig = Path.Combine(StarcounterEnvironment.Server.ServerDir, StarcounterEnvironment.FileNames.GatewayConfigFileName);
            }

            if (!noNetworkGateway) {

                // Registering JSON patch handlers on default user port.
                PuppetRestHandler.RegisterJsonPatchHandlers(defaultUserHttpPort);

                // Initializing URI mapping for ordinary and ontology mappings.
                UriMapping.Init();

                Handle.GET(defaultSystemHttpPort, "/schedulers/" + dbName, () => {

                    String allResults = "{\"Schedulers\":[";

                    for (Int32 i = 0; i < StarcounterEnvironment.SchedulerCount; i++) {

                        allResults += "{\"SchedulerId\":\"" + GatewayHandlers.SchedulerNumRequests[i] + "\"}";

                        if (i < (StarcounterEnvironment.SchedulerCount - 1))
                            allResults += ",";
                    }

                    allResults += "]}";

                    return allResults;
                });

                if (!StarcounterEnvironment.IsAdministratorApp) {

                    // Registering redirection port.
                    Handle.GET(defaultSystemHttpPort, "/sc/redirect/" + defaultUserHttpPort + "{?};{?}", (String fromUri, String toUri) => {

                        RegisterRedirectHandler(defaultUserHttpPort, Handle.GET_METHOD, fromUri, toUri);

                        return 200;
                    }, new HandlerOptions() { SkipMiddlewareFilters = true });

                    // Registering URI aliasing port.
                    Handle.GET(defaultSystemHttpPort, "/sc/alias/" + defaultUserHttpPort + "{?};{?}", (String fromUri, String toUri) => {

                        RegisterUriAliasHandler(Handle.GET_METHOD, fromUri, toUri, defaultUserHttpPort);

                        return 200;
                    }, new HandlerOptions() { SkipMiddlewareFilters = true });
                }
                else {

                    // Registering Reverse proxy handlers
                    RegisterReverseProxyHandlers(defaultSystemHttpPort);
                }
            }

            // Starting a timer that will schedule a job for the session-cleanup on each scheduler.
            DbSession dbSession = new DbSession();
            int interval = 1000 * 60;
            sessionCleanupTimer_ = new Timer((state) => {
                // Schedule a job to check once for inactive sessions on each scheduler.
                for (Byte i = 0; i < numSchedulers; i++) {
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
        /// Initializing the codehost apps.
        /// </summary>
        static AppsBootstrapper() {

            // Constructing file server for all ports.
            ConcurrentDictionary<UInt16, StaticWebServer> fileServer = new ConcurrentDictionary<UInt16, StaticWebServer>();
            AppServer_ = new AppRestServer(fileServer);
        }

        /// <summary>
        /// Gets a list of directories used by the web server to
        /// resolve GET requests for static content.
        /// </summary>
        internal static Dictionary<UInt16, IList<string>> GetFileServingDirectories() {
            return AppServer_.GetWorkingDirectories();
        }

        /// <summary>
        /// Adding static files directory.
        /// </summary>
        /// <param name="webResourcesDir">Path to static files directory.</param>
        /// <param name="port">Port number.</param>
        public static void AddStaticFileDirectory(String webResourcesDir,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort) {

            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == port) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    port = StarcounterEnvironment.Default.SystemHttpPort;
                }
                else {
                    port = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

            InternalAddStaticFileDirectory(port, webResourcesDir, StarcounterEnvironment.AppName);
        }

        /// <summary>
        /// Adding static files directory.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <param name="webResourcesDir">Path to static files directory.</param>
        /// <param name="appName">Application name.</param>
        internal static void InternalAddStaticFileDirectory(UInt16 port, String webResourcesDir, String appName) {

            // Checking if resources directory exists.
            if (!Directory.Exists(webResourcesDir)) {
                throw ErrorCode.ToException(Error.SCERRCANTACCESSAPPRESOURCEDIR, "Directory path: " + webResourcesDir);
            }

            /*
            // Checking that we don't have a network path registered for resource directory.
            if (!HelperFunctions.IsDirectoryLocal(webResourcesDir)) {
                throw new ArgumentOutOfRangeException("You are trying to use a resource directory that is on network drive (file modifications are not observable on network drives): " + webResourcesDir);
            }
            */

            // Obtaining full path to directory.
            String fullPathToResourcesDir = Path.GetFullPath(webResourcesDir);

            // Registering files directory.
            AppServer_.UserAddedLocalFileDirectoryWithStaticContent(appName, port, fullPathToResourcesDir);
        }

        /// <summary>
        /// Registering URI alias handler.
        /// </summary>
        static void RegisterUriAliasHandler(String httpMethod, String fromUri, String toUri, UInt16 port) {

            NetworkGateway conf = NetworkGateway.Deserealize();

            NetworkGateway.UriAlias a = new NetworkGateway.UriAlias() {
                HttpMethod = httpMethod,
                FromUri = fromUri,
                ToUri = toUri,
                Port = port
            };

            conf.AddOrReplaceUriAlias(a);
            conf.UpdateConfiguration();
        }

        /// <summary>
        /// Registering redirect handler.
        /// </summary>
        static void RegisterRedirectHandler(UInt16 port, String method, String fromUri, String toUri) {

            String origAppName = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = null;

            try {

                Handle.CUSTOM(port, method + " " + fromUri, (Request req) => {

                    Response resp = new Response() {
                        StatusCode = 302,
                        StatusDescription = "Found"
                    };

                    resp["Location"] = "http://" + req.Host + toUri;

                    return resp;

                }, new HandlerOptions() {
                    ReplaceExistingHandler = true,
                    ProxyDelegateTrigger = true
                });

            }
            finally {

                StarcounterEnvironment.AppName = origAppName;
            }
        }

        /// <summary>
        /// Registering reverse proxy handler.
        /// </summary>
        static void RegisterReverseProxyHandlers(ushort port) {

            Handle.GET(port, "/sc/reverseproxies", () => {
                try {
                    NetworkGateway conf = NetworkGateway.Deserealize();
                    ReverseProxiesJson reverseProxiesJson = new ReverseProxiesJson();
                    reverseProxiesJson.Items.Data = conf.ReverseProxies;

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, Body = reverseProxiesJson.ToJson() };
                }
                catch (Exception e) {

                    ErrorResponse errorResponse = new ErrorResponse();
                    errorResponse.message = e.Message;
                    errorResponse.stackTrace = e.StackTrace;
                    errorResponse.helpLink = e.HelpLink;

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse.ToJson() };
                }
            }, new HandlerOptions() { SkipMiddlewareFilters = true });

            Handle.PUT(port, "/sc/reverseproxies", (Request req) => {
                try {
                    NetworkGateway conf = NetworkGateway.Deserealize();

                    Starcounter.Internal.NetworkGateway.ReverseProxy reverseProxy = new NetworkGateway.ReverseProxy();
                    ReverseProxyJson reverseProxyJson = new ReverseProxyJson();
                    reverseProxyJson.Data = reverseProxy;
                    reverseProxyJson.PopulateFromJson(req.Body);

                    bool bExists = conf.AddOrReplaceReverseProxy(reverseProxy);

                    if (bExists) {
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
                    }

                    bool bSuccess = conf.UpdateConfiguration();
                    if (bSuccess == false) {
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.message = "Failed to update the configuration file, Consult the Starcounter log for more information.";
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse.ToJson() };
                    }

                    Dictionary<String, String> headersDictionary = new Dictionary<string, string>();
                    headersDictionary.Add("Location", string.Format("{0}/{1}/{2}", req.Uri, HttpUtility.UrlEncode(reverseProxy.MatchingHost), reverseProxy.StarcounterProxyPort));

                    Response resp = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = reverseProxyJson.ToJson() };
                    resp.SetHeadersDictionary(headersDictionary);

                    return resp;
                }
                catch (Exception e) {

                    ErrorResponse errorResponse = new ErrorResponse();
                    errorResponse.message = e.Message;
                    errorResponse.stackTrace = e.StackTrace;
                    errorResponse.helpLink = e.HelpLink;

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse.ToJson() };
                }

            }, new HandlerOptions() { SkipMiddlewareFilters = true });


            Handle.DELETE(port, "/sc/reverseproxies/{?}/{?}", (string matchingHost, string starcounterProxyPort, Request req) => {
                try {
                    NetworkGateway conf = NetworkGateway.Deserealize();

                    Starcounter.Internal.NetworkGateway.ReverseProxy reverseProxy = new NetworkGateway.ReverseProxy() { MatchingHost = HttpUtility.UrlDecode(matchingHost), StarcounterProxyPort = ushort.Parse(starcounterProxyPort) };

                    bool bRemoved = conf.RemoveReverseProxy(reverseProxy);

                    if (bRemoved) {
                        bool bSuccess = conf.UpdateConfiguration();
                        if (bSuccess == false) {
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.message = "Failed to update the configuration file, Consult the Starcounter log for more information.";
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse.ToJson() };
                        }
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                }
                catch (Exception e) {

                    ErrorResponse errorResponse = new ErrorResponse();
                    errorResponse.message = e.Message;
                    errorResponse.stackTrace = e.StackTrace;
                    errorResponse.helpLink = e.HelpLink;

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse.ToJson() };
                }

            }, new HandlerOptions() { SkipMiddlewareFilters = true });
        }

        /// <summary>
        /// Function that registers a default handler in the gateway and handles incoming requests
        /// and dispatch them to Apps. Also registers internal handlers for jsonpatch.
        /// </summary>
        internal static void Bootstrap(
            UInt16 port,
            String webResourcesDir,
            String appName) {

            // Checking if there is no network gateway, then just returning.
            if (StarcounterEnvironment.NoNetworkGatewayFlag)
                return;

            // By default middleware filters are enabled.
            StarcounterEnvironment.MiddlewareFiltersEnabled = true;

            // Adding Starcounter specific static files directory.
            String specialStaticFiles = Path.Combine(StarcounterEnvironment.InstallationDirectory, "ClientFiles\\StaticFiles");
            if (Directory.Exists(specialStaticFiles)) {
                AddStaticFileDirectory(specialStaticFiles);
            }

            // Checking if there is a given web resource path.
            if (webResourcesDir != null) {

                // Obtaining full path to directory.
                String fullPathToResourcesDir = Path.GetFullPath(webResourcesDir);

                // Path to wwwroot folder, if any exists.
                String extendedResourceDirPath = Path.Combine(fullPathToResourcesDir, StarcounterConstants.WebRootFolderName);

                if (Directory.Exists(extendedResourceDirPath)) {

                    fullPathToResourcesDir = extendedResourceDirPath;

                }
                else {

                    extendedResourceDirPath = Path.Combine(fullPathToResourcesDir, "src", appName, StarcounterConstants.WebRootFolderName);

                    if (Directory.Exists(extendedResourceDirPath)) {
                        fullPathToResourcesDir = extendedResourceDirPath;
                    }
                }

                // Adding found directory to static file server.
                InternalAddStaticFileDirectory(port, fullPathToResourcesDir, appName);
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

                // Setting calling level to -1 because internal call will be made immediately.
                Handle.CallLevel = -1;

                // Getting handler information.
                UriHandlersManager uhm = UriHandlersManager.GetUriHandlersManager(HandlerOptions.HandlerLevels.DefaultLevel);
                UserHandlerInfo uhi = uhm.AllUserHandlerInfos[req.ManagedHandlerId];
                if (!uhi.SkipMiddlewareFilters) {
                    // Checking if there is a filtering delegate.
                    resp = Handle.RunMiddlewareFilters(req);
                }

                // Checking if filter level did allow this request.
                if (null == resp) {

                    // Setting empty handler options.
                    req.HandlerOpts = new HandlerOptions();

                    // Handling request on initial level.
                    resp = AppServer_.RunDelegateAndProcessResponse(
                        req.GetRawMethodAndUri(),
                        req.GetRawParametersInfo(),
                        req);
                }

            }
            catch (ResponseException exc) {

                resp = exc.ResponseObject;
                resp.ConnFlags = Response.ConnectionFlags.DisconnectAfterSend;

            }
            catch (UriInjectMethods.IncorrectSessionException) {

                resp = Response.FromStatusCode(400);
                resp.Body = "Incorrect session supplied!";
                resp["Connection"] = "close";

            }
            catch (Exception exc) {

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

                case HandlerStatusInternal.ResourceNotFound:
                case HandlerStatusInternal.Done: {

                        // Creating response serialization buffer.
                        if (responseSerializationBuffer_ == null) {
                            responseSerializationBuffer_ = new Byte[DefaultResponseSerializationBufferSize];
                        }

                        // Standard response send.
                        try {
                            req.SendResponse(resp, responseSerializationBuffer_);
                        }
                        catch (Exception ex) {
                            // Exception when constructing or sending response. Can happen for example 
                            // if the mimeconverter for the resource fails.
                            LogSources.Hosting.LogException(ex);
                            resp = Response.FromStatusCode(500);
                            resp.Body = AppRestServer.GetExceptionString(ex);
                            resp.ContentType = "text/plain";
                            req.SendResponse(resp, responseSerializationBuffer_);
                        }

                        break;
                    }

                default: {

                        // Checking if request should be finalized.
                        if (req.ShouldBeFinalized())
                            req.CreateFinalizer();

                        break;
                    }
            }

            return true;
        }
    }
}
