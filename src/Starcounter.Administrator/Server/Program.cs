
using System;
using System.IO;
using System.Net;
using Starcounter.Administrator.API;
using Starcounter.Administrator.Server.Handlers;
using Starcounter.Internal;
using Starcounter.Internal.REST;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;
using Starcounter.Internal.Web;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Starcounter.Hosting;
using Administrator.Server.Managers;

namespace Starcounter.Administrator.Server {


    /// <summary>
    /// 
    /// </summary>
    public class Program {

        public static IServerRuntime ServerInterface;
        public static ServerEngine ServerEngine;

        public static string ResourceFolder;

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
            UInt16 adminPort = StarcounterEnvironment.Default.SystemHttpPort;
            Console.WriteLine("Starcounter Administrator started on port: " + adminPort);

#if ANDWAH
            Program.ResourceFolder = @"c:\github\Level1\src\Starcounter.Administrator";
#else
            Program.ResourceFolder = "scadmin";
#endif

            // Create a Server Engine
            Program.ServerEngine = new ServerEngine(args[0]);      // .srv\Personal\Personal.server.config
            Program.ServerEngine.Setup();
            Program.ServerInterface = Program.ServerEngine.Start();

            // Start listening on log-events
            ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();
            LogApp.Setup(serverInfo.Configuration.LogDirectory);

            // Register and setup the API subsystem handlers
            var admin = new AdminAPI();
            RestAPI.Bootstrap(admin, Dns.GetHostEntry(String.Empty).HostName, adminPort, Program.ServerEngine, Program.ServerInterface);

            ServerManager.Init();

            // Registering Default handlers.
            RegisterHandlers();

            // Bootstrapping the application.
            AppsBootstrapper.Bootstrap(
                StarcounterEnvironment.Default.SystemHttpPort,
                Program.ResourceFolder,
                StarcounterEnvironment.AppName);

            // Bootstrap Admin API handlers
            StarcounterAdminAPI.Bootstrap(adminPort, Program.ServerEngine, Program.ServerInterface, Program.ResourceFolder);

            // Start User Tracking (Send data to tracking server each hour and crash reports)
            if (serverInfo.Configuration.SendUsageAndCrashReports) {
                Tracking.Client.Instance.StartTrackUsage(Program.ServerInterface, Program.ServerEngine.HostLog);
            }
        }

        static Int32 WsEchoesCounter = 0;
        static Int32 WsDisconnectsCounter = 0;
        static Int32 WsHandshakesCounter = 0;
        static Int32 HttpEchoesCounter = 0;

        /// <summary>
        /// Register default handlers
        /// </summary>
        static void RegisterHandlers() {

            // Redirecting root to index.html.
            Handle.GET("/", () => {
                // Returns this response to original request.
                return Node.LocalhostSystemPortNode.GET("/index.html", null);
            });

            // Register a static resource folder
            Handle.POST(StarcounterConstants.StaticFilesDirRegistrationUri, (Request req) => {

                // Getting POST contents.
                String content = req.Body;

                // Splitting contents.
                String[] settings = content.Split(new String[] { StarcounterConstants.NetworkConstants.CRLF }, StringSplitOptions.RemoveEmptyEntries);

                // Application name.
                String appName = settings[0];

                // Is Polyjuice app directory.
                Boolean isPolyjuiceApp = Boolean.Parse(settings[1]);

                // Getting port of the resource.
                UInt16 port = UInt16.Parse(settings[2]);

                // Static file path.
                String path = settings[3];

                // Adding static files serving directory.
                AppsBootstrapper.InternalAddStaticFileDirectory(port, path, appName);

                return "Success!";
            });

            // Handler to get all registered static resource folders
            Handle.GET("/staticcontentdir", (Request req) => {

                Dictionary<UInt16, IList<string>> folders = AppsBootstrapper.GetFileServingDirectories();

                WorkingFolders workingFolders = new WorkingFolders();

                foreach (KeyValuePair<UInt16, IList<string>> entry in folders) {

                    if (entry.Value != null && entry.Value.Count > 0) {
                        foreach (string folder in entry.Value) {
                            var folderJson = workingFolders.Items.Add();
                            folderJson.Port = entry.Key;
                            folderJson.Folder = folder;
                        }
                    }
                }

                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = workingFolders.ToJsonUtf8() };
            });

