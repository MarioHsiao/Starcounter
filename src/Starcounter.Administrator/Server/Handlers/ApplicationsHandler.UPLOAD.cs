using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Web;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        internal static readonly object locker = new object();
        private static ConcurrentDictionary<ulong, UploadTask> Uploads = new ConcurrentDictionary<ulong, UploadTask>();

        /// <summary>
        /// Register Applications websocket UPLOAD
        /// </summary>
        public static void Applications_UPLOAD(ushort port) {

            Handle.GET("/api/admin/databases/{?}/applicationuploadws", (string databaseName, Request request) => {

                return UploadHandler(databaseName, "?overwrite=false&upgrade=false", request);
            });

            Handle.GET("/api/admin/databases/{?}/applicationuploadws?{?}", (string databaseName, string parameters, Request request) => {

                return UploadHandler(databaseName, parameters, request);
            });

            #region Incoming patch on socket
            Handle.WebSocket("UploadChannelName", (byte[] data, WebSocket ws) => {

                if (!Uploads.ContainsKey(ws.ToUInt64())) {
                    ws.Disconnect("Could not find correct socket to handle the incoming data.", WebSocket.WebSocketCloseCodes.WS_CLOSE_CANT_ACCEPT_DATA);
                    return;
                }

                UploadTask task = Uploads[ws.ToUInt64()];

                // Write data to stream/file
                task.Stream.Write(data, 0, data.Length);
                //task.Stream.Flush();
                // Report progress
                //if (task.FileSize > 0) {
                //    ws.Send(((int)(100.0 * task.Stream.Position / task.FileSize)).ToString());
                //}
            });
            #endregion

            Handle.WebSocketDisconnect("UploadChannelName", (ws) => {
                UploadTask task;
                if (Uploads.TryRemove(ws.ToUInt64(), out task)) {
                    if (task.DoneCallback != null) {
                        lock (locker) {
                            try {
                                task.DoneCallback(task, (databaseApplication) => {

                                    // Success

                                }, (error) => {

                                    // Error
                                    task.Socket.Send(error);
                                    StarcounterAdminAPI.AdministratorLogSource.LogWarning(string.Format("PacketUpload error, {0}", error));
                                });

                            }
                            catch (Exception e) {

                                // Error
                                task.Socket.Send(e.ToString());
                                StarcounterAdminAPI.AdministratorLogSource.LogError(string.Format("PacketUpload error, {0}", e.ToString()));
                            };
                        }
                    }
                }
            });
        }

        private static Response UploadHandler(string databaseName, string parameters, Request request) {

            lock (ServerManager.ServerInstance) {

                try {

                    NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(parameters);

                    bool bOverWrite = false;
                    if (!bool.TryParse(collection.Get("overwrite"), out bOverWrite)) {
                        bOverWrite = false;
                    }

                    bool bUpgrade = false;
                    if (!bool.TryParse(collection.Get("upgrade"), out bUpgrade)) {
                        bUpgrade = false;
                    }

                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    if (database == null) {
                        // Database not found
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Could not find the {0} database", databaseName);
                        errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    DatabaseApplicationsJson result = new DatabaseApplicationsJson();
                    if (request.WebSocketUpgrade) {

                        try {
                            ulong wsId = request.GetWebSocketId();

                            UploadTask ut = new UploadTask() { Stream = new MemoryStream(), Database = database, OverWrite = bOverWrite, Upgrade = bUpgrade };
                            ut.DoneCallback = UploadTaskCallback;

                            Uploads.TryAdd(wsId, ut);
                            ut.Socket = request.SendUpgrade("UploadChannelName");
                        }
                        catch (Exception e) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.Message };
                        }
                        return HandlerStatus.Handled;
                    }

                    return 513; // 513 Message Too Large                    
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        private static void UploadTaskCallback(UploadTask task, Action<DatabaseApplication> completionCallback, Action<string> errorCallback) {

            DeployedConfigFile config = null;

            try {

                string imageResourceFolder = System.IO.Path.Combine(Program.ResourceFolder, DeployManager.GetAppImagesFolder());

                string hash = string.Empty;
                DataContractSerializer serializer = new DataContractSerializer(task.Stream.GetType());
                using (MemoryStream memoryStream = new MemoryStream()) {
                    serializer.WriteObject(memoryStream, task.Stream);
                    MD5CryptoServiceProvider d = new MD5CryptoServiceProvider();
                    d.ComputeHash(memoryStream.ToArray());
                    hash = Convert.ToBase64String(d.Hash);
                }

                string sourceUrl = "http://127.0.0.1/local/" + hash;
                string storUrl = "http://127.0.0.1/localstore/" + hash;
                // TODO: HTML ENCODE

                try {
                    MemoryStream memStream = new MemoryStream();
                    task.Stream.Seek(0, SeekOrigin.Begin);
                    task.Stream.CopyTo(memStream);
                    // Check if version is already installed
                    PackageManager.Unpack(memStream, HttpUtility.HtmlEncode(sourceUrl), HttpUtility.HtmlEncode(storUrl), DeployManager.GetDeployFolder(task.Database.ID), imageResourceFolder, out config);
                    memStream.Dispose();
                }
                catch (InvalidOperationException e) {
                    // App already exists
                    if (task.OverWrite == false) {
                        errorCallback.Invoke(e.ToString());
                        return;
                    }
                }

                #region Uninstall existing version.
                bool bStartApplication = false;

                DatabaseApplication dataBaseApplication = task.Database.GetLatestApplication(config.Namespace, config.Channel);

                bStartApplication = dataBaseApplication != null && dataBaseApplication.IsRunning;

                if (dataBaseApplication != null && task.OverWrite) {
                    // Uninstall app if it already exist
                    dataBaseApplication.DeleteApplication(true, (application) => {

                        // Done
                        try {

                            PackageManager.Unpack(task.Stream, HttpUtility.HtmlEncode(sourceUrl), HttpUtility.HtmlEncode(storUrl), DeployManager.GetDeployFolder(task.Database.ID), imageResourceFolder, out config);

                            // Add to model
                            DatabaseApplication newDeployedApplication = DatabaseApplication.ToApplication(config, task.Database.ID);
                            newDeployedApplication.IsDeployed = true;
                            task.Database.Applications.Add(newDeployedApplication);

                            #region start application
                            if (bStartApplication) {

                                newDeployedApplication.StartApplication((databaseApplication) => {
                                    // Success
                                    completionCallback?.Invoke(databaseApplication);

                                }, (app, wasCancelled, title, message, helpLink) => {

                                    // Failed to start
                                    errorCallback.Invoke(message);
                                });
                            }
                            else {
                                completionCallback?.Invoke(newDeployedApplication);
                            }
                            #endregion

                        }
                        catch (Exception e) {
                            errorCallback.Invoke(e.ToString());
                        }

                    }, (application, wasCancelled, title, message, helpLink) => {

                        // Error
                        errorCallback.Invoke(message);
                    });
                    return;
                }
                #endregion

                // Update server model
                DatabaseApplication deployedApplication = DatabaseApplication.ToApplication(config, task.Database.ID);
                deployedApplication.IsDeployed = true;
                task.Database.Applications.Add(deployedApplication);

                #region Upgrade
                if (dataBaseApplication != null && task.Upgrade) {

                    dataBaseApplication.UpgradeApplication(deployedApplication, (application) => {

                        // Done
                        completionCallback?.Invoke(application);

                    }, (application, wasCancelled, title, message, helpLink) => {

                        // Error
                        errorCallback.Invoke(message);
                    });
                    return;
                }
                #endregion

                completionCallback?.Invoke(deployedApplication);
            }
            catch (Exception e) {

                //task.Socket.Disconnect(e.ToString());

                errorCallback.Invoke(e.ToString());
            }
        }


        private class UploadTask {
            public Stream Stream;
            public Database Database;
            public WebSocket Socket;
            public Action<UploadTask, Action<DatabaseApplication>, Action<string>> DoneCallback;
            public bool OverWrite;
            public bool Upgrade;
        }
    }
}
