using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

            // Get a list with all available local applications  (Deployed,Installed,Running)
            // Note: Remote AppStore applications is not included
            Handle.GET("/api/admin/databases/{?}/applicationuploadws", (string databaseName, Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

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

                                UploadTask ut = new UploadTask() { Stream = new MemoryStream(), Database = database };
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
                            try { task.DoneCallback(task); } catch { };
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        private static void UploadTaskCallback(UploadTask task) {

            bool throwErrorIfExist = true;
            DeployedConfigFile config = null;

            try {

                //using (MemoryStream packageZip = new MemoryStream(data)) {

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
                PackageManager.Unpack(task.Stream, HttpUtility.HtmlEncode(sourceUrl), HttpUtility.HtmlEncode(storUrl), DeployManager.GetDeployFolder(task.Database.ID), imageResourceFolder, out config);

                // Install package (Unzip)
                //                    PackageManager.Unpack(packageZip, sourceUrl, sourceUrl, DeployManager.GetDeployFolder(database.ID), imageResourceFolder, out config);

                // Update server model
                DatabaseApplication deployedApplication = DatabaseApplication.ToApplication(config, task.Database.ID);
                deployedApplication.IsDeployed = true;
                task.Database.Applications.Add(deployedApplication);
                //if (completionCallback != null) {
                //    completionCallback(deployedApplication);
                //}
                //                }
            }
            catch (InvalidOperationException e) {

                //task.Socket.Disconnect(e.ToString());

                task.Socket.Send(e.ToString());


                if (throwErrorIfExist == false && config != null) {
                    // Find app
                    DatabaseApplication existingApplication = task.Database.GetApplication(config.Namespace, config.Channel, config.Version);
                    if (existingApplication != null) {
                        //if (completionCallback != null) {
                        //    completionCallback(existingApplication);
                        //}
                        return;
                    }
                }

                // TODO:
                //if (errorCallback != null) {
                //    errorCallback(e.Message);
                //}
            }
            catch (Exception e) {

                //                task.Socket.Disconnect(e.ToString());

                task.Socket.Send(e.ToString());

                // TODO:
                //if (errorCallback != null) {
                //    errorCallback(e.Message);
                //}
            }


            //string file = @"d:\tmp\uploaded.zip";

            //try {

            //    // Move and name the temp file to it's correct place
            //    //            PackageManager.VerifyPacket(task.TempFilename);

            //    using (FileStream fileStream = new FileStream(file, FileMode.Create)) {
            //        task.Stream.Seek(0, SeekOrigin.Begin);
            //        task.Stream.CopyTo(fileStream);
            //    }
            //}
            //catch (Exception) {

            //    if (file != null && File.Exists(file)) {
            //        File.Delete(file);
            //    }

            //    throw;
            //}
        }


        private class UploadTask {
            public Stream Stream;
            public Database Database;
            public WebSocket Socket;
            public Action<UploadTask> DoneCallback;
        }




    }
}
