using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.Server;
using Starcounter.Administrator.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Administrator.Server.Managers {
    public class ExternalAPI {

        static List<Task> Tasks = new List<Task>();

        public static void Register() {

            // Get task
            Handle.GET("/api/tasks/{?}", (string taskID, Request request) => {

                lock (ServerManager.ServerInstance) {

                    string id = HttpUtility.UrlDecode(taskID);

                    // TODO: Make hashtable or ExternalAPI.Tasks
                    foreach (Task task in ExternalAPI.Tasks) {
                        if (task.ID == id) {
                            TaskJson taskItemJson = new TaskJson();
                            taskItemJson.Data = task;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, Body = taskItemJson.ToJson() };
                        }
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                }
            });

            // Create database task
            Handle.POST("/api/tasks/createdatabase", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        CreateDatabaseTaskJson task = new CreateDatabaseTaskJson();
                        task.PopulateFromJson(request.Body);

                        return CreateDatabase_Task(task.Settings);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Delete database task
            Handle.POST("/api/tasks/deletedatabase", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        DeleteDatabaseTaskJson task = new DeleteDatabaseTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);
                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return DeleteDatabase_Task(database);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });


            // Install application task
            Handle.POST("/api/tasks/installapplication", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        InstallApplicationTaskJson task = new InstallApplicationTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);
                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return InstallApplication_Task(database, task);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });


            // Upgrade application task
            Handle.POST("/api/tasks/upgradeapplication", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        UpgradeApplicationTaskJson task = new UpgradeApplicationTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);
                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        // Find app to upgrade, if there is a running app pick that one, otherwice pick the latest
                        DatabaseApplication databaseApplication = null;
                        IList<DatabaseApplication> apps = database.GetApplications(task.Namespace, task.Channel);
                        foreach (DatabaseApplication app in apps) {
                            if (app.IsRunning) {
                                databaseApplication = app;
                                break;
                            }
                        }

                        if (databaseApplication == null) {
                            databaseApplication = database.GetLatestApplication(task.Namespace, task.Channel);
                        }

                        if (databaseApplication == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "There is no application to upgrade from, try installing it first";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return UpgradeApplication_Task(databaseApplication, database, task.SourceUrl);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Install application task
            Handle.POST("/api/tasks/uninstallapplication", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        UninstallApplicationTaskJson task = new UninstallApplicationTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);
                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        DatabaseApplication databaseApplication = database.GetApplication(task.ID);
                        if (databaseApplication == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Application not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return UninstallApplication_Task(database, databaseApplication);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Start application task
            Handle.POST("/api/tasks/startapplication", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        StartApplicationTaskJson task = new StartApplicationTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);
                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        DatabaseApplication databaseApplication = database.GetApplication(task.ID);
                        if (databaseApplication == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Application not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return StartApplication_Task(database, databaseApplication);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Start database task
            Handle.POST("/api/tasks/startdatabase", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        StartDatabaseTaskJson task = new StartDatabaseTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);

                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return StartDatabase_Task(database, task);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Stop database task
            Handle.POST("/api/tasks/stopdatabase", (Request request) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        StopDatabaseTaskJson task = new StopDatabaseTaskJson();
                        task.PopulateFromJson(request.Body);

                        Database database = ServerManager.ServerInstance.GetDatabase(task.DatabaseName);

                        if (database == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Database not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return StopDatabase_Task(database, task);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

        }

        /// <summary>
        /// 
        /// </summary>
        class Task {
            public string ID;
            public DateTime Created;
            public string Namespace;
            public string Channel;
            public string Version;
            public string VersionDate;
            public string ResourceUri;
            public string ResourceID;
            public string TaskUri;
            public int Status;  // 0 = Success, 1 = Busy, <0 Error
            public String Message;

            public Task() {
                this.Created = DateTime.UtcNow;
                this.ID = RestUtils.GetHashString(this.Created.ToString("o"));
                this.TaskUri = string.Format("/api/tasks/{0}", this.ID);

                // Assure unique id
                while (true) {

                    bool unique = true;

                    foreach (Task task in ExternalAPI.Tasks) {
                        if (task.ID == this.ID) {
                            this.ID += "0";
                            unique = false;
                            break;
                        }
                    }

                    if (unique) break;
                }


                this.Status = 1; // Busy
            }
        }


        /// <summary>
        /// TODO: Installation Task
        /// </summary>
        /// <param name="database"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        static Response InstallApplication_Task(Database database, InstallApplicationTaskJson task) {

            // Create TaskItem
            Task taskItem = new Task();

            DeployManager.Download(task.SourceUrl, database, false, (deployedApplication) => {

                // Success
                #region Install Application
                deployedApplication.InstallApplication((installedApplication) => {

                    #region SetCanBeUninstalledFlag

                    installedApplication.SetCanBeUninstalledFlag(task.CanBeUninstalled, (databaseApplication) => {

                        // Success
                        // If database is started start application
                        if (installedApplication.Database.IsRunning) {

                            #region Start Application
                            // TODO: Handle success
                            installedApplication.StartApplication((startedApplication) => {
                                // TODO: Handle success
                                taskItem.Namespace = startedApplication.Namespace;
                                taskItem.Channel = startedApplication.Channel;
                                taskItem.Version = startedApplication.Version;
                                taskItem.VersionDate = startedApplication.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                                taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", HttpUtility.UrlEncode(databaseApplication.DatabaseName), HttpUtility.UrlEncode(databaseApplication.ID));
                                taskItem.ResourceID = databaseApplication.ID;
                                taskItem.Status = 0; // Done;
                            }, (startedApplication, wasCancelled, title, message, helpLink) => {
                                // TODO: Handle error
                                taskItem.Status = -5; // Error;
                                taskItem.Message = message;
                            });
                            #endregion
                        }
                        else {

                            taskItem.Namespace = databaseApplication.Namespace;
                            taskItem.Channel = databaseApplication.Channel;
                            taskItem.Version = databaseApplication.Version;
                            taskItem.VersionDate = databaseApplication.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                            taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", HttpUtility.UrlEncode(databaseApplication.DatabaseName), HttpUtility.UrlEncode(databaseApplication.ID));
                            taskItem.ResourceID = databaseApplication.ID;
                            taskItem.Status = 0; // Done;
                        }

                    }, (dapplication, wasCancelled, title, message, helpLink) => {

                        taskItem.Status = -7; // Error;
                        taskItem.Message = message;
                        // Error
                    });
                    #endregion

                }, (installedApplication, wasCancelled, title, message, helpLink) => {
                    // TODO: Handle error
                    taskItem.Status = -4; // Error;
                    taskItem.Message = message;
                });

                #endregion


            }, (message) => {
                taskItem.Status = -1; // Error;
                taskItem.Message = message;
            });

            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }

        /// <summary>
        /// Upgrade Application
        /// </summary>
        /// <param name="database"></param>
        /// <param name="databaseApplication"></param>
        /// <returns></returns>
        static Response UpgradeApplication_Task(DatabaseApplication currentDatabaseApplication, Database database, string sourceUrl) {

            // Create TaskItem
            Task taskItem = new Task();

            DeployManager.Download(sourceUrl, database, false, (deployedApplication) => {

                // Start upgrade
                currentDatabaseApplication.UpgradeApplication(deployedApplication, (databaseApplication) => {

                    // Success
                    taskItem.Namespace = deployedApplication.Namespace;
                    taskItem.Channel = deployedApplication.Channel;
                    taskItem.Version = deployedApplication.Version;
                    taskItem.VersionDate = deployedApplication.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                    taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", HttpUtility.UrlEncode(deployedApplication.DatabaseName), HttpUtility.UrlEncode(deployedApplication.ID));
                    taskItem.ResourceID = deployedApplication.ID;
                    taskItem.Status = 0; // Done;

                }, (databaseApplication, wasCanceled, title, message, helpLink) => {
                    taskItem.Status = -3; // Error;
                    taskItem.Message = string.Format("{0}. {1}. {2}", title, message, helpLink);
                });

            }, (errorMessage) => {

                taskItem.Status = -2; // Error;
                taskItem.Message = errorMessage;
            });

            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }

        /// <summary>
        /// Uninstall Application
        /// </summary>
        /// <param name="database"></param>
        /// <param name="databaseApplication"></param>
        /// <returns></returns>
        static Response UninstallApplication_Task(Database database, DatabaseApplication databaseApplication) {

            // Create TaskItem
            Task taskItem = new Task();

            // TODO: Force delete
            databaseApplication.DeleteApplication(true, (startedApplication) => {
                taskItem.Status = 0; // Done;
            }, (startedApplication, wasCancelled, title, message, helpLink) => {

                taskItem.Status = -1; // Error;
                taskItem.Message = message;
            });

            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }

        /// <summary>
        /// Start Application
        /// </summary>
        /// <param name="database"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        static Response StartApplication_Task(Database database, DatabaseApplication databaseApplication) {

            // Create TaskItem
            Task taskItem = new Task();

            databaseApplication.StartApplication((startedApplication) => {

                taskItem.Namespace = startedApplication.Namespace;
                taskItem.Channel = startedApplication.Channel;
                taskItem.Version = startedApplication.Version;
                taskItem.VersionDate = startedApplication.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", HttpUtility.UrlEncode(databaseApplication.DatabaseName), HttpUtility.UrlEncode(databaseApplication.ID));
                taskItem.ResourceID = databaseApplication.ID;
                taskItem.Status = 0; // Done;
            }, (startedApplication, wasCancelled, title, message, helpLink) => {

                taskItem.Status = -1; // Error;
                taskItem.Message = message;
            });

            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }

        /// <summary>
        /// Start database 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        static Response StartDatabase_Task(Database database, StartDatabaseTaskJson task) {

            // Create TaskItem
            Task taskItem = new Task();

            database.StartDatabase((aDatabase) => {

                taskItem.ResourceUri = aDatabase.Url;
                taskItem.ResourceID = database.ID;
                taskItem.Status = 0; // Done;

            }, (aDatabase, wasCancelled, title, message, helpLink) => {

                taskItem.Status = -1; // Error;
                taskItem.Message = message;
            });

            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }

        /// <summary>
        /// Stop database 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        static Response StopDatabase_Task(Database database, StopDatabaseTaskJson task) {

            // Create TaskItem
            Task taskItem = new Task();

            database.StopDatabase((aDatabase) => {

                taskItem.ResourceUri = aDatabase.Url;
                taskItem.ResourceID = aDatabase.ID;
                taskItem.Status = 0; // Done;

            }, (aDatabase, wasCancelled, title, message, helpLink) => {

                taskItem.Status = -1; // Error;
                taskItem.Message = message;
            });

            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }


        /// <summary>
        /// Create database 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        static Response CreateDatabase_Task(DatabaseSettings settings) {

            lock (ServerManager.ServerInstance) {

                // Create TaskItem
                Task taskItem = new Task();

                ServerManager.ServerInstance.CreateDatabase(settings, (database) => {

                    taskItem.ResourceUri = database.Url;
                    taskItem.ResourceID = database.ID;
                    taskItem.Status = 0; // Done;
                }, (wasCancelled, title, message, helpLink) => {

                    taskItem.Message = message;
                    taskItem.Status = -1; // Error;
                });

                TaskJson taskItemJson = new TaskJson();
                taskItemJson.Data = taskItem;
                ExternalAPI.Tasks.Add(taskItem);

                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
            }
        }

        /// <summary>
        /// Delete database 
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        static Response DeleteDatabase_Task(Database database) {

            // Create TaskItem
            Task taskItem = new Task();

            database.DeleteDatabase((deletedDatabase) => {

                taskItem.Status = 0; // Done;

            }, (deletedDatabase, wasCancelled, title, message, helpLink) => {

                taskItem.Status = -1; // Error;
                taskItem.Message = message;
            });


            TaskJson taskItemJson = new TaskJson();
            taskItemJson.Data = taskItem;
            ExternalAPI.Tasks.Add(taskItem);

            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = taskItemJson.ToJson() };
        }

    }
}