            #region Debug/Test

            Handle.GET("/return/{?}", (int code) => {
                return code;
            });

            Handle.GET("/returnstatus/{?}", (int code) => {
                return (System.Net.HttpStatusCode)code;
            });

            Handle.GET("/returnwithreason/{?}", (string codeAndReason) => {
                // Example input: 404ThisIsMyCustomReason
                var code = int.Parse(codeAndReason.Substring(0, 3));
                var reason = codeAndReason.Substring(3);
                return new HttpStatusCodeAndReason(code, reason);
            });

            Handle.GET("/test", () => {

                Response resp = new Response();
                resp.Body = "hello";
                resp.ContentType = "text/plain";

                return resp;
            });

            Handle.GET("/delay", () => {

                Thread.Sleep(250);

                return 204;
            });

            Handle.GET("/native-allocs", () => {
                return "Number of native allocations: " + BitsAndBytes.NumNativeAllocations;
            });

            Handle.GET("/echotestws", (Request req) => {

                // Checking for client IP address.
                String clientIp = req.ClientIpAddress.ToString();
                if (clientIp != "127.0.0.1")
                    throw new Exception("Wrong client IP address: " + clientIp);

                if (req.WebSocketUpgrade) {
                    Interlocked.Increment(ref WsHandshakesCounter);

                    req.SendUpgrade("echotestws");

                    return HandlerStatus.Handled;
                }

                return 513;
            });

            Handle.GET("/wscounters", (Request req) => {

                // Checking for client IP address.
                String clientIp = req.ClientIpAddress.ToString();
                if (clientIp != "127.0.0.1")
                    throw new Exception("Wrong client IP address: " + clientIp);

                Int32 e = WsEchoesCounter,
                    d = WsDisconnectsCounter,
                    h = WsHandshakesCounter;

                WsEchoesCounter = 0;
                WsDisconnectsCounter = 0;
                WsHandshakesCounter = 0;

                return new Response() { Body = String.Format("WebSockets counters: handshakes={0}, echoes received={1}, disconnects={2}", h, e, d) };
            });

            Handle.WebSocket("echotestws", (String s, WebSocket ws) => {
                Interlocked.Increment(ref WsEchoesCounter);

                ws.Send(s);
            });

            Handle.WebSocket("echotestws", (Byte[] bs, WebSocket ws) => {
                Interlocked.Increment(ref WsEchoesCounter);

                ws.Send(bs);
            });

            Handle.WebSocketDisconnect("echotestws", (WebSocket ws) => {
                Interlocked.Increment(ref WsDisconnectsCounter);

                // Do nothing!
            });

            Handle.DELETE("/resetcounters", (Request req) => {

                // Checking for client IP address.
                String clientIp = req.ClientIpAddress.ToString();
                if (clientIp != "127.0.0.1")
                    throw new Exception("Wrong client IP address: " + clientIp);

                WsEchoesCounter = 0;
                WsDisconnectsCounter = 0;
                WsHandshakesCounter = 0;
                HttpEchoesCounter = 0;

                return 200;
            });

            Handle.GET("/httpcounters", (Request req) => {

                // Checking for client IP address.
                String clientIp = req.ClientIpAddress.ToString();
                if (clientIp != "127.0.0.1")
                    throw new Exception("Wrong client IP address: " + clientIp);

                Int32 e = HttpEchoesCounter;

                HttpEchoesCounter = 0;

                return new Response() { Body = String.Format("Http counters: echoes received={0}.", e) };
            });

            Handle.POST("/echotest", (Request req) => {

                // Checking for client IP address.
                String clientIp = req.ClientIpAddress.ToString();
                if (clientIp != "127.0.0.1")
                    throw new Exception("Wrong client IP address: " + clientIp);

                Interlocked.Increment(ref HttpEchoesCounter);

                return new Response() { BodyBytes = req.BodyBytes };
            });

            #endregion

        }

    }

}
