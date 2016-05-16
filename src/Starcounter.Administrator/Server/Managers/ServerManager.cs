using Starcounter;
using Starcounter.Administrator.Server;
using System;
using System.Collections.Generic;
using Administrator.Server.Model;
using Starcounter.Internal;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.XSON;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Administrator.Server.Managers {
    public class ServerManager {

        public static Model.Server ServerInstance;
        private static ConcurrentDictionary<ulong, Database> databaseModelSockets;
        static Object lockObject_ = new Object();

        private static List<String> sessionList_ = new List<String>();

        static void DestroySession(Session s) {

            if (null == s)
                return;

            lock (lockObject_) {

                String sessionString = s.SessionId;

                if (sessionList_.Contains(sessionString)) {
                    sessionList_.Remove(sessionString);
                }
            }
        }

        /// <summary>
        /// Initilized and register handlers
        /// </summary>
        public static void Init() {

            ServerManager.ServerInstance = new Model.Server();
            ServerManager.ServerInstance.Init();
            ServerManager.ServerInstance.Changed += ServerModel_Changed;

            ServerManager.databaseModelSockets = new ConcurrentDictionary<ulong, Database>();

            ServerManager.RegisterInternalModelApi();
            ServerManager.RegisterFullModelApi();
            ServerManager.RegisterDatabaseModelApi(8182);


            ExternalAPI.Register();
        }

        /// <summary>
        /// Returns response with all needed access control headers set.
        /// </summary>
        /// <returns></returns>
        static Response GetAllowAccessControlResponse() {

            Response response = new Response();

            response.Headers["Access-Control-Expose-Headers"] = "Location, X-Location";
            response.Headers["Access-Control-Allow-Headers"] = "Accept, Content-Type, X-Location, Location";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, HEAD, OPTIONS, PUT, DELETE, PATCH";
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.StatusCode = (UInt16)System.Net.HttpStatusCode.OK;

            return response;
        }

        /// <summary>
        /// Register handlers for the full model api
        /// </summary>
        private static void RegisterFullModelApi() {

            string socketChannelName = "servermodel";

            Handle.GET("/api/servermodel/{?}/{?}", (string key, Session session, Request request) => {

                lock (lockObject_) {

                    // Check if the request was a WebSocket request.
                    if (request.WebSocketUpgrade) {

                        // Checking if its internal Self.GET that has no session.
                        if (session != null) {
                            String sessionString = session.SessionId;

                            if (!sessionList_.Contains(sessionString)) {
                                sessionList_.Add(sessionString);
                            }
                        }

                        WebSocket ws = request.SendUpgrade(socketChannelName, null, null, session);
                        return HandlerStatus.Handled;
                    }

                    return 513; // 513 Message Too Large
                }

            });

            Handle.GET("/api/servermodel", (Request request) => {

                lock (lockObject_) {

                    // Create view-model
                    ServerJson serverModelJson = new ServerJson();

                    // Set data object to view-model
                    serverModelJson.Data = ServerManager.ServerInstance;

                    // Store the view-model 
                    string id = TemporaryStorage.Store(serverModelJson, DestroySession);

                    // Create response
                    Response response = GetAllowAccessControlResponse();
                    response.Resource = serverModelJson;
                    response.Headers["Set-Cookie"] = request.Uri + "/" + id;
                    response.Headers["X-Location"] = request.Uri + "/" + id + "/" + Session.Current.SessionId;

                    return response;
                }
            });

            // Incoming patch on socket
            Handle.WebSocket(socketChannelName, (string data, WebSocket ws) => {

                lock (lockObject_) {

                    if (ws.Session == null) {
                        ws.Disconnect("Session is null", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                        return;
                    }

                    Json viewModel = ((Starcounter.Session)ws.Session).Data;
                    ServerManager.ServerInstance.JsonPatchInstance.Apply(viewModel, data);
                }
            });

            Handle.WebSocketDisconnect(socketChannelName, (ws) => {
                // Remove ws.
                lock (lockObject_) {

                    Session s = ((Session)ws.Session);

                    DestroySession(s);
                }

            });
        }

        /// <summary>
        /// Register handlers for the database model api
        /// </summary>
        private static void RegisterDatabaseModelApi(ushort port) {

            string socketChannelName = "databaseGroupName";

            Handle.GET(port, "/api/servermodel/{?}/{?}/{?}", (string databaseName, string key, Session session, Request request) => {

                lock (lockObject_) {

                    // Check if the request was a WebSocket request.
                    if (request.WebSocketUpgrade) {

                        // Checking if its internal Self.GET that has no session.
                        if (session != null) {

                            String sessionString = session.SessionId;

                            if (!sessionList_.Contains(sessionString)) {
                                sessionList_.Add(sessionString);
                            }
                        }

                        Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                        if (database != null) {

                            WebSocket ws = request.SendUpgrade(socketChannelName, null, null, session);
                            databaseModelSockets[ws.ToUInt64()] = database;
                            return HandlerStatus.Handled;
                        } else {
                            // TODO:
                        }
                    }

                    return 513; // 513 Message Too Large
                }
            });

            Handle.GET(port, "/api/servermodel/{?}", (string databaseName, Request request) => {

                lock (lockObject_) {

                    // Create view-model
                    DatabaseJson databaseJson = new DatabaseJson();

                    // Set data object to view-model
                    databaseJson.Data = ServerManager.ServerInstance.GetDatabase(databaseName);

                    // Store the view-model 
                    string id = TemporaryStorage.Store(databaseJson, DestroySession);

                    // Create response
                    Response response = new Response();
                    response.Resource = databaseJson;
                    response.Headers["Set-Cookie"] = request.Uri + "/" + id;
                    response.Headers["X-Location"] = request.Uri + "/" + id + "/" + Session.Current.SessionId;

                    response.Headers["Access-Control-Allow-Origin"] = "*"; // "http://localhost:8080";
                    response.Headers["Access-Control-Expose-Headers"] = "Location, X-Location";
                    //response.Headers["Access-Control-Allow-Headers"] = "X-Referer";
                    return response;
                }
            });

            // Incoming patch on http
            Handle.PATCH(port, "/api/servermodel/{?}/{?}/{?}", (string dbName, string id, Session session, Request request) => {

                lock (lockObject_) {

                    Json json = TemporaryStorage.Find(id);
                    ServerManager.ServerInstance.JsonPatchInstance.Apply(json, request.Body);
                    return GetAllowAccessControlResponse();
                }
            });

            // Options for server model request.
            Handle.OPTIONS(port, "/api/servermodel/{?}/{?}/{?}", (string dbName, string id, Session session, Request request) => {

                return GetAllowAccessControlResponse();
            });



            // Incoming patch on socket
            Handle.WebSocket(port, socketChannelName, (string data, WebSocket ws) => {
                lock (lockObject_) {

                    if (ws.Session == null) {
                        ws.Disconnect("Session is null", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                        return;
                    }

                    Database database;
                    if (databaseModelSockets.TryGetValue(ws.ToUInt64(), out database)) {
                        Json viewModel = ((Starcounter.Session)ws.Session).Data;
                        database.JsonPatchInstance.Apply(viewModel, data);
                    }
                }
            });

            Handle.WebSocketDisconnect(port, socketChannelName, (ws) => {
                lock (lockObject_) {

                    DestroySession((Session)ws.Session);

                    // Remove ws.
                    Database database;
                    databaseModelSockets.TryRemove(ws.ToUInt64(), out database);
                }
            });
        }

        /// <summary>
        /// Register internal handler
        /// Used to uppdate the model from within the "PublicModelProvider"
        /// </summary>
        private static void RegisterInternalModelApi() {

            Handle.POST("/__internal_api/databases", (Request request) => {

                lock (lockObject_) {

                    // Database added
                    ServerManager.ServerInstance.InvalidateDatabases();
                }

                return 200;
            });

            Handle.DELETE("/__internal_api/databases/{?}", (string databaseName, Request request) => {

                lock (lockObject_) {

                    // Database deleted
                    ServerManager.ServerInstance.InvalidateDatabases();
                }

                return 200;
            });

            Handle.PUT("/__internal_api/databases/{?}", (string databaseName, Request request) => {

                lock (lockObject_) {
                    // Database properties changed and/or database application(s) started/stopped

                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    if (database == null) {
                        ServerManager.ServerInstance.InvalidateDatabases();
                        database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    }

                    if (database == null) {
                        // Error;
                        return 500;
                    }

                    database.InvalidateModel();
                }

                return 200;
            });
        }

        /// <summary>
        /// Called when the server model has been changed
        /// The model includes the Server, Database and Application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void ServerModel_Changed(object sender, EventArgs e) {

            Session.ScheduleTask(ServerManager.sessionList_, (Session session, String sessionId) => {

                lock (lockObject_) {

                    if (session == null) {
                        if (sessionList_.Contains(sessionId)) {
                            sessionList_.Remove(sessionId);
                        }
                        return;
                    }

                    string changes = ServerManager.GetModelChanges(session);
                    if (changes != null) {
                        session.ActiveWebSocket.Send(changes);
                    }
                }
            });
        }

        /// <summary>
        /// Get Session model changes
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static string GetModelChanges(Session session) {

            string changes = null;
            if (session.PublicViewModel is DatabaseJson) {

                Database database;
                if (databaseModelSockets.TryGetValue(session.ActiveWebSocket.ToUInt64(), out database)) {
                    changes = database.JsonPatchInstance.Generate(session.PublicViewModel, true, false);
                }
            } else if (session.PublicViewModel is ServerJson) {
                changes = ServerManager.ServerInstance.JsonPatchInstance.Generate(session.PublicViewModel, true, false);
            }

            if (!string.IsNullOrEmpty(changes) && changes != "[]") {
                return changes;
            }

            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// 
    public static class TemporaryStorage {
        private static Dictionary<string, Json> Storage = new Dictionary<string, Json>();

        public static string Store(Json json, Action<Session> sessionDestroy) {
            string id = Guid.NewGuid().ToString();

            Session session = new Session(SessionOptions.StrictPatchRejection);
            session.AddDestroyDelegate(sessionDestroy);

            session.TimeoutMinutes = 1;
            session.Data = json;

            Storage[id] = json;
            return id;

        }
        public static Json Find(string key) {
            if (Storage.ContainsKey(key)) {
                return Storage[key];
            }
            return null;
        }

        public static void Remove(string key) {
            if (Storage.ContainsKey(key)) {
                Storage.Remove(key);
            }
        }

    }
}
