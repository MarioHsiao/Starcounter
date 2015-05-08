using Starcounter;
using Starcounter.Administrator.Server;
using System;
using System.Collections.Generic;
using Administrator.Server.Model;
using Starcounter.Internal;
using Starcounter.Administrator.Server.Utilities;

namespace Administrator.Server.Managers {
    public class ServerManager {

        private const string SocketChannelName = "servermodel";

        public static Model.Server ServerInstance;

        /// <summary>
        /// Initilized and register handlers
        /// </summary>
        public static void Init() {

            ServerManager.ServerInstance = new Model.Server();
            ServerManager.ServerInstance.Init();
            ServerManager.ServerInstance.Changed += ServerModel_Changed;

            ServerManager.RegisterInternalModelApi();

            Handle.GET("/api/servermodel/{?}/{?}", (string key, Session session, Request request) => {

                // Check if the request was a WebSocket request.
                if (request.WebSocketUpgrade) {

                    WebSocket ws = request.SendUpgrade(ServerManager.SocketChannelName, null, null, session);
                    return HandlerStatus.Handled;
                }

                return 513; // 513 Message Too Large
            });

            Handle.GET("/api/servermodel", (Request request) => {

                // Create view-model
                ServerJson serverModelJson = new ServerJson() { LogChanges = true };
                serverModelJson.Session.CargoId = 4;

                // Set data object to view-model
                serverModelJson.Data = ServerManager.ServerInstance; // ServerManager.Server;

                // Store the view-model 
                string id = TemporaryStorage.Store(serverModelJson);

                // Create response
                Response response = new Response();
                response.Resource = serverModelJson;
                response["Set-Cookie"] = request.Uri + "/" + id;
                response["X-Location"] = request.Uri + "/" + id + "/" + Session.Current.SessionIdString;

                response["Access-Control-Allow-Origin"] = "http://localhost:8080";
                response["Access-Control-Expose-Headers"] = "Location, X-Location";
                return response;
            });


            // Incoming patch on socket
            Handle.WebSocket(ServerManager.SocketChannelName, (string data, WebSocket ws) => {

                if (ws.Session == null) {
                    ws.Disconnect("Session is null", WebSocket.WebSocketCloseCodes.WS_CLOSE_UNEXPECTED_CONDITION);
                    return;
                }

                Json viewModel = ((Starcounter.Session)ws.Session).Data;
                viewModel.ChangeLog.ApplyChanges(data);
            });

            Handle.WebSocketDisconnect(ServerManager.SocketChannelName, (session) => {

                Console.WriteLine("WebSocketDisconnect");

                // TODO: How to find and remove view-model from TemporaryStorage
                //TemporaryStorage.Remove(key);
            });

            // Incoming patch on http
            Handle.PATCH("/api/servermodel/{?}/{?}", (string id, Session session, Request request) => {

                Json json = TemporaryStorage.Find(id);
                json.ChangeLog.ApplyChanges(request.Body);
                return System.Net.HttpStatusCode.OK;
            });

            //Handle.POST("/api/servermodel/get-patches-and-clear-log?{?}", (Request r, string id) => {

            //      var serverStatusJson = TemporaryStorage.Find(id);
            //      var patches = serverStatusJson.ChangeLog.GetChanges();
            //      serverStatusJson.ChangeLog.Clear();
            //      return patches;
            //  });
        }

        /// <summary>
        /// Register internal handler
        /// Used to uppdate the model from within the "PublicModelProvider"
        /// </summary>
        private static void RegisterInternalModelApi() {

            #region Internal API
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

            #endregion
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

            //WebSocket.ForEach(ServerManager.SocketChannelName, (WebSocket socket) => {

            //    Json model = ((Starcounter.Session)socket.Session).PublicViewModel;
            //    string changes = model.ChangeLog.GetChanges();
            //    if (changes != "[]") {
            //        WriteToLog(changes);
            //        model.ChangeLog.Clear();
            //        socket.Send(changes);
            //    }
            //});

            Session.ForEach(4, (s) => {

                try {
                    if (s.ActiveWebSocket != null) {
                        string changes = s.PublicViewModel.ChangeLog.GetChanges();
                        if (changes != "[]") {
                            s.PublicViewModel.ChangeLog.Clear();
                            s.ActiveWebSocket.Send(changes);
                        }
                    }
                }
                catch (Exception) { }
            });


            //lock (ServerManager.Server) {

            //    string changes = ServerModel.ChangeLog.GetChanges();
            //    ServerModel.ChangeLog.Clear();

            //    if (changes == "[]") return;

            //    // Broadcast message to all listeners
            //    WebSocket.ForEach(ServerManager.SocketChannelName, (WebSocket socket) => {
            //        socket.Send(changes);
            //    });
            //}
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
