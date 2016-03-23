using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.Server;
using Starcounter.Administrator.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Managers {
    public class ExternalAPI {

        static List<Task> Tasks = new List<Task>();
        static Object lockObject_ = new Object();

        //Handle.GET("/__internal_api/databases/{?}/task/{?}", (string databaseName, string taskID, Request request) => {
        //Handle.POST("/__internal_api/databases/{?}/task", (string databaseName, Request request) => {


        public static void Register() {

            // Get task
            Handle.GET("/api/tasks/{?}", (string taskID, Request request) => {

                lock (lockObject_) {

                    // TODO: Make hashtable or ExternalAPI.Tasks
                    foreach (Task task in ExternalAPI.Tasks) {
                        if (task.ID == taskID) {
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

                lock (lockObject_) {

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

                lock (lockObject_) {

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

                lock (lockObject_) {

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


            // Install application task
            Handle.POST("/api/tasks/upgradeapplication", (Request request) => {

                lock (lockObject_) {

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

                        DatabaseApplication databaseApplication = database.GetApplication(task.ID);
                        if (databaseApplication == null) {

                            Starcounter.Administrator.Server.ErrorResponse errorResponse = new Starcounter.Administrator.Server.ErrorResponse();
                            errorResponse.Text = "Application not found";
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        return UpgradeApplication_Task(database, databaseApplication, task.SourceUrl);
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });


            // Install application task
            Handle.POST("/api/tasks/uninstallapplication", (Request request) => {

                lock (lockObject_) {

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

                lock (lockObject_) {

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

                lock (lockObject_) {

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

                lock (lockObject_) {

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

            DeployManager.Download(task.SourceUrl, database, (deployedApplication) => {

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
                                taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", databaseApplication.DatabaseName, databaseApplication.ID); // TODO: Fix hardcodes IP and Port
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

                            taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", databaseApplication.DatabaseName, databaseApplication.ID); // TODO: Fix hardcodes IP and Port
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
        static Response UpgradeApplication_Task(Database database, DatabaseApplication currentDatabaseApplication, string sourceUrl) {

            // Create TaskItem
            Task taskItem = new Task();

            database.InvalidateAppStoreStores(() => {
                // Success
                AppStoreApplication appStoreApplication = null;

                // Get the application
                foreach (AppStoreStore store in database.AppStoreStores) {

                    foreach (AppStoreApplication item in store.Applications) {

                        if (item.SourceUrl == sourceUrl) {
                            appStoreApplication = item;
                            break;
                        }
                    }

                    if (appStoreApplication != null) {
                        break;
                    }
                }

                if (appStoreApplication == null) {
                    taskItem.Status = -2; // Error;
                    taskItem.Message = "AppStore Application not found";
                }
                else {

                    appStoreApplication.UpgradeApplication(currentDatabaseApplication, (databaseApplication) => {

                        taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", databaseApplication.DatabaseName, databaseApplication.ID); // TODO: Fix hardcodes IP and Port
                        taskItem.ResourceID = databaseApplication.ID;
                        taskItem.Status = 0; // Done;
                    }, (startedApplication, wasCancelled, title, message, helpLink) => {

                        taskItem.Status = -1; // Error;
                        taskItem.Message = message;
                    });
                }

            }, (title, message, helpLink) => {
                // Error
                taskItem.Status = -1; // Error;
                taskItem.Message = message;
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

                //                taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", databaseApplication.DatabaseName, databaseApplication.ID); // TODO: Fix hardcodes IP and Port
                //                taskItem.ResourceID = databaseApplication.ID;
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

                taskItem.ResourceUri = string.Format("/api/admin/databases/{0}/applications/{1}", databaseApplication.DatabaseName, databaseApplication.ID); // TODO: Fix hardcodes IP and Port
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
