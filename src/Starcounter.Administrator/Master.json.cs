
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
            UInt16 adminPort = NewConfig.Default.SystemHttpPort;
            Console.WriteLine("Starcounter Administrator started on port: " + adminPort);

#if ANDWAH
            AppsBootstrapper.Bootstrap(@"c:\github\Level1\src\Starcounter.Administrator", adminPort);   // TODO:REMOVE
#else
            AppsBootstrapper.Bootstrap("scadmin", adminPort);
#endif

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

            // Registering Administrator handlers.
            RegisterHandlers();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sytemHttpPort">Port for doing SQL queries</param>
        static void RegisterHandlers() {

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
            GET("/server", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.server = null;
                    resultJson.exception = null;

                    try {
                        ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

                        if (serverInfo != null) {
                            resultJson.server = new {
                                id = Master.EncodeTo64(serverInfo.Uri),
                                name = serverInfo.Configuration.Name,
                                httpPort = serverInfo.Configuration.SystemHttpPort
                            };
                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
                }
                return HttpStatusCode.NotAcceptable;


                //ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                //ServerApp serverApp = new ServerApp();
                //serverApp.SystemHttpPort = serverInfo.Configuration.SystemHttpPort;
                //serverApp.DatabaseDirectory = serverInfo.Configuration.DatabaseDirectory;
                //serverApp.LogDirectory = serverInfo.Configuration.LogDirectory;
                //serverApp.TempDirectory = serverInfo.Configuration.TempDirectory;
                //serverApp.ServerName = serverInfo.Configuration.Name;

                //return serverApp;
            });


            PUT("/server", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.server = null;
                    resultJson.exception = null;
                    resultJson.message = null;
                    resultJson.validationErrors = new object[] { };

                    try {

                        String content = req.GetBodyStringUtf8_Slow();

                        dynamic incomingJson = DynamicJson.Parse(content);

                        ServerInfo server = Master.ServerInterface.GetServerInfo();

                        if (server != null) {

                            // Validate settings
                            int validationErrors = 0;

                            // Port number
                            ushort port;
                            if (ushort.TryParse(incomingJson.httpPort.ToString(), out port) && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort) {
                                server.Configuration.SystemHttpPort = port;
                            }
                            else {
                                resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
                            }

                            if (validationErrors == 0) {
                                // Validation OK
                                server.Configuration.Save(server.ServerConfigurationPath);
                                resultJson.message = "Settings saved. The new settings will be used at the next start of the server";

                                // Get new database settings
                                server = Master.ServerInterface.GetServerInfo();

                                if (server != null) {
                                    resultJson.server = new {
                                        id = Master.EncodeTo64(server.Uri),
                                        name = server.Configuration.Name,
                                        httpPort = server.Configuration.SystemHttpPort
                                    };
                                }

                            }
                            else {
                                // Validation Errors
                            }

                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
                }
                return HttpStatusCode.NotAcceptable;

            });

            #endregion

            #region Command Status
            GET("/command/{?}", (string commandId, Request req) => {

                lock (Master.ServerInterface) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.isCompleted = true;
                    resultJson.exception = null;
                    resultJson.message = null;
                    resultJson.progressText = "";
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

                                if (command.HasProgress) {
                                    foreach (ProgressInfo progressInfo in command.Progress) {
                                        if (!progressInfo.IsCompleted) {
                                            resultJson.progressText = progressInfo.Text;
                                        }
                                    }
                                }

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

            #region Application(s)

            // Returns a list of databases
            GET("/apps", (Request req) => {

                dynamic resultJson = new DynamicJson();
                resultJson.apps = new object[] { };
                resultJson.exception = null;

                try {

                    if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                        DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                        foreach (var database in databases) {
                            for (int i = 0; i < database.HostedApps.Length; i++) {

                                resultJson.apps[i] = new {
                                    status = "Running",
                                    name = Path.GetFileNameWithoutExtension(database.HostedApps[i].ExecutablePath),
                                    path = database.HostedApps[i].ExecutablePath,
                                    folder = database.HostedApps[i].WorkingDirectory,
                                    databaseName = database.Name,
                                    databaseID = Master.EncodeTo64(database.Uri)
                                };
                            }
                        }
                    }
                    else {
                        return HttpStatusCode.NotAcceptable;
                    }
                }
                catch (Exception e) {
                    resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                }

                return resultJson.ToString();

            });


            #endregion

            #region Database(s)

            // Returns a list of databases
            GET("/databases", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.databases = new object[] { };
                    resultJson.exception = null;

                    try {
                        DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                        for (int i = 0; i < databases.Length; i++) {
                            resultJson.databases[i] = new {
                                id = Master.EncodeTo64(databases[i].Uri),
                                name = databases[i].Name,
                                status = databases[i].HostProcessId,
                                httpPort = databases[i].Configuration.Runtime.DefaultUserHttpPort,
                                schedulerCount = databases[i].Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                chunksNumber = databases[i].Configuration.Runtime.ChunksNumber,
                                sqlProcessPort = databases[i].Configuration.Runtime.SQLProcessPort,
                                sqlAggregationSupport = databases[i].Configuration.Runtime.SqlAggregationSupport
                            };
                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });


            //  TODO: This should be "/databases/{?}?{?}"
            POST("/a/{?}?{?}", (string name, string parameters, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    dynamic resultJson = new DynamicJson();
                    resultJson.commandId = null;
                    resultJson.exception = null;

                    try {

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);
                        // TODO: Check for null value of database
                        NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(parameters);
                        ServerCommand command = null;
                        switch (collection["action"]) {
                            case "start":
                                command = new StartDatabaseCommand(Master.ServerEngine, database.Name);
                                break;
                            case "stop":
                                command = new StopDatabaseCommand(Master.ServerEngine, database.Name);
                                break;
                            default:
                                // Unknown command
                                break;
                        }

                        if (command != null) {
                            CommandInfo commandInfo = Master.ServerInterface.Execute(command);
                            resultJson.commandId = commandInfo.Id.Value;
                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }
            });

            // Stop   /database/mydatabas?action=stop
            //POST("/databases/{?}?action=stop", (string id, Request req) => {


            //    if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

            //        dynamic resultJson = new DynamicJson();
            //        resultJson.commandId = null;
            //        resultJson.exception = null;

            //        try {

            //            DatabaseInfo database = Master.ServerInterface.GetDatabase(Master.DecodeFrom64(id));
            //            // TODO: Check for null value of database

            //            StopDatabaseCommand command = new StopDatabaseCommand(Master.ServerEngine, database.Name);
            //            CommandInfo commandInfo = Master.ServerInterface.Execute(command);
            //            resultJson.commandId = commandInfo.Id.Value;
            //        }
            //        catch (Exception e) {
            //            resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
            //        }
            //        return resultJson.ToString();
            //    }
            //    else {
            //        return HttpStatusCode.NotAcceptable;
            //    }
            //});


            // Returns a database
            GET("/databases/{?}", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.database = null;
                    resultJson.exception = null;

                    try {
                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);
                        if (database != null) {
                            resultJson.database = new {
                                id = Master.EncodeTo64(database.Uri),
                                name = database.Name,
                                status = database.HostProcessId,
                                httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                                schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                chunksNumber = database.Configuration.Runtime.ChunksNumber,
                                sqlProcessPort = database.Configuration.Runtime.SQLProcessPort,
                                sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport
                            };
                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
                }
                return HttpStatusCode.NotAcceptable;
            });

            // Create a database
            POST("/databases/{?}", (string name, Request req) => {

                dynamic resultJson = new DynamicJson();
                resultJson.commandId = null;
                resultJson.exception = null;
                resultJson.validationErrors = new object[] { };

                try {
                    // Validate settings
                    int validationErrors = 0;

                    // Getting POST contents.
                    String content = req.GetBodyStringUtf8_Slow();

                    var incomingJson = DynamicJson.Parse(content);

                    #region Validate incoming json data
                    // Database name
                    if (string.IsNullOrEmpty(incomingJson.name)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "name", message = "invalid database name" };
                    }

                    // Port number
                    ushort port;
                    if (!ushort.TryParse(incomingJson.httpPort.ToString(), out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                        resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
                    }

                    // Scheduler Count
                    int schedulerCount;
                    if (!int.TryParse(incomingJson.schedulerCount.ToString(), out schedulerCount)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "schedulerCount", message = "invalid scheduler count" };
                    }

                    // Chunks Number
                    int chunksNumber;
                    if (!int.TryParse(incomingJson.chunksNumber.ToString(), out chunksNumber)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "chunksNumber", message = "invalid chunks number" };
                    }

                    // Dump Directory
                    if (string.IsNullOrEmpty(incomingJson.dumpDirectory)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "dumpDirectory", message = "invalid dump directory" };
                    }

                    // Temp Directory
                    if (string.IsNullOrEmpty(incomingJson.tempDirectory)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "tempDirectory", message = "invalid temp directory" };
                    }

                    // Image Directory
                    if (string.IsNullOrEmpty(incomingJson.imageDirectory)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "imageDirectory", message = "invalid image directory" };
                    }

                    // Log Directory
                    if (string.IsNullOrEmpty(incomingJson.transactionLogDirectory)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "transactionLogDirectory", message = "invalid transaction log directory" };
                    }

                    // SQL Aggregation support
                    bool sqlAggregationSupport;
                    if (!bool.TryParse(incomingJson.sqlAggregationSupport.ToString(), out sqlAggregationSupport)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "sqlAggregationSupport", message = "invalid SQL Aggregation support" };
                    }

                    // sqlProcessPort
                    ushort sqlProcessPort;
                    if (!ushort.TryParse(incomingJson.sqlProcessPort.ToString(), out sqlProcessPort) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                        resultJson.validationErrors[validationErrors++] = new { property = "sqlProcessPort", message = "invalid port number" };
                    }

                    // Collation File
                    if (string.IsNullOrEmpty(incomingJson.collationFile)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "collationFile", message = "invalid collation file" };
                    }

                    // maxImageSize
                    long maxImageSize;
                    if (!long.TryParse(incomingJson.maxImageSize.ToString(), out maxImageSize)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "maxImageSize", message = "invalid max image size" };
                    }

                    // supportReplication
                    bool supportReplication;
                    if (!bool.TryParse(incomingJson.supportReplication.ToString(), out supportReplication)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "supportReplication", message = "invalid support replication" };
                    }

                    // transactionLogSize
                    long transactionLogSize;
                    if (!long.TryParse(incomingJson.transactionLogSize.ToString(), out transactionLogSize)) {
                        resultJson.validationErrors[validationErrors++] = new { property = "transactionLogSize", message = "invalid transaction log size" };
                    }

                    #endregion

                    if (validationErrors == 0) {

                        var command = new CreateDatabaseCommand(Master.ServerEngine, incomingJson.name);
                        command.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = port;
                        command.SetupProperties.Configuration.Runtime.SchedulerCount = schedulerCount;
                        command.SetupProperties.Configuration.Runtime.ChunksNumber = chunksNumber;

                        command.SetupProperties.Configuration.Runtime.DumpDirectory = incomingJson.dumpDirectory;
                        command.SetupProperties.Configuration.Runtime.TempDirectory = incomingJson.tempDirectory;
                        command.SetupProperties.Configuration.Runtime.ImageDirectory = incomingJson.imageDirectory;
                        command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = incomingJson.transactionLogDirectory;

                        command.SetupProperties.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
                        command.SetupProperties.Configuration.Runtime.SQLProcessPort = sqlProcessPort;

                        command.SetupProperties.StorageConfiguration.CollationFile = incomingJson.collationFile;

                        command.SetupProperties.StorageConfiguration.MaxImageSize = maxImageSize;
                        command.SetupProperties.StorageConfiguration.SupportReplication = supportReplication;
                        command.SetupProperties.StorageConfiguration.TransactionLogSize = transactionLogSize;

                        CommandInfo commandInfo = Master.ServerInterface.Execute(command);
                        resultJson.commandId = commandInfo.Id.Value;
                    }
                }
                catch (Exception e) {
                    resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                }
                return resultJson.ToString();

            });


            PUT("/databases/{?}", (string name, Request req) => {

                // Update settings
                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.database = null;
                    resultJson.exception = null;
                    resultJson.message = null;
                    resultJson.validationErrors = new object[] { };

                    try {

                        String content = req.GetBodyStringUtf8_Slow();

                        dynamic incomingJson = DynamicJson.Parse(content);

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);


                        if (database != null) {

                            // Validate settings
                            int validationErrors = 0;

                            #region Validate incoming json data

                            // Port number
                            ushort port;
                            if (!ushort.TryParse(incomingJson.httpPort.ToString(), out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                                resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
                            }

                            // Scheduler Count
                            int schedulerCount;
                            if (!int.TryParse(incomingJson.schedulerCount.ToString(), out schedulerCount)) {
                                resultJson.validationErrors[validationErrors++] = new { property = "schedulerCount", message = "invalid scheduler count" };
                            }

                            // Chunks Number
                            int chunksNumber;
                            if (!int.TryParse(incomingJson.chunksNumber.ToString(), out chunksNumber)) {
                                resultJson.validationErrors[validationErrors++] = new { property = "chunksNumber", message = "invalid chunks number" };
                            }

                            // SQL Aggregation support
                            bool sqlAggregationSupport;
                            if (!bool.TryParse(incomingJson.sqlAggregationSupport.ToString(), out sqlAggregationSupport)) {
                                resultJson.validationErrors[validationErrors++] = new { property = "sqlAggregationSupport", message = "invalid SQL Aggregation support" };
                            }

                            // sqlProcessPort
                            ushort sqlProcessPort;
                            if (!ushort.TryParse(incomingJson.sqlProcessPort.ToString(), out sqlProcessPort) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                                resultJson.validationErrors[validationErrors++] = new { property = "sqlProcessPort", message = "invalid port number" };
                            }

                            #endregion

                            if (validationErrors == 0) {
                                // Validation OK

                                database.Configuration.Runtime.DefaultUserHttpPort = port;
                                database.Configuration.Runtime.SchedulerCount = schedulerCount;
                                database.Configuration.Runtime.ChunksNumber = chunksNumber;
                                database.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
                                database.Configuration.Runtime.SQLProcessPort = sqlProcessPort;

                                database.Configuration.Save();
                                resultJson.message = "Settings saved. The new settings will be used at the next start of the database";

                                // Get new database settings
                                database = Master.ServerInterface.GetDatabaseByName(database.Name);

                                // Return the database
                                if (database != null) {
                                    resultJson.database = new {
                                        id = Master.EncodeTo64(database.Uri),
                                        name = database.Name,
                                        status = database.HostProcessId,
                                        httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                                        schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                        chunksNumber = database.Configuration.Runtime.ChunksNumber,
                                        sqlProcessPort = database.Configuration.Runtime.SQLProcessPort,
                                        sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport
                                    };
                                }

                            }
                            else {
                                // Validation Errors
                            }

                        }
                    }
                    catch (Exception e) {
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                    }
                    return resultJson.ToString();
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
                        json.name = "myDatabase"; // TODO: Generate a unique default database name
                        json.httpPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DefaultUserHttpPort;
                        json.schedulerCount = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SchedulerCount ?? Environment.ProcessorCount;

                        json.chunksNumber = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ChunksNumber;

                        // TODO: this is a workaround to get the default dumpdirectory path (fix this in the public model api)
                        if (string.IsNullOrEmpty(serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory)) {
                            //  By default, dump files are stored in ImageDirectory
                            json.dumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                        }
                        else {
                            json.dumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory;
                        }

                        json.tempDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TempDirectory;
                        json.imageDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                        json.transactionLogDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TransactionLogDirectory;

                        json.sqlAggregationSupport = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SqlAggregationSupport;
                        json.sqlProcessPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SQLProcessPort;
                        json.collationFile = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.CollationFile;

                        json.collationFiles = new object[] { };
                        // TODO: Extend the Public model api to be able to retrive a list of all available collation files
                        json.collationFiles[0] = new { name = "TurboText_en-GB_2.dll", description = "English" };
                        json.collationFiles[1] = new { name = "TurboText_sv-SE_2.dll", description = "Swedish" };
                        json.collationFiles[2] = new { name = "TurboText_nb-NO_2.dll", description = "Norwegian" };


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

            POST("/sql/{?}", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    try {
                        Response response;

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Node.LocalhostSystemPortNode.POST(
                            string.Format("/__{0}/sql", database.Name),
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

                        // TODO: Do not return error code. return a more user friendly message
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
            GET("/databases/{?}/console", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.console = null;
                    resultJson.exception = null;

                    try {
                        Response response;
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Node.LocalhostSystemPortNode.GET(string.Format("/__{0}/console", name), null, null, out response);

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

                // Getting port of the resource.
                UInt16 port = UInt16.Parse(settings[0]);

                // Adding static files serving directory.
                AppsBootstrapper.AddFileServingDirectory(port, settings[1]);

                try {
                    // Registering static handler on given port.
                    GET(port, "/{?}", (string res) => {
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
