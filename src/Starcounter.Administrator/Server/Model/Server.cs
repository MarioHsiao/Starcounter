using Administrator.Server.Managers;
using Representations.JSON;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.XSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Administrator.Server.Model {

    /// <summary>
    /// Server
    /// </summary>
    public class Server : INotifyPropertyChanged {

        internal JsonPatch JsonPatchInstance;

        #region Properties

        private bool _CreatingDatabaseError;
        public bool CreatingDatabaseError
        {
            get
            {
                return this._CreatingDatabaseError;
            }
            set
            {
                if (this._CreatingDatabaseError == value) return;
                this._CreatingDatabaseError = value;
                this.OnPropertyChanged("CreatingDatabaseError");
            }
        }


        #region Status

        private ErrorMessage _ErrorMessage = new ErrorMessage();
        public ErrorMessage ErrorMessage
        {
            get
            {
                return this._ErrorMessage;
            }
        }

        public bool HasErrorMessage
        {
            get
            {
                return !(
                string.IsNullOrEmpty(this.ErrorMessage.Title) &&
                string.IsNullOrEmpty(this.ErrorMessage.Message) &&
                string.IsNullOrEmpty(this.ErrorMessage.HelpLink));
            }
        }

        private ServerStatus _Status;
        public ServerStatus Status
        {
            get
            {
                return this._Status;
            }
            set
            {
                if (this._Status == value) return;
                this._Status = value;
                this.OnPropertyChanged("Status");
            }
        }

        private string _StatusText;
        public string StatusText
        {
            get
            {
                return this._StatusText;
            }
            set
            {
                if (this._StatusText == value) return;
                this._StatusText = value;
                this.OnPropertyChanged("StatusText");
            }
        }

        #endregion


        /// <summary>
        /// List of databases
        /// TODO: Make thread-safe
        /// </summary>
        public ObservableCollection<Database> Databases = new ObservableCollection<Database>();

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public Server() {

            this.PropertyChanged += Server_PropertyChanged;
            this.Databases.CollectionChanged += Databases_CollectionChanged;
            this.JsonPatchInstance = new JsonPatch();
        }

        /// <summary>
        /// When a property is changed on the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Server_PropertyChanged(object sender, PropertyChangedEventArgs e) {

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// Initilize list of databases
        /// </summary>
        public void Init() {

            this.InvalidateDatabases();
        }

        /// <summary>
        /// Called when a database has been added or removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Databases_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    // Add listeners on the database instance
                    foreach (Database item in e.NewItems) {
                        item.Changed += Database_Changed;
                        item.InvalidateModel();
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                    // Remove listeners on the database instance
                    foreach (Database item in e.OldItems) {
                        item.Changed -= Database_Changed;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    // Remove listeners on the database instance
                    foreach (Database item in this.Databases) {
                        item.Changed -= Database_Changed;
                        item.Changed += Database_Changed;
                        item.InvalidateModel();
                    }
                    break;
            }

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// Called when a property on a database has been changed
        /// Ex. An application can have been started/stopped/removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Database_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// Called when the model has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, EventArgs e) {

            if (Changed != null) {
                Changed(sender, e);
            }
        }

        public bool InvalidateDatabase(string databaseName) {

            Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
            if (database == null) {
                ServerManager.ServerInstance.InvalidateDatabases();
                database = ServerManager.ServerInstance.GetDatabase(databaseName);
            }

            if (database == null) {
                // Error;
                return false;
            }

            database.InvalidateModel();
            return true;
        }

        /// <summary>
        /// Invalidate list of databases
        /// Assure that the list of databases is up-to-date
        /// </summary>
        public void InvalidateDatabases() {

            var server = RootHandler.Host.Runtime;
            var databaseInfos = server.GetDatabases();

            foreach (DatabaseInfo databaseInfo in databaseInfos) {

                Database database = this.GetDatabase(databaseInfo.Name);
                if (database == null) {
                    // Create
                    database = new Database();
                    database.ID = databaseInfo.Name.ToLower();
                    database.DisplayName = databaseInfo.Name;
                    database.Url = string.Format("/api/admin/databases/{0}", database.ID); // TODO: Fix hardcodes IP and Port
                    database.UserHttpPort = databaseInfo.Configuration.Runtime.DefaultUserHttpPort;
                    this.Databases.Add(database);
                }
            }

            // Remove removed database
            List<Database> removeList = new List<Database>();
            foreach (Database database in this.Databases) {

                bool bExist = false;
                foreach (DatabaseInfo databaseInfo in databaseInfos) {
                    if (databaseInfo.Name == database.ID) {
                        bExist = true;
                        break;
                    }
                }

                if (bExist == false) {
                    removeList.Add(database);
                }
            }

            foreach (var item in removeList) {
                this.Databases.Remove(item);
            }
        }

        /// <summary>
        /// Get database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Database GetDatabase(string id) {

            foreach (var database in this.Databases) {
                if (string.Equals(database.ID, id, StringComparison.InvariantCultureIgnoreCase)) {
                    return database;
                }
            }
            return null;
        }

        /// <summary>
        /// Get database
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        //private Database GetDatabaseByUrl(string url) {

        //    // TODO: Compare two url's
        //    foreach (var database in this.Databases) {
        //        if (string.Equals( database.Url, url, StringComparison.InvariantCultureIgnoreCase)) {
        //            return database;
        //        }
        //    }
        //    return null;
        //}

        #region CreateDatabase

        //private ConcurrentStack<Action<Database>> CreateDatabaseCallbacks = new ConcurrentStack<Action<Database>>();
        //private ConcurrentStack<Action< bool, string, string, string>> CreateDatabaseErrorCallbacks = new ConcurrentStack<Action< bool, string, string, string>>();

        /// <summary>
        /// Create database
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public void CreateDatabase(DatabaseSettings settings, Action<Database> completionCallback = null, Action<bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            this.CreatingDatabaseError = false;
            this.Status |= ServerStatus.CreatingDatabase;

            CreateDatabaseCommand command;
            command = new CreateDatabaseCommand(Program.ServerEngine, settings.Name);

            command.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = (ushort)settings.DefaultUserHttpPort;
            command.SetupProperties.Configuration.Runtime.SchedulerCount = (int)settings.SchedulerCount;
            command.SetupProperties.Configuration.Runtime.ChunksNumber = (int)settings.ChunksNumber;
            command.SetupProperties.StorageConfiguration.CollationFile = settings.CollationFile;
            command.SetupProperties.Configuration.Runtime.DumpDirectory = settings.DumpDirectory;
            command.SetupProperties.Configuration.Runtime.TempDirectory = settings.TempDirectory;
            command.SetupProperties.Configuration.Runtime.ImageDirectory = settings.ImageDirectory;
            command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = settings.TransactionLogDirectory;

            command.EnableWaiting = false;

            this.ExecuteCommand(command, () => {

                this.Status &= ~ServerStatus.CreatingDatabase;

                ServerManager.ServerInstance.InvalidateDatabases();

                Database database = this.GetDatabase(settings.Name);
                if (database == null) {
                }

                if (completionCallback != null) {
                    completionCallback(database);
                }
            }, (wasCancelled, title, message, helpLink) => {

                this.Status &= ~ServerStatus.CreatingDatabase;
                this.CreatingDatabaseError = true;
                this.OnCommandError(title, message, helpLink);

                if (errorCallback != null) {
                    errorCallback(wasCancelled, title, message, helpLink);
                }
            });
        }

        #endregion

        private void ResetErrorMessage() {

            if (string.IsNullOrEmpty(this.ErrorMessage.Title) && string.IsNullOrEmpty(this.ErrorMessage.Message) && string.IsNullOrEmpty(this.ErrorMessage.HelpLink)) {
                // No change
                return;
            }
            this.ErrorMessage.Title = string.Empty;
            this.ErrorMessage.Message = string.Empty;
            this.ErrorMessage.HelpLink = string.Empty;

            this.OnPropertyChanged("ErrorMessage");
            this.OnPropertyChanged("HasErrorMessage");
        }

        /// <summary>
        /// Execut Command (Start/Stop)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="database"></param>
        private void ExecuteCommand(ServerCommand command, Action completionCallback = null, Action<bool, string, string, string> errorCallback = null) {

            var runtime = RootHandler.Host.Runtime;

            // Execute Command
            var c = runtime.Execute(command, (commandId) => {

                //if (command is CreateDatabaseCommand && this.Status.HasFlag(ServerStatus.CreatingDatabase)) {

                //    return true;    // return true to cancel
                //}

                return false;   // return true to cancel

            }, (commandId) => {

                lock (ServerManager.ServerInstance) {
                    CommandInfo commandInfo = runtime.GetCommand(commandId);

                    this.StatusText = string.Empty;

                    if (commandInfo.HasError) {

                        //Check if command was Canceled
                        bool wasCancelled = false;
                        if (commandInfo.HasProgress) {
                            foreach (var p in commandInfo.Progress) {
                                if (p.WasCancelled == true) {
                                    wasCancelled = true;
                                    break;
                                }
                            }
                        }

                        ErrorInfo single = commandInfo.Errors.PickSingleServerError();
                        var msg = single.ToErrorMessage();

                        if (errorCallback != null) {
                            errorCallback(wasCancelled, command.Description, msg.Brief, msg.Helplink);
                        }
                    }
                    else {

                        if (completionCallback != null) {
                            completionCallback();
                        }
                    }
                }
            });

            this.StatusText = c.Description;

            if (c.IsCompleted) {

                CommandInfo commandInfo = runtime.GetCommand(c.CorrelatedCommandId);

                this.StatusText = string.Empty;

                if (c.HasError) {

                    //Check if command was Canceled
                    bool wasCancelled = false;
                    if (commandInfo.HasProgress) {
                        foreach (var p in commandInfo.Progress) {
                            if (p.WasCancelled == true) {
                                wasCancelled = true;
                                break;
                            }
                        }
                    }

                    ErrorInfo single = c.Errors.PickSingleServerError();
                    var msg = single.ToErrorMessage();
                    if (errorCallback != null) {
                        errorCallback(wasCancelled, command.Description, msg.Brief, msg.Helplink);
                    }
                }
                else {
                    if (completionCallback != null) {
                        completionCallback();
                    }
                }
            }
        }

        /// <summary>
        /// Set status text
        /// </summary>
        /// <param name="message"></param>
        private void OnCommandError(string title, string message, string helpLink) {

            // TODO: Append errors to notification list

            this.ErrorMessage.Title = title;
            this.ErrorMessage.Message = message;
            this.ErrorMessage.HelpLink = helpLink;

            this.OnPropertyChanged("ErrorMessage");
            this.OnPropertyChanged("HasErrorMessage");
        }

        /// <summary>
        /// Invoke action listeners
        /// </summary>
        /// <param name="listeners"></param>
        private void InvokeActionListeners(ConcurrentStack<Action<Database>> listeners, Database database) {

            while (listeners.Count > 0) {

                Action<Database> callback;
                if (listeners.TryPop(out callback)) {
                    callback(database);
                }
                else {
                    // TODO:
                    Console.WriteLine("TryPop() failed when it should have succeeded");
                }
            }
        }

        /// <summary>
        /// Invoke action error listeners
        /// </summary>
        /// <param name="listeners"></param>
        private void InvokeActionErrorListeners(ConcurrentStack<Action<bool, string, string, string>> listeners, bool wasCancelled, string title, string message, string helpLink) {

            while (listeners.Count > 0) {

                Action<bool, string, string, string> callback;
                if (listeners.TryPop(out callback)) {
                    callback(wasCancelled, title, message, helpLink);
                }
                else {
                    // TODO:
                    Console.WriteLine("TryPop() failed when it should have succeeded");
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }

        #endregion
    }

    [Flags]
    public enum ServerStatus : long {
        None = 0,
        Starting = 1,
        Stopping = 2,
        CreatingDatabase = 4
    }
}
