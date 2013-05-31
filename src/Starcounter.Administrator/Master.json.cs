
using Codeplex.Data;
using Starcounter;
using Starcounter.ABCIPC.Internal;
using Starcounter.Administrator;
using Starcounter.Administrator.API;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.REST;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest;
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

            // Start listening on log-events
            ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

            LogApp.Setup(serverInfo.Configuration.LogDirectory);

            // Register and setup the API subsystem handlers
            var admin = new AdminAPI();
            RestAPI.Bootstrap(admin, Dns.GetHostEntry(String.Empty).HostName, adminPort, Master.ServerEngine, Master.ServerInterface);

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
                // Doing another request with original request attached.
                Response resp = Node.LocalhostSystemPortNode.GET("/index.html", null, req);

                // Returns this response to original request.
                return resp;
            });

            Register_Administrator_Server_Handler();

            Register_Administrator_Databases_Handler();

            Register_Administrator_Applications_Handler();

            #region Log (/adminapi/v1/server/log)

            GET("/adminapi/v1/log?{?}", (string parameters, Request req) => {

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
            GET("/adminapi/v1/log", (Request req) => {

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

            #region TODO: Get Default Settings (/adminapi/v1/settings/default)
            GET("/adminapi/v1/settings/default/{?}", (string type, Request req) => {

                // Get default settings for a (type='database')
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
                        //json.sqlProcessPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SQLProcessPort;
                        json.collationFile = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.CollationFile;

                        json.collationFiles = new object[] { };
                        // TODO: Extend the Public model api to be able to retrive a list of all available collation files
                        json.collationFiles[0] = new { name = "TurboText_en-GB_3.dll", description = "English" };
                        json.collationFiles[1] = new { name = "TurboText_sv-SE_3.dll", description = "Swedish" };
                        json.collationFiles[2] = new { name = "TurboText_nb-NO_3.dll", description = "Norwegian" };


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

            #region TODO: SQL (/adminapi/v1/sql)

            POST("/adminapi/v1/sql/{?}", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    try {
                        ;

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Response resp = Node.LocalhostSystemPortNode.POST(
                            string.Format("/__{0}/sql", database.Name),
                            bodyData,
                            "MyHeader1: 123\r\nMyHeader2: 456\r\n",
                            null);

                        /*if (resp == null) {
                            dynamic errorJson = new DynamicJson();
                            errorJson.message = string.Format("Could not retrive the query result from the {0} database, Caused by a missing database or if the database is not running.", database.Name);
                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.ServiceUnavailable, null, errorJson.ToString()) };
                        }*/

                        if (resp.StatusCode >= 200 && resp.StatusCode < 300) {
                            return resp.GetBodyStringUtf8_Slow();
                        }

                        // TODO: Do not return error code. return a more user friendly message
                        return (int)resp.StatusCode;
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

            #region Debug/Test

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
            #endregion


        }

        static private void Register_Administrator_Server_Handler() {

            #region Get Server (/adminapi/v1/server)
            GET("/adminapi/v1/server", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {

                        ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                        if (serverInfo == null) {
                            throw new InvalidProgramException("Could not retrive server informaiton");
                        }

                        dynamic resultJson = new DynamicJson();

                        resultJson.server = new {
                            id = Master.EncodeTo64(serverInfo.Uri),
                            name = serverInfo.Configuration.Name,
                            httpPort = serverInfo.Configuration.SystemHttpPort,
                            version = CurrentVersion.Version
                        };

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                    }
                    catch (Exception e) {

                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                return HttpStatusCode.NotAcceptable;

            });
            #endregion

            #region Update settings (/adminapi/v1/server)
            PUT("/adminapi/v1/server", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {

                        dynamic resultJson = new DynamicJson();
                        resultJson.validationErrors = new object[] { };

                        String content = req.GetBodyStringUtf8_Slow();

                        dynamic incomingJson = DynamicJson.Parse(content);

                        ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                        if (serverInfo == null) {
                            throw new InvalidProgramException("Could not retrive server informaiton");
                        }

                        // Validate settings
                        int validationErrors = 0;

                        #region Validate incoming json data

                        // Port number
                        ushort port;
                        if (ushort.TryParse(incomingJson.httpPort.ToString(), out port) && port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort) {
                            serverInfo.Configuration.SystemHttpPort = port;
                        }
                        else {
                            resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
                        }
                        #endregion

                        if (validationErrors == 0) {
                            // Validation OK
                            resultJson.Delete("validationErrors"); // Cleanup, remove the validationErrors property from the resultJson (it's empty = no need for it)

                            serverInfo.Configuration.Save(serverInfo.ServerConfigurationPath);
                            resultJson.message = "Settings saved. The new settings will be used at the next start of the server";

                            // Get new database settings
                            serverInfo = Master.ServerInterface.GetServerInfo();
                            if (serverInfo == null) {
                                throw new InvalidProgramException("Could not retrive server informaiton");
                            }

                            resultJson.server = new {
                                id = Master.EncodeTo64(serverInfo.Uri),
                                name = serverInfo.Configuration.Name,
                                httpPort = serverInfo.Configuration.SystemHttpPort,
                                version = CurrentVersion.Version
                            };

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                        }
                        else {
                            // Validation Errors
                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Forbidden, null, resultJson.ToString()) };
                        }

                    }
                    catch (Exception e) {
                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                return HttpStatusCode.NotAcceptable;

            });
            #endregion

            #region Get Commands (/adminapi/v1/server/commands) - Get a list of all ongoing database commands
            GET("/adminapi/v1/server/commands", (Request req) => {
                lock (Master.ServerInterface) {

                    try {

                        dynamic resultJson = new DynamicJson();
                        resultJson.commands = new object[] { };

                        CommandInfo[] commandInfos = Master.ServerInterface.GetCommands();
                        for (int i = 0; i < commandInfos.Length; i++) {

                            CommandInfo command = commandInfos[i];

                            if (command.IsCompleted) continue;
                            if (command.IsDatabaseActivity == false) continue;

                            DatabaseInfo database = Master.ServerInterface.GetDatabase(command.DatabaseUri);
                            if (database == null) continue;

                            // The reason we also send the HostProcessId it to let the client know it the database process is running or not.
                            var hostProcId = database.Engine == null ? 0 : database.Engine.HostProcessId;
                            resultJson.commands[i] = new { description = command.Description, name = database.Name, hostProcessId = hostProcId, status = command.Status.ToString() };
                            resultJson.commands[i].errors = new object[] { };
                            resultJson.commands[i].progressText = null;

                            if (command.HasProgress) {
                                foreach (ProgressInfo progressInfo in command.Progress) {
                                    if (!progressInfo.IsCompleted && !string.IsNullOrEmpty(progressInfo.Text)) {
                                        if (!string.IsNullOrEmpty(resultJson.commands[i].progressText)) {
                                            resultJson.commands[i].progressText += ", ";
                                        }

                                        resultJson.commands[i].progressText += progressInfo.Text;
                                    }
                                }
                            }

                            if (command.HasError) {
                                for (int n = 0; n < command.Errors.Length; n++) {
                                    ErrorInfo errorInfo = command.Errors[n];
                                    ErrorMessage errorMessage = errorInfo.ToErrorMessage();
                                    resultJson.commands[i].errors[n] = new { message = errorMessage.Message, helpLink = errorMessage.Helplink };
                                }
                            }
                        }

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                    }
                    catch (Exception e) {
                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };

                    }
                }
            });
            #endregion

            #region Get Command (/adminapi/v1/server/commands)
            GET("/adminapi/v1/server/commands/{?}", (string commandId, Request req) => {

                lock (Master.ServerInterface) {

                    try {

                        dynamic resultJson = new DynamicJson();

                        // Get command
                        CommandInfo[] commandInfos = Master.ServerInterface.GetCommands();
                        foreach (CommandInfo command in commandInfos) {
                            if (command.Id.Value == commandId) {

                                resultJson.command = new { };

                                resultJson.command.isCompleted = command.IsCompleted;
                                resultJson.command.message = command.Description;
                                resultJson.command.status = command.Status.ToString();
                                resultJson.command.progressText = null;
                                resultJson.command.errors = new object[] { };

                                if (command.HasProgress) {
                                    foreach (ProgressInfo progressInfo in command.Progress) {
                                        if (!progressInfo.IsCompleted) {
                                            resultJson.command.progressText = progressInfo.Text;
                                        }
                                    }
                                }

                                if (command.HasError) {
                                    int index = 0;

                                    foreach (ErrorInfo eInfo in command.Errors) {
                                        ErrorMessage emsg = eInfo.ToErrorMessage();
                                        resultJson.command.errors[index] = new { message = emsg.Message, helpLink = emsg.Helplink };
                                        index++;
                                    }
                                }
                                break;
                            }
                        }

                        if (resultJson.command == null) {
                            // command not found

                            dynamic errorJson = new DynamicJson();

                            errorJson.message = string.Format("Could not find the command with id {0}", commandId);
                            errorJson.code = (int)HttpStatusCode.NotFound;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                        }

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                    }
                    catch (Exception e) {
                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
            });

            #endregion

        }

        static private void Register_Administrator_Databases_Handler() {

            #region Get databases (/adminapi/v1/databases)
            GET("/adminapi/v1/databases", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {

                        dynamic resultJson = new DynamicJson();

                        resultJson.databases = new object[] { };

                        DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                        for (int i = 0; i < databases.Length; i++) {
                            resultJson.databases[i] = new {
                                id = Master.EncodeTo64(databases[i].Uri),
                                status = (databases[i].Engine != null) ? "Running" : ".",
                                name = databases[i].Name,
                                hostProcessId = databases[i].Engine == null ? 0 : databases[i].Engine.HostProcessId,
                                httpPort = databases[i].Configuration.Runtime.DefaultUserHttpPort,
                                schedulerCount = databases[i].Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                chunksNumber = databases[i].Configuration.Runtime.ChunksNumber,
                                //sqlProcessPort = databases[i].Configuration.Runtime.SQLProcessPort,
                                sqlAggregationSupport = databases[i].Configuration.Runtime.SqlAggregationSupport
                            };
                        }

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                    }
                    catch (Exception e) {
                        dynamic exceptionJson = new DynamicJson();
                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });
            #endregion

            #region Get database (/adminapi/v1/databases/{?})
            GET("/adminapi/v1/databases/{?}", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);
                        if (database == null) {
                            // Database not found

                            dynamic errorJson = new DynamicJson();

                            errorJson.message = string.Format("Could not find the {0} database", name);
                            errorJson.code = (int)HttpStatusCode.NotFound;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                        }
                        else {

                            dynamic resultJson = new DynamicJson();

                            resultJson.database = new {
                                id = Master.EncodeTo64(database.Uri),
                                status =  (database.Engine != null) ? "Running" : ".",
                                name = database.Name,
                                hostProcessId = database.Engine == null ? 0 : database.Engine.HostProcessId,
                                httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                                schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                chunksNumber = database.Configuration.Runtime.ChunksNumber,
                                //sqlProcessPort = database.Configuration.Runtime.SQLProcessPort,
                                sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport

                            };

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                        }
                    }
                    catch (Exception e) {

                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                return HttpStatusCode.NotAcceptable;
            });
            #endregion

            #region Create a database (/adminapi/v1/databases)
            POST("/adminapi/v1/databases", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {
                        dynamic resultJson = new DynamicJson();
                        resultJson.commandId = null;
                        resultJson.validationErrors = new object[] { };

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
                        //ushort sqlProcessPort;
                        //if (!ushort.TryParse(incomingJson.sqlProcessPort.ToString(), out sqlProcessPort) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                        //    resultJson.validationErrors[validationErrors++] = new { property = "sqlProcessPort", message = "invalid port number" };
                        //}

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
                            resultJson.Delete("validationErrors"); // Cleanup, remove the validationErrors property from the resultJson (it's empty = no need for it)

                            var command = new CreateDatabaseCommand(Master.ServerEngine, incomingJson.name);
                            command.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = port;
                            command.SetupProperties.Configuration.Runtime.SchedulerCount = schedulerCount;
                            command.SetupProperties.Configuration.Runtime.ChunksNumber = chunksNumber;

                            command.SetupProperties.Configuration.Runtime.DumpDirectory = incomingJson.dumpDirectory;
                            command.SetupProperties.Configuration.Runtime.TempDirectory = incomingJson.tempDirectory;
                            command.SetupProperties.Configuration.Runtime.ImageDirectory = incomingJson.imageDirectory;
                            command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = incomingJson.transactionLogDirectory;

                            command.SetupProperties.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
                            //command.SetupProperties.Configuration.Runtime.SQLProcessPort = sqlProcessPort;

                            command.SetupProperties.StorageConfiguration.CollationFile = incomingJson.collationFile;

                            command.SetupProperties.StorageConfiguration.MaxImageSize = maxImageSize;
                            command.SetupProperties.StorageConfiguration.SupportReplication = supportReplication;
                            command.SetupProperties.StorageConfiguration.TransactionLogSize = transactionLogSize;

                            CommandInfo commandInfo = Master.ServerInterface.Execute(command);
                            resultJson.commandId = commandInfo.Id.Value;

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Accepted, null, resultJson.ToString()) };
                        }
                        else {
                            // Validation errors
                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Forbidden, null, resultJson.ToString()) };
                        }

                    }
                    catch (Exception e) {
                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }

                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });
            #endregion

            #region Update settings (/adminapi/v1/databases/{?})
            PUT("/adminapi/v1/databases/{?}", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {

                        String content = req.GetBodyStringUtf8_Slow();

                        dynamic incomingJson = DynamicJson.Parse(content);

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                        if (database == null) {
                            // Database not found

                            dynamic errorJson = new DynamicJson();

                            errorJson.message = string.Format("Could not find the {0} database", name);
                            errorJson.code = (int)HttpStatusCode.NotFound;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                        }
                        else {

                            dynamic resultJson = new DynamicJson();
                            resultJson.validationErrors = new object[] { };

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
                            //ushort sqlProcessPort;
                            //if (!ushort.TryParse(incomingJson.sqlProcessPort.ToString(), out sqlProcessPort) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                            //    resultJson.validationErrors[validationErrors++] = new { property = "sqlProcessPort", message = "invalid port number" };
                            //}

                            #endregion

                            if (validationErrors == 0) {
                                // Validation OK
                                resultJson.Delete("validationErrors"); // Cleanup, remove the validationErrors property from the resultJson (it's empty = no need for it)

                                database.Configuration.Runtime.DefaultUserHttpPort = port;
                                database.Configuration.Runtime.SchedulerCount = schedulerCount;
                                database.Configuration.Runtime.ChunksNumber = chunksNumber;
                                database.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
                                //database.Configuration.Runtime.SQLProcessPort = sqlProcessPort;

                                database.Configuration.Save();
                                resultJson.message = "Settings saved. The new settings will be used at the next start of the database";

                                // Get new database with the new settings
                                database = Master.ServerInterface.GetDatabaseByName(database.Name);
                                if (database == null) {
                                    // Database not found

                                    dynamic errorJson = new DynamicJson();

                                    errorJson.message = string.Format("Could not find the {0} database", name);
                                    errorJson.code = (int)HttpStatusCode.NotFound;
                                    errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                                    return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                                }
                                else {

                                    // Return the database
                                    resultJson.database = new {
                                        id = Master.EncodeTo64(database.Uri),
                                        name = database.Name,
                                        //status = database.HostProcessId,
                                        hostProcessId = database.Engine == null ? 0 : database.Engine.HostProcessId,
                                        httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                                        schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                        chunksNumber = database.Configuration.Runtime.ChunksNumber,
                                        //sqlProcessPort = database.Configuration.Runtime.SQLProcessPort,
                                        sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport
                                    };

                                    return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                                }
                            }
                            else {
                                // Validation Errors
                                return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Forbidden, null, resultJson.ToString()) };
                            }
                        }
                    }
                    catch (Exception e) {

                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                return HttpStatusCode.NotAcceptable;

            });
            #endregion

            #region Get Console output from database (/adminapi/v1/databases/{?}/console)
            GET("/adminapi/v1/databases/{?}/console", (string name, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.console = null;
                    resultJson.exception = null;

                    try {
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the message

                        Response response = Node.LocalhostSystemPortNode.GET(string.Format("/__{0}/console", name), null, null);

                        if (response == null) {

                            dynamic errorJson = new DynamicJson();

                            errorJson.message = string.Format("Could not connect to the {0} database", name);
                            errorJson.code = (int)HttpStatusCode.NotFound;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                        }

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            // Success
                            return response.GetBodyStringUtf8_Slow();
                        }
                        else {
                            // Error
                            dynamic errorJson = new DynamicJson();
                            if (string.IsNullOrEmpty(bodyData)) {
                                errorJson.message = string.Format("Could not retrive the console output from the {0} database, Caused by a missing database or if the database is not running.", name);
                            }
                            else {
                                errorJson.message = bodyData;
                            }
                            errorJson.code = (int)response.StatusCode;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)response.StatusCode, null, errorJson.ToString()) };

                        }

                    }
                    catch (Exception e) {

                        dynamic exceptionJson = new DynamicJson();

                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });
            #endregion

            #region TODO: Execute action on a database (/adminapi/v1/databases/{?}?{?})
            POST("/adminapi/v1/databases/{?}?{?}", (string name, string parameters, Request req) => {

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
            #endregion

        }

        static private void Register_Administrator_Applications_Handler() {

            #region Get 'running' Applications (/adminapi/v1/apps)

            GET("/adminapi/v1/apps", (Request req) => {
                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    try {

                        dynamic resultJson = new DynamicJson();
                        resultJson.apps = new object[] { };

                        DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                        foreach (var database in databases) {
                            var engineState = database.Engine;
                            var appsState = engineState == null ? null : engineState.HostedApps;
                            for (int i = 0; appsState != null && i < appsState.Length; i++) {

                                resultJson.apps[i] = new {
                                    status = "Running", // TODO: Use an id
                                    name = Path.GetFileNameWithoutExtension(appsState[i].ExecutablePath),
                                    path = appsState[i].ExecutablePath,
                                    folder = appsState[i].WorkingDirectory,
                                    databaseName = database.Name,
                                    databaseID = Master.EncodeTo64(database.Uri)
                                };
                            }
                        }

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                    }
                    catch (Exception e) {

                        dynamic exceptionJson = new DynamicJson();
                        exceptionJson.message = e.Message;
                        exceptionJson.helpLink = e.HelpLink;
                        exceptionJson.stackTrace = e.StackTrace;
                        exceptionJson.code = (int)HttpStatusCode.InternalServerError;

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });


            #endregion

        }

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
    }


}
