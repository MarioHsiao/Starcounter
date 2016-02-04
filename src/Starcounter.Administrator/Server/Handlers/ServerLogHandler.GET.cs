using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Internal;
using System.Threading;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Server log WebSockets group name.
        /// </summary>
        const String ServerLogWebSocketGroupName = "logs";

        /// <summary>
        /// All active console web sockets.
        /// </summary>
        static LinkedList<UInt64> ServerLogWebSockets = new LinkedList<UInt64>();

        /// <summary>
        /// The Timer is used to eliminate multiple events from the FileSystemWatcher
        /// </summary>
        private static System.Timers.Timer delaySendTimer;

        public static void ServerLog_GET(ushort port) {

            #region Log (/adminapi/v1/server/log)

            Handle.GET("/api/admin/log?{?}", (string parameters, Request req) => {

                lock (LOCK) {
                    try {

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

                        string filter_sourceList = collection["source"];
                        if (!string.IsNullOrEmpty(filter_sourceList)) {
                            logApp.FilterSource = filter_sourceList;
                        }

                        long filter_maxitems;
                        if (long.TryParse(collection["maxitems"], out filter_maxitems) ) {
                            logApp.FilterMaxItems = filter_maxitems;
                        }

                        #endregion

                        logApp.RefreshLogEntriesList();
                        return logApp;
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Returns the log
            Handle.GET("/api/admin/log", (Request req) => {
                lock (LOCK) {

                    try {

                        LogApp logApp = new LogApp() { FilterDebug = false, FilterNotice = false, FilterWarning = true, FilterError = true };
                        logApp.RefreshLogEntriesList();
                        return logApp;
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            #endregion

            // Handle WebSocket connections for listening on log changes
            Handle.GET("/api/admin/log/event/ws", (Request req) => {

                if (req.WebSocketUpgrade)
                {
                    Byte schedId = StarcounterEnvironment.CurrentSchedulerId;

                    WebSocket ws = req.SendUpgrade(ServerLogWebSocketGroupName);

                    lock (ServerLogWebSocketGroupName) {
                        ServerLogWebSockets.AddFirst(ws.ToUInt64());
                    }

                    return HandlerStatus.Handled;
                }

                return new Response() {
                    StatusCode = 500,
                    StatusDescription = "WebSocket upgrade on " + req.Uri + " was not approved."
                };
            });

            Handle.WebSocketDisconnect(ServerLogWebSocketGroupName, (WebSocket ws) =>
            {
                lock (ServerLogWebSocketGroupName) {

                    UInt64 wsId = ws.ToUInt64();

                    if (ServerLogWebSockets.Contains(wsId))
                        ServerLogWebSockets.Remove(wsId);
                }   
            });

            Handle.WebSocket(ServerLogWebSocketGroupName, (String s, WebSocket ws) =>
            {
                // We don't use client messages.
            });

            SetupLogListener();
        }
        
        /// <summary>
        /// Setup a listener on the log file(s)
        /// </summary>
        /// <param name="WebSocketSessions"></param>
        private static void SetupLogListener() {

            ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = serverInfo.Configuration.LogDirectory;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            watcher.Filter = "starcounter.*.log";
            watcher.Changed += watcher_Changed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Event when a log file has changed
        /// </summary>
        /// <param name="sender">FileSystemWatcher</param>
        /// <param name="e"></param>
        static void watcher_Changed(object sender, FileSystemEventArgs e) {

            // The Timer is used to eliminate multiple events from the FileSystemWatcher
            if (delaySendTimer == null) {
                delaySendTimer = new System.Timers.Timer(100);
                delaySendTimer.Elapsed += delaySendTimer_Elapsed;
            }

            if (delaySendTimer.Enabled) return;
            delaySendTimer.Enabled = true;
        }

        /// <summary>
        /// Event when the delaySendTimer has elapsed
        /// </summary>
        /// <param name="sender">Timer</param>
        /// <param name="e"></param>
        static void delaySendTimer_Elapsed(object sender, ElapsedEventArgs e) {
            sendLogChangedEvent();
            delaySendTimer.Enabled = false;
        }

        /// <summary>
        /// Send log changed event to all WebSocket connections
        /// </summary>
        static void sendLogChangedEvent() {

            // Getting sessions for current scheduler.
            Scheduling.ScheduleTask(() => {

                lock (ServerLogWebSocketGroupName) {

                    foreach (UInt64 wsId in ServerLogWebSockets) {
                        WebSocket ws = new WebSocket(wsId);
                        ws.Send("1");
                    }
                }
            });
        }
    }
}
