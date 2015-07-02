using Starcounter;
using Starcounter.Administrator.Server;
using System;
using System.Collections.Generic;
using Administrator.Server.Model;
using Starcounter.Internal;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.XSON;
using System.Collections.Concurrent;

namespace Administrator.Server.Managers {
    public class ServerManager {

        public static Model.Server ServerInstance;
        private static ConcurrentDictionary<ulong, Database> databaseModelSockets;
        static Object lockObject_ = new Object();

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
            ServerManager.RegisterDatabaseModelApi();

            //Handle.WebSocketDisconnect(ServerManager.SocketChannelName, (session) => {
            //});
        }

        /// <summary>
        /// Returns response with all needed access control headers set.
        /// </summary>
        /// <returns></returns>
        static Response GetAllowAccessControlResponse() {

            Response response = new Response();

            response["Access-Control-Expose-Headers"] = "Location, X-Location";
            response["Access-Control-Allow-Headers"] = "Accept, Content-Type, X-Location, Location";
            response["Access-Control-Allow-Methods"] = "GET, POST, HEAD, OPTIONS, PUT, DELETE, PATCH";
            response["Access-Control-Allow-Origin"] = "*";
            response.StatusCode = (UInt16)System.Net.HttpStatusCode.OK;

            return response;
        }

