
using Codeplex.Data;
using Starcounter;
using Starcounter.ABCIPC.Internal;
using Starcounter.Administrator;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.REST;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

// http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.aspx

namespace Starcounter.Administrator {

    partial class Master : Json {

        public static IServerRuntime ServerInterface;
        public static ServerEngine ServerEngine;

        // Argument <path to server configuraton file> <portnumber>
        static void Main(string[] args) {

            if (args == null || args.Length < 1) {
                Console.WriteLine("Starcounter Administrator: Invalid arguments: Usage <path to server configuraton file>");
                return;
            }

            // Server configuration file.
            if (string.IsNullOrEmpty(args[0]) || !File.Exists(args[0])) {
                Console.WriteLine("Starcounter Administrator: Missing server configuration file {0}", args[0]);
            }

            // Administrator port.
            UInt16 adminPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
            if (UInt16.TryParse(args[1], out adminPort) == false) {
                Console.WriteLine("Starcounter Administrator: Invalid port number {0}", args[1]);
                return;
            };

            Console.WriteLine("Starcounter Administrator started on port: " + adminPort);

            AppsBootstrapper.Bootstrap(adminPort, "scadmin");
            //AppsBootstrapper.Bootstrap(adminPort, @"c:\github\Level1\src\Starcounter.Administrator");   // TODO:REMOVE

            Master.ServerEngine = new ServerEngine(args[0]);      // .srv\Personal\Personal.server.config
            Master.ServerEngine.Setup();
            Master.ServerInterface = Master.ServerEngine.Start();

            // Start Engine services
            StartListeningService();

            // Start listening on log-events
            ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

            LogApp.Setup(serverInfo.Configuration.LogDirectory);

            HostedExecutables.Setup(
                Dns.GetHostEntry(String.Empty).HostName,
                adminPort,
                Master.ServerEngine,
                Master.ServerInterface
            );

            RegisterHandlers(serverInfo.Configuration.SystemHttpPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sytemHttpPort">Port for doing SQL queries</param>
        static void RegisterHandlers(ushort sytemHttpPort) {

            // Registering default handler for ALL static resources on the server.
            GET("/{?}", (string res) => {
                return null;
            });

            // Redirecting root to index.html.
            GET("/", (Request req) => {
                Response resp;

                // Doing another request with original request attached.
                Node.LocalhostSystemPortNode.GET("/index.html", null, req, out resp);

                if (resp == null)
                    throw ErrorCode.ToException(Error.SCERRENDPOINTUNREACHABLE);

                // Returns this response to original request.
                return resp;
            });

            #region Server
            // Accept "", text/html, OR application/json. Otherwise, 406.
            GET("/server", () => {

                ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                ServerApp serverApp = new ServerApp();
                serverApp.SystemHttpPort = serverInfo.Configuration.SystemHttpPort;
                serverApp.DatabaseDirectory = serverInfo.Configuration.DatabaseDirectory;
                serverApp.LogDirectory = serverInfo.Configuration.LogDirectory;
                serverApp.TempDirectory = serverInfo.Configuration.TempDirectory;
                serverApp.ServerName = serverInfo.Configuration.Name;

                return serverApp;
            });

            #endregion

            #region Command Status
            GET("/command/{?}", (string commandId, Request req) => {

                lock (Master.ServerInterface) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.isCompleted = true;
                    resultJson.exception = null;
                    resultJson.message = null;
                    resultJson.status = null;
                    resultJson.errors = new object[] { };

                    try {
                        // Get command
                        CommandInfo[] commandInfos = Master.ServerInterface.GetCommands();
                        foreach (CommandInfo command in commandInfos) {
                            if (command.Id.Value == commandId) {
                                resultJson.isCompleted = command.IsCompleted;
                                resultJson.message = command.Description;
                                resultJson.status = command.Status.ToString();

                                if (command.HasError) {
                                    int index = 0;
                                    foreach (ErrorInfo eInfo in command.Errors) {
                                        ErrorMessage emsg = eInfo.ToErrorMessage();
                                        resultJson.errors[index] = new { message = emsg.Message, helpLink = emsg.Helplink };
                                        index++;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
                }
            });

            #endregion

            #region Database(s)

            // Returns a list of databases
            GET("/databases", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                    DatabasesApp databaseList = new DatabasesApp();
                    foreach (var database in databases) {
                        DatabaseApp databaseApp = new DatabaseApp();
                        databaseApp.SetDatabaseInfo(database);
                        databaseList.DatabaseList.Add(databaseApp);
                    }
                    return databaseList;
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });

            // Create a database
            POST("/databases/{?}", (string databaseName, Request req) => {

                dynamic resultJson = new DynamicJson();
                resultJson.commandId = null;
                resultJson.exception = null;

                try {
                    // Getting POST contents.
                    String content = req.GetBodyStringUtf8_Slow();

                    // TODO: Validation of values

                    var json = DynamicJson.Parse(content);

                    var createDb = new CreateDatabaseCommand(Master.ServerEngine, json.databaseName);

                    createDb.SetupProperties.Configuration.Runtime.ChunksNumber = (int)json.chunksNumber;
                    createDb.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = (ushort)json.defaultUserHttpPort;

                    createDb.SetupProperties.Configuration.Runtime.DumpDirectory = json.dumpDirectory;
                    createDb.SetupProperties.Configuration.Runtime.TempDirectory = json.tempDirectory;
                    createDb.SetupProperties.Configuration.Runtime.ImageDirectory = json.imageDirectory;
                    createDb.SetupProperties.Configuration.Runtime.TransactionLogDirectory = json.transactionLogDirectory;
                    createDb.SetupProperties.Configuration.Runtime.SchedulerCount = (int)json.schedulerCount;
                    createDb.SetupProperties.Configuration.Runtime.SqlAggregationSupport = (bool)json.sqlAggregationSupport;
                    createDb.SetupProperties.Configuration.Runtime.SQLProcessPort = (ushort)json.sqlProcessPort;

                    createDb.SetupProperties.StorageConfiguration.CollationFile = json.collationFile;
                    createDb.SetupProperties.StorageConfiguration.MaxImageSize = (long)json.maxImageSize;
                    createDb.SetupProperties.StorageConfiguration.SupportReplication = (bool)json.supportReplication;
                    createDb.SetupProperties.StorageConfiguration.TransactionLogSize = (long)json.transactionLogSize;

                    CommandInfo commandInfo = Master.ServerInterface.Execute(createDb);
                    resultJson.commandId = commandInfo.Id.Value;
                }
                catch (Exception e) {
                    resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                }
                return resultJson.ToString();

            });


            // Returns a database
            GET("/databases/{?}", (string databaseid, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    DatabaseInfo database = Master.ServerInterface.GetDatabase(Master.DecodeFrom64(databaseid));
                    if (database != null) {
                        DatabaseApp databaseApp = new DatabaseApp();
                        databaseApp.SetDatabaseInfo(database);
                        //Session.Data = databaseApp;
                        return databaseApp;
                    }
                    return HttpStatusCode.NotFound;
                }
                return HttpStatusCode.NotAcceptable;

            });

            #endregion

            #region Get Default Settings
            // Get default settings for a (type='database')
            GET("/settings/default/{?}", (string type, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    Starcounter.Configuration.DatabaseConfiguration d = new Starcounter.Configuration.DatabaseConfiguration();

                    if ("database".Equals(type, StringComparison.CurrentCultureIgnoreCase)) {
                        ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

                        dynamic json = new DynamicJson();
                        json.databaseName = "myDatabase"; // TODO: Generate a unique default database name
                        json.chunksNumber = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ChunksNumber;
                        json.defaultUserHttpPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DefaultUserHttpPort;

                        json.dumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory;
                        json.tempDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TempDirectory;
                        json.imageDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                        json.transactionLogDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TransactionLogDirectory;

                        json.schedulerCount = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SchedulerCount ?? 0;
                        json.sqlAggregationSupport = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SqlAggregationSupport;
                        json.sqlProcessPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SQLProcessPort;
                        json.collationFile = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.CollationFile;
                        json.maxImageSize = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.MaxImageSize ?? -1;
                        json.supportReplication = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.SupportReplication;
                        json.transactionLogSize = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.TransactionLogSize ?? -1;

                        return json.ToString();
                    }
                    else {
                        return HttpStatusCode.NotFound;
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });
            #endregion

            #region Log

            GET("/log?{?}", (string parameters, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(parameters);

                    LogApp logApp = new LogApp();

                    #region Set Filter
                    Boolean filter_debug;
                    Boolean.TryParse(collection["debug"], out filter_debug);
                    logApp.FilterDebug = filter_debug;

                    Boolean filter_notice;
                    Boolean.TryParse(collection["notice"], out filter_notice);
                    logApp.FilterNotice = filter_notice;

                    Boolean filter_warning;
                    Boolean.TryParse(collection["warning"], out filter_warning);
                    logApp.FilterWarning = filter_warning;

                    Boolean filter_error;
                    Boolean.TryParse(collection["error"], out filter_error);
                    logApp.FilterError = filter_error;
                    #endregion

                    logApp.RefreshLogEntriesList();
                    return logApp;
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }
            });

            // Returns the log
            GET("/log", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    LogApp logApp = new LogApp() { FilterDebug = false, FilterNotice = false, FilterWarning = true, FilterError = true };
                    logApp.RefreshLogEntriesList();
                    return logApp;
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }
            });

            #endregion

            #region SQL

            POST("/sql/{?}", (string databasename, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    try {
                        Response response;
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Node.LocalhostSystemPortNode.POST(
                            string.Format("/__{0}/sql", databasename),
                            bodyData,
                            "MyHeader1: 123\r\nMyHeader2: 456\r\n",
                            null,
                            out response);

                        if (response == null) {
                            return HttpStatusCode.ServiceUnavailable;
                        }
                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            return response.GetBodyStringUtf8_Slow();
                        }

                        return (int)response.StatusCode;
                    }
                    catch (Exception e) {
                        dynamic resultJson = new DynamicJson();
                        resultJson.columns = new object[] { };
                        resultJson.rows = new object[] { };
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                        resultJson.sqlException = null;
                        return resultJson.ToString();
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });

            #endregion

            #region Get Console output from database
            // Returns the log
            GET("/databases/{?}/console", (string databaseid, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.console = null;
                    resultJson.exception = null;

                    try {
                        Response response;
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Node.LocalhostSystemPortNode.GET(string.Format("/__{0}/console", databaseid), null, null, out response);

                        if (response == null) {
                            return HttpStatusCode.ServiceUnavailable;
                        }

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            // Success
                            return response.GetBodyStringUtf8_Slow();
                        }

                        return (int)response.StatusCode;
                    }
                    catch (Exception e) {
                        resultJson = new DynamicJson();
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                        return resultJson.ToString();
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });
            #endregion


            POST("/addstaticcontentdir", (Request req) => {

                // Getting POST contents.
                String content = req.GetBodyStringUtf8_Slow();

                // Splitting contents.
                String[] settings = content.Split(new String[] { StarcounterConstants.NetworkConstants.CRLF }, StringSplitOptions.RemoveEmptyEntries);

                try {
                    // Registering static handler on given port.
                    GET(UInt16.Parse(settings[0]), "/{?}", (string res) => {
                        return null;
                    });
                }
                catch (Exception exc) {
                    UInt32 errCode;

                    // Checking if this handler is already registered.
                    if (ErrorCode.TryGetCode(exc, out errCode)) {
                        if (Starcounter.Error.SCERRHANDLERALREADYREGISTERED == errCode)
                            return "Success!";
                    }
                    throw exc;
                }

                // Adding static files serving directory.
                AppsBootstrapper.AddFileServingDirectory(settings[1]);

                return "Success!";
            });

            GET("/return/{?}", (int code) => {
                return code;
            });

            GET("/returnstatus/{?}", (int code) => {
                return (System.Net.HttpStatusCode)code;
            });

            GET("/returnwithreason/{?}", (string codeAndReason) => {
                // Example input: 404ThisIsMyCustomReason
                var code = int.Parse(codeAndReason.Substring(0, 3));
                var reason = codeAndReason.Substring(3);
                return new HttpStatusCodeAndReason(code, reason);
            });

            GET("/test", () => {
                return "hello";
            });


        }

        #region ServerServices

        static void StartListeningService() {
            System.Threading.ThreadPool.QueueUserWorkItem(ServerServicesThread);
        }

        static private void ServerServicesThread(object state) {
            ServerServices services;
            string pipeName;
            pipeName = ScUriExtensions.MakeLocalServerPipeString(Master.ServerEngine.Name);
            var ipcServer = ClientServerFactory.CreateServerUsingNamedPipes(pipeName);
            ipcServer.ReceivedRequest += OnIPCServerReceivedRequest;
            services = new ServerServices(Master.ServerEngine, ipcServer);
            ToConsoleWithColor(string.Format("Accepting service calls on pipe '{0}'...", pipeName), ConsoleColor.DarkGray);

            services.Setup();
            // Start the engine and run the configured services.
            Master.ServerEngine.Run(services);

        }

        static void OnIPCServerReceivedRequest(object sender, string e) {
            ToConsoleWithColor(string.Format("Request: {0}", e), ConsoleColor.Yellow);
        }

        #endregion

        static public string EncodeTo64(string toEncode) {
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public string DecodeFrom64(string encodedData) {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }

        static void ToConsoleWithColor(string text, ConsoleColor color) {
            try {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            }
            finally {
                Console.ResetColor();
            }
        }


    }


}