        /// <summary>
        /// Register handlers for the full model api
        /// </summary>
        private static void RegisterFullModelApi() {

            string socketChannelName = "servermodel";

            // TODO: Anders check please if this was needed and why?
            // I.e. why there are 3 parameters sometimes and 2???

            Handle.GET("/api/servermodel/{?}/{?}", (string key, Session session, Request request) => {

                // Check if the request was a WebSocket request.
                if (request.WebSocketUpgrade) {

                    WebSocket ws = request.SendUpgrade(socketChannelName, null, null, session);
                    return HandlerStatus.Handled;
                }

                return 513; // 513 Message Too Large
            });

            Handle.GET("/api/servermodel", (Request request) => {

                // Create view-model
                ServerJson serverModelJson = new ServerJson();

                // Set data object to view-model
                serverModelJson.Data = ServerManager.ServerInstance;

                // Store the view-model 
                string id = TemporaryStorage.Store(serverModelJson);

                // Create response
                Response response = GetAllowAccessControlResponse();
                response.Resource = serverModelJson;
                response["Set-Cookie"] = request.Uri + "/" + id;
                response["X-Location"] = request.Uri + "/" + id + "/" + Session.Current.SessionIdString;

                return response;
            });

            // Incoming patch on http
            Handle.PATCH("/api/servermodel/{?}/{?}/{?}", (string dbName, string id, Session session, Request request) => {

                Json json = TemporaryStorage.Find(id);

                ServerManager.ServerInstance.JsonPatchInstance.Apply(json, request.Body);

                return GetAllowAccessControlResponse();
            });

            // Options for server model request.
            Handle.OPTIONS("/api/servermodel/{?}/{?}/{?}", (string dbName, string id, Session session, Request request) => {

                return GetAllowAccessControlResponse();
            });

            // Incoming patch on socket
            Handle.WebSocket(socketChannelName, (string data, WebSocket ws) => {

                if (ws.Session == null) {
                    ws.Disconnect("Session is null", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                    return;
                }

                Json viewModel = ((Starcounter.Session)ws.Session).Data;
                ServerManager.ServerInstance.JsonPatchInstance.Apply(viewModel, data);
            });

            Handle.WebSocketDisconnect(socketChannelName, (ws) => {
                // Remove ws.
            });
        }

        /// <summary>
        /// Register handlers for the database model api
        /// </summary>
        private static void RegisterDatabaseModelApi() {

            string socketChannelName = "databaseGroupName";

            Handle.GET("/api/servermodel/{?}/{?}/{?}", (string databaseName, string key, Session session, Request request) => {

                // Check if the request was a WebSocket request.
                if (request.WebSocketUpgrade) {
                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    if (database != null) {

                        WebSocket ws = request.SendUpgrade(socketChannelName, null, null, session);
                        databaseModelSockets[ws.ToUInt64()] = database;
                        return HandlerStatus.Handled;
                    }
                    else {
                        // TODO:
                    }
                }

                return 513; // 513 Message Too Large
            });

            Handle.GET("/api/servermodel/{?}", (string databaseName, Request request) => {

                // Create view-model
                DatabaseJson databaseJson = new DatabaseJson();

                // Set data object to view-model
                databaseJson.Data = ServerManager.ServerInstance.GetDatabase(databaseName);

                // Store the view-model 
                string id = TemporaryStorage.Store(databaseJson);

                // Create response
                Response response = new Response();
                response.Resource = databaseJson;
                response["Set-Cookie"] = request.Uri + "/" + id;
                response["X-Location"] = request.Uri + "/" + id + "/" + Session.Current.SessionIdString;

                response["Access-Control-Allow-Origin"] = "*"; // "http://localhost:8080";
                response["Access-Control-Expose-Headers"] = "Location, X-Location";
                //response["Access-Control-Allow-Headers"] = "X-Referer";
                return response;
            });

            // Incoming patch on socket
            Handle.WebSocket(socketChannelName, (string data, WebSocket ws) => {

                if (ws.Session == null) {
                    ws.Disconnect("Session is null", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                    return;
                }

                Database database;
                if (databaseModelSockets.TryGetValue(ws.ToUInt64(), out database)) {
                    Json viewModel = ((Starcounter.Session)ws.Session).Data;
                    database.JsonPatchInstance.Apply(viewModel, data);
                }
            });

            // TODO: Anders check please if this was needed and why?
            // Incoming patch on http
            /*Handle.PATCH("/api/servermodel/{?}/{?}/{?}", (string databaseName, string id, Session session, Request request) => {

                //                Json json = TemporaryStorage.Find(id);
                //                ServerManager.ServerInstance.JsonPatchInstance.Apply(json, request.Body);
                return System.Net.HttpStatusCode.NotImplemented;
            });*/

            Handle.WebSocketDisconnect(socketChannelName, (ws) => {
                // Remove ws.
                Database database;
                databaseModelSockets.TryRemove(ws.ToUInt64(), out database);
            });
        }

        /// <summary>
        /// Register internal handler
        /// Used to uppdate the model from within the "PublicModelProvider"
        /// </summary>
        private static void RegisterInternalModelApi() {

            Handle.POST("/__internal_api/databases", (Request request) => {
                // Database added
                ServerManager.ServerInstance.InvalidateDatabases();
                return 200;
            });

            Handle.DELETE("/__internal_api/databases/{?}", (string databaseName, Request request) => {
                // Database deleted
                ServerManager.ServerInstance.InvalidateDatabases();
                return 200;
            });

            Handle.PUT("/__internal_api/databases/{?}", (string databaseName, Request request) => {
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

                return 200;
            });

            Handle.POST("/__internal_api/databases/{?}/task", (string databaseName, Request request) => {

                try {

                    Representations.JSON.ApplicationTask task = new Representations.JSON.ApplicationTask();
                    task.PopulateFromJson(request.Body);

                    if (string.Equals("Install", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        // TODO: Able to use the local ID to get the sourceUrl.
                        //return StarcounterAdminAPI.Install(task.SourceUrl, appsRootFolder, imageResourceFolder);

                        Database database = ServerInstance.GetDatabase(databaseName);

                        AppStoreApplication appStoreApplication = null;

                        foreach (AppStoreStore store in database.AppStoreStores) {

                            foreach (AppStoreApplication item in store.Applications) {

                                if (item.ID == task.ID) {
                                    appStoreApplication = item;
                                    break;
                                }
                            }
                        }

                        if (appStoreApplication != null) {
                            appStoreApplication.WantDeployed = true;    // Download

                            // TODO: this will not work when we fix the async download mode
                            if (appStoreApplication.DatabaseApplication != null) {
                                appStoreApplication.DatabaseApplication.WantInstalled = true;
                                appStoreApplication.DatabaseApplication.WantRunning = true;
                            }

                        }
                        else {
                            // TODO: Error
                        }
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };


                    }
                    else if (string.Equals("Uninstall", task.Type, StringComparison.InvariantCultureIgnoreCase)) {

                        Database database = ServerInstance.GetDatabase(databaseName);

                        AppStoreApplication appStoreApplication = null;
                        foreach (AppStoreStore store in database.AppStoreStores) {

                            foreach (AppStoreApplication item in store.Applications) {

                                if (item.ID == task.ID) {
                                    appStoreApplication = item;
                                    break;
                                }
                            }
                        }

                        if (appStoreApplication != null) {
                            appStoreApplication.DatabaseApplication.WantDeleted = true;

                        }
                        else {
                            // TODO: Error
                        }

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
                    }
                    else if (string.Equals("Upgrade", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        //return StarcounterAdminAPI.Upgrade(port, task.ID, appsRootFolder, imageResourceFolder);
                    }
                    else if (string.Equals("Start", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        //return StarcounterAdminAPI.Start(task.ID, task.DatabaseName, task.Arguments, appsRootFolder);
                    }
                    else if (string.Equals("Stop", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        //return StarcounterAdminAPI.Stop(task.ID, task.DatabaseName, appsRootFolder);
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.BadRequest };
                }
                catch (InvalidOperationException e) {
                    Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                    errorResponse.Text = e.Message;
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }

        /// <summary>
        /// Called when the server model has been changed
        /// The model includes the Server, Database and Application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void ServerModel_Changed(object sender, EventArgs e) {

            ServerManager.PushchangesToListeners();
        }

        /// <summary>
        /// This will be called when the model has been changed
        /// And we want to inform our listeners (connected clients)
        /// </summary>
        private static void PushchangesToListeners() {

            //// Database model listeners
            //foreach (var item in databaseModelSockets) {

            //    Database ws = item.Value;
            //    Json viewModel = ((Starcounter.Session)ws.Session).Data;
            //    Database db;
            //    string changes = db.JsonPatchInstance.Generate(viewModel, true, false);
            //    if (changes != "[]") {
            //        ws.Send(changes);
            //    }
            //    //Console.WriteLine(display.Value);
            //}


            Session.ForAll((session) => {
                lock (lockObject_) {

                    try {

                        if (session.ActiveWebSocket != null) {

                            string changes = string.Empty;

                            if (session.PublicViewModel is DatabaseJson) {

                                Database database;
                                if (databaseModelSockets.TryGetValue(session.ActiveWebSocket.ToUInt64(), out database)) {
                                    changes = database.JsonPatchInstance.Generate(session.PublicViewModel, true, false);
                                }
                                else {
                                    // Error, failed to get database
                                    //TODO:
                                }

                            }
                            else if (session.PublicViewModel is ServerJson) {
                                changes = ServerManager.ServerInstance.JsonPatchInstance.Generate(session.PublicViewModel, true, false);
                            }
                            else {
                                // TODO: Unknown?
                            }

                            //string changes = ServerManager.ServerInstance.JsonPatchInstance.Generate(s.PublicViewModel, true, false);
                            if (!string.IsNullOrEmpty(changes) && changes != "[]") {
                                session.ActiveWebSocket.Send(changes);
                            }
                        }
                    }
                    catch (Exception) {
                    }
                }

            });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// 
    public static class TemporaryStorage {
        private static Dictionary<string, Json> Storage = new Dictionary<string, Json>();

        public static string Store(Json json) {
            string id = Guid.NewGuid().ToString();

            Session session = new Session(SessionOptions.StrictPatchRejection);
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
