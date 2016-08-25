using Administrator.Server.Managers;
using Representations.JSON;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Administrator.Server.Model {

    /// <summary>
    /// Database
    /// </summary>
    public class Database : INotifyPropertyChanged {

        internal JsonPatch JsonPatchInstance;

        #region Properties
        private string _ID;
        public string ID {
            get {
                return this._ID;
            }
            internal set {
                if (this._ID == value) return;
                this._ID = value;
                this.OnPropertyChanged("ID");
            }
        }

        private ushort _UserHttpPort;
        public ushort UserHttpPort {
            get {
                return this._UserHttpPort;
            }
            internal set {
                if (this._UserHttpPort == value) return;
                this._UserHttpPort = value;
                this.OnPropertyChanged("UserHttpPort");
            }
        }

        private string _Url;
        public string Url {
            get {
                return this._Url;
            }
            set {
                if (this._Url == value) return;
                this._Url = value;
                this.OnPropertyChanged("Url");
            }
        }

        private string _DisplayName;
        public string DisplayName {
            get {
                return this._DisplayName;
            }
            set {
                if (this._DisplayName == value) return;
                this._DisplayName = value;
                this.OnPropertyChanged("DisplayName");
            }
        }

        #region Status

        private ErrorMessage _ErrorMessage = new ErrorMessage();
        public ErrorMessage ErrorMessage {
            get {
                return this._ErrorMessage;
            }
        }

        public bool HasErrorMessage {
            get {
                return !(
                string.IsNullOrEmpty(this.ErrorMessage.Title) &&
                string.IsNullOrEmpty(this.ErrorMessage.Message) &&
                string.IsNullOrEmpty(this.ErrorMessage.HelpLink));
            }
        }

        private DatabaseStatus _Status;
        public DatabaseStatus Status {
            get {
                return this._Status;
            }
            set {
                if (this._Status == value) return;
                this._Status = value;
                this.OnPropertyChanged("Status");
            }
        }

        private string _StatusText;
        public string StatusText {
            get {
                return this._StatusText;
            }
            set {
                if (this._StatusText == value) return;
                this._StatusText = value;
                this.OnPropertyChanged("StatusText");
            }
        }

        #endregion

        #region Running
        private bool _IsRunning;
        public bool IsRunning {
            get {
                return this._IsRunning;
            }
            set {
                if (this._IsRunning == value) return;
                this._IsRunning = value;
                this.OnPropertyChanged("IsRunning");
            }
        }

        private bool _StartingError;
        public bool StartingError {
            get {
                return this._StartingError;
            }
            set {
                if (this._StartingError == value) return;
                this._StartingError = value;
                this.OnPropertyChanged("StartingError");
            }
        }

        private bool _StoppingError;
        private bool StoppingError {
            get {
                return this._StoppingError;
            }
            set {
                if (this._StoppingError == value) return;
                this._StoppingError = value;
                this.OnPropertyChanged("StoppingError");
            }
        }
        #endregion

        #region Delete
        private bool _IsDeleted;
        public bool IsDeleted {
            get {
                return this._IsDeleted;
            }
            set {
                if (this._IsDeleted == value) return;
                this._IsDeleted = value;
                this.OnPropertyChanged("IsDeleted");
            }
        }

        private bool _DeletingError;
        public bool DeletingError {
            get {
                return this._DeletingError;
            }
            set {
                if (this._DeletingError == value) return;
                this._DeletingError = value;
                this.OnPropertyChanged("DeletingError");
            }
        }
        #endregion

        // TODO: Make thread-safe
        public RangeEnabledObservableCollection<DatabaseApplication> Applications = new RangeEnabledObservableCollection<DatabaseApplication>();
        public RangeEnabledObservableCollection<AppStoreStore> AppStoreStores = new RangeEnabledObservableCollection<AppStoreStore>();

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public Database() {

            this.PropertyChanged += Database_PropertyChanged;
            this.Applications.CollectionChanged += DatabaseApplications_CollectionChanged;
            this.AppStoreStores.CollectionChanged += AppStoreStores_CollectionChanged;
            this.JsonPatchInstance = new JsonPatch();
        }

        void AppStoreStores_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    foreach (AppStoreStore store in e.NewItems) {

                        //store.Database = this;

                        store.Changed -= AppStoreStore_Changed;
                        store.Changed += AppStoreStore_Changed;

                        this.UpdateAppStoreItems(store);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                    foreach (AppStoreStore item in e.OldItems) {
                        item.Changed -= AppStoreStore_Changed;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    IList<AppStoreStore> list = (IList<AppStoreStore>)sender;

                    // Remove listeners on the database instance
                    foreach (AppStoreStore item in list) {
                        item.Changed -= Application_Changed;
                        item.Changed += Application_Changed;
                        this.UpdateAppStoreItems(item);
                    }

                    break;
            }

            this.OnChanged(sender, e);
        }

        private void UpdateAppStoreItems(AppStoreStore store) {

            AppStoreManager.GetApplications(store, (remoteApplications) => {

                this.UpdateAppStoreApplications(store, remoteApplications);

            }, (errorMessage) => {

                this.OnCommandError("AppStore", errorMessage, null);
            });

        }

        void AppStoreStore_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// Called when an application has been added or removed from a database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DatabaseApplications_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    // Add listeners on the database instance
                    foreach (DatabaseApplication item in e.NewItems) {
                        item.Changed += Application_Changed;

                        // Connect AppStoreAppliction with DatabaseApplication (if available)
                        AppStoreApplication appStoreApplication = this.GetAppStoreApplication(item.SourceUrl);
                        if (appStoreApplication != null) {
                            appStoreApplication.DatabaseApplication = item;
                            appStoreApplication.UpdateUpgradeFlag();
                        }

                        item.InvalidateModel();
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                    // Remove listeners on the database instance
                    foreach (DatabaseApplication item in e.OldItems) {
                        item.Changed -= Application_Changed;

                        // Disconnect
                        foreach (AppStoreStore store in this.AppStoreStores) {
                            foreach (AppStoreApplication app in store.Applications) {
                                if (app.HasDatabaseAppliction && app.DatabaseApplication == item) {
                                    app.DatabaseApplication = null;
                                    app.UpdateUpgradeFlag();
                                }
                                else if (app.Namespace == item.Namespace && app.Channel == item.Channel) {
                                    app.UpdateUpgradeFlag();
                                }
                            }
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    IList<DatabaseApplication> list = (IList<DatabaseApplication>)sender;

                    // Remove listeners on the database instance
                    foreach (DatabaseApplication item in list) {
                        item.Changed -= Application_Changed;
                        item.Changed += Application_Changed;
                        AppStoreApplication appStoreApplication = this.GetAppStoreApplication(item.SourceUrl);
                        if (appStoreApplication != null) {
                            appStoreApplication.DatabaseApplication = item;
                            appStoreApplication.UpdateUpgradeFlag();
                        }

                        item.InvalidateModel();
                    }

                    // Remove listeners on the database instance
                    //foreach (DatabaseApplication item in this.Applications) {
                    //    item.Changed -= Application_Changed;
                    //    item.Changed += Application_Changed;
                    //}
                    break;
            }

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// Called when an application property has been changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Application_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// Called when an application property has been changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AppStoreApplication_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Database_PropertyChanged(object sender, PropertyChangedEventArgs e) {

            this.OnChanged(sender, e);

            if (e.PropertyName == "IsRunning") {
                if (this.IsRunning == true) {
                    this.OnStarted();
                }
                else {
                    this.OnStopped();
                }
            }
        }

        private void OnChanged(object sender, EventArgs e) {

            if (Changed != null) {
                Changed(sender, e);
            }
        }

        /// <summary>
        /// Event when database is started
        /// </summary>
        private void OnStarted() {

            // If the database was started by an app request we then also need to avaluate the app.
            // Database is not started and the app want to start, it first starts the database then the app
            //foreach (DatabaseApplication application in this.Applications) {
            //    application.Evaluate();
            //}

            // Playlist
            // TODO: Only auto-start apps if the database was started with the the administrator
            // This is due to a bug in the inner mechanism in combination with Visual Studio Starcounter Extention.
            //this.RunPlayList();
        }

        /// <summary>
        /// Start installed applications
        /// </summary>
        private void RunPlayList() {

            // Playlist
            foreach (DatabaseApplication application in this.Applications) {
                if (application.IsInstalled) {

                    application.StartApplication((startedApplication) => {
                        // TODO: Handle success
                    }, (startedApplication, wasCancelled, title, message, helpLink) => {
                        // TODO: Handle error
                    });
                }
            }
        }

        /// <summary>
        /// Event when database is stopped
        /// </summary>
        private void OnStopped() {

            this.InvalidateApplications();
        }

        #region Get

        /// <summary>
        /// Get Application
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DatabaseApplication GetApplication(string id) {

            foreach (DatabaseApplication dbApp in this.Applications) {

                if (dbApp.ID == id) {
                    return dbApp;
                }
            }
            return null;
        }


        /// <summary>
        /// Get Applications by namespace an channel
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public IList<DatabaseApplication> GetApplications(string nameSpace, string channel) {

            List<DatabaseApplication> result = new List<DatabaseApplication>();

            foreach (DatabaseApplication app in this.Applications) {

                if ((nameSpace.Equals('*') || string.Equals(app.Namespace, nameSpace, StringComparison.InvariantCultureIgnoreCase)) &&
                    (channel.Equals('*') || string.Equals(app.Channel, channel, StringComparison.InvariantCultureIgnoreCase))) {
                    result.Add(app);
                }
            }

            return result;
        }

        public DatabaseApplication GetApplication(string nameSpace, string channel, string version) {

            IList<DatabaseApplication> apps = this.GetApplications(nameSpace, channel);
            foreach (var app in apps) {

                if (string.Equals(app.Version, version, StringComparison.InvariantCultureIgnoreCase)) {
                    return app;
                }
            }
            return null;
        }

        public DatabaseApplication GetLatestApplication(string nameSpace, string channel) {

            DatabaseApplication latestApp = null;

            IList<DatabaseApplication> apps = this.GetApplications(nameSpace, channel);
            foreach (var app in apps) {

                if (latestApp == null || app.VersionDate > latestApp.VersionDate) {
                    latestApp = app;
                }
            }

            return latestApp;
        }


        /// <summary>
        /// Get Store
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private AppStoreStore GetStore(string id) {

            foreach (AppStoreStore store in this.AppStoreStores) {

                if (store.ID == id) {
                    return store;
                }
            }
            return null;
        }

        /// <summary>
        /// Get AppStore Application
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private AppStoreApplication GetAppStoreApplication(AppStoreStore store, string id) {

            foreach (AppStoreApplication dbApp in store.Applications) {

                if (dbApp.ID == id) {
                    return dbApp;
                }
            }
            return null;
        }

        /// <summary>
        /// Get Application
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DatabaseApplication GetApplicationByName(string name) {

            foreach (DatabaseApplication dbApp in this.Applications) {

                if (dbApp.AppName == name) {
                    return dbApp;
                }
            }
            return null;
        }

        /// <summary>
        /// Get Application
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        public DatabaseApplication GetApplicationBySourceUrl(string sourceUrl) {

            foreach (DatabaseApplication app in this.Applications) {
                // TODO: Use Uri.Compare?
                if (String.Equals(app.SourceUrl, sourceUrl, StringComparison.OrdinalIgnoreCase)) {
                    return app;
                }
            }
            return null;
        }

        /// <summary>
        /// Get loaded appstore application
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <returns></returns>
        private AppStoreApplication GetAppStoreApplication(string sourceUrl) {

            foreach (AppStoreStore store in this.AppStoreStores) {

                foreach (AppStoreApplication app in store.Applications) {
                    // TODO: USe Uri.Compare(
                    if (String.Equals(app.SourceUrl, sourceUrl, StringComparison.OrdinalIgnoreCase)) {
                        return app;
                    }
                }
            }
            return null;
        }

        #endregion

        #region Invalidate Model

        /// <summary>
        /// Called once a when a database is added to the server database list.
        /// </summary>
        public void InvalidateModel() {

            this.IsRunning = this.DatabaseRunningState();

            this.InvalidateApplications();
            this.InvalidateAppStoreStores();
        }

        /// <summary>
        /// Invalidate databases applications
        /// </summary>
        private void InvalidateApplications() {

            IList<DatabaseApplication> freshApplications;
            ApplicationManager.GetApplications(this.ID, out freshApplications);

            List<DatabaseApplication> addList = new List<DatabaseApplication>();

            foreach (DatabaseApplication freshApplication in freshApplications) {

                DatabaseApplication application = GetApplication(freshApplication.ID);
                if (application == null) {
                    this.Applications.Add(freshApplication);
                }
                else {

                    if (application.IsRunning != freshApplication.IsRunning) {
                        application.IsRunning = freshApplication.IsRunning;
                    }
                }
            }

            if (addList.Count > 0) {
                this.Applications.InsertRange(addList);
            }

            // Remove
            IList<DatabaseApplication> removeList = new List<DatabaseApplication>();
            foreach (DatabaseApplication application in this.Applications) {

                bool bExist = false;
                foreach (DatabaseApplication freshApplication in freshApplications) {

                    if (application.ID == freshApplication.ID) {
                        bExist = true;
                        break;
                    }
                }

                if (bExist == false) {
                    // add to remove list
                    removeList.Add(application);
                }
            }

            foreach (DatabaseApplication item in removeList) {
                this.Applications.Remove(item);
            }
        }

        /// <summary>
        /// Refresh appstore list
        /// </summary>
        public void RefreshAppStoreStores() {

            this.ResetErrorMessage();
            this.InvalidateAppStoreStores();
        }

        /// <summary>
        /// Invalidate app store applications
        /// </summary>
        private void InvalidateAppStoreStores() {


            //this.ResetErrorMessage();

            AppStoreManager.GetStores((freshStores) => {

                this.UpdateAppStoreList(freshStores);
            }, (errorMessage) => {

                this.OnCommandError("Retriving AppStore stores", errorMessage, null);
            });

        }

        public void InvalidateAppStoreStores(Action completionCallback = null, Action<string, string, string> errorCallback = null) {

            AppStoreManager.GetStores((freshStores) => {

                this.UpdateAppStoreList(freshStores);
                int counter = this.AppStoreStores.Count;
                foreach (AppStoreStore store in this.AppStoreStores) {

                    AppStoreManager.GetApplications(store, (remoteApplications) => {

                        this.UpdateAppStoreApplications(store, remoteApplications);

                        counter--;

                        if (counter == 0) {
                            if (completionCallback != null) {
                                completionCallback();
                            }
                        }
                    }, (errorMessage) => {

                        counter = -1;    // Only show one error message

                        this.OnCommandError("Retriving AppStore stores", errorMessage, null);

                        if (errorCallback != null) {
                            errorCallback("Retriving AppStore stores", errorMessage, null);
                        }
                    });
                }

            }, (errorMessage) => {

                this.OnCommandError("Retriving AppStore stores", errorMessage, null);

                if (errorCallback != null) {
                    errorCallback("Retriving AppStore stores", errorMessage, null);
                }

            });

        }

        private void UpdateAppStoreList(IList<AppStoreStore> freshStores) {

            lock (ServerManager.ServerInstance) {

                // Add new stores
                IList<AppStoreStore> addList = new List<AppStoreStore>();

                foreach (AppStoreStore freshStore in freshStores) {

                    AppStoreStore store = this.GetStore(freshStore.ID);
                    if (store == null) {
                        // Add store.
                        freshStore.Database = this;
                        addList.Add(freshStore);
                    }
                    else {
                        this.UpdateAppStoreItems(store);
                    }
                }

                //foreach (AppStoreStore freshStore in newStoresList) {
                //    freshStore.Database = this;
                //    this.AppStoreStores.Add(freshStore);
                //}

                if (addList.Count > 0) {
                    this.AppStoreStores.InsertRange(addList);
                }
                // Remove removed stores
                bool bExist;
                IList<AppStoreStore> removeStoresList = new List<AppStoreStore>();
                foreach (AppStoreStore store in this.AppStoreStores) {

                    bExist = false;

                    foreach (AppStoreStore freshStore in freshStores) {
                        if (store.ID == freshStore.ID) {
                            bExist = true;
                            break;
                        }
                    }

                    if (bExist == false) {
                        removeStoresList.Add(store);
                    }
                }

                foreach (AppStoreStore store in removeStoresList) {
                    this.AppStoreStores.Remove(store);
                }
            }
        }

        private void UpdateAppStoreApplications(AppStoreStore store, IList<AppStoreApplication> freshAppStoreApplications) {

            lock (ServerManager.ServerInstance) {
                // Add new stores
                List<AppStoreApplication> addList = new List<AppStoreApplication>();

                foreach (AppStoreApplication freshApp in freshAppStoreApplications) {

                    AppStoreApplication app = this.GetAppStoreApplication(store, freshApp.ID);
                    if (app == null) {
                        addList.Add(freshApp);
                        //store.Applications.Add(freshApp);
                    }
                    else {
                        // TODO: update app properties with freshApp properties
                    }
                }

                if (addList.Count > 0) {
                    store.Applications.InsertRange(addList);
                }

                // Remove removed stores
                bool bExist;
                IList<AppStoreApplication> removeList = new List<AppStoreApplication>();
                foreach (AppStoreApplication app in store.Applications) {

                    bExist = false;

                    foreach (AppStoreApplication freshApp in freshAppStoreApplications) {
                        if (app.ID == freshApp.ID) {
                            bExist = true;
                            break;
                        }
                    }

                    if (bExist == false) {
                        removeList.Add(app);
                    }
                }

                foreach (AppStoreApplication app in removeList) {
                    store.Applications.Remove(app);
                }
            }
        }

        #endregion

        #region Actions

        #region Start Database

        private ConcurrentStack<Action<Database>> DatabaseStartCallbacks = new ConcurrentStack<Action<Database>>();
        private ConcurrentStack<Action<Database, bool, string, string, string>> DatabaseStartErrorCallbacks = new ConcurrentStack<Action<Database, bool, string, string, string>>();

        /// <summary>
        /// Start database
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public void StartDatabase(Action<Database> completionCallback = null, Action<Database, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.DatabaseStartCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.DatabaseStartErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(DatabaseStatus.Starting)) {
                // Busy
                return;
            }

            if (this.IsRunning) {
                // Already running
                this.DatabaseStartErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DatabaseStartCallbacks);
                return;
            }

            this.StartingError = false;
            this.Status |= DatabaseStatus.Starting;

            // Create Command
            StartDatabaseCommand command;
            command = new StartDatabaseCommand(RootHandler.Host.Engine, this.ID);
            // FAKE ERROR
            //command = new StartDatabaseCommand(RootHandler.Host.Engine, "FAKEDATABASEID");
            command.EnableWaiting = false;
            command.NoDb = false;
            command.LogSteps = false;
            command.CodeHostCommandLineAdditions = string.Empty;

            this.ExecuteCommand(command, (database) => {

                this.Status &= ~DatabaseStatus.Starting;

                this.DatabaseStartErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DatabaseStartCallbacks);
            }, (database, wasCancelled, title, message, helpLink) => {

                this.Status &= ~DatabaseStatus.Starting;
                this.StartingError = true;

                this.OnCommandError(title, message, helpLink);

                this.DatabaseStartCallbacks.Clear();
                this.InvokeActionErrorListeners(this.DatabaseStartErrorCallbacks, wasCancelled, title, message, helpLink);
            });
        }

        #endregion

        #region Stop Database

        private ConcurrentStack<Action<Database>> DatabaseStopCallbacks = new ConcurrentStack<Action<Database>>();
        private ConcurrentStack<Action<Database, bool, string, string, string>> DatabaseStopErrorCallbacks = new ConcurrentStack<Action<Database, bool, string, string, string>>();

        /// <summary>
        /// Stop Database
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public void StopDatabase(Action<Database> completionCallback = null, Action<Database, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.DatabaseStopCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.DatabaseStopErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(DatabaseStatus.Stopping)) {
                // Busy
                return;
            }

            if (this.IsRunning == false) {
                // Already stopped
                this.DatabaseStopErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DatabaseStopCallbacks);
                return;
            }

            this.StoppingError = false;
            this.Status |= DatabaseStatus.Stopping;

            StopDatabaseCommand command;
            command = new StopDatabaseCommand(RootHandler.Host.Engine, this.ID, true);
            command.EnableWaiting = false;

            this.ExecuteCommand(command, (database) => {

                this.Status &= ~DatabaseStatus.Stopping;
                this.DatabaseStopErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DatabaseStopCallbacks);

            }, (database, wasCancelled, title, message, helpLink) => {

                this.Status &= ~DatabaseStatus.Stopping;
                this.StoppingError = true;
                this.OnCommandError(title, message, helpLink);
                this.DatabaseStopCallbacks.Clear();
                this.InvokeActionErrorListeners(this.DatabaseStopErrorCallbacks, wasCancelled, title, message, helpLink);
            });
        }

        #endregion

        #region Delete Database

        private ConcurrentStack<Action<Database>> DatabaseDeleteCallbacks = new ConcurrentStack<Action<Database>>();
        private ConcurrentStack<Action<Database, bool, string, string, string>> DatabaseDeleteErrorCallbacks = new ConcurrentStack<Action<Database, bool, string, string, string>>();

        /// <summary>
        /// Delete Database
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public void DeleteDatabase(Action<Database> completionCallback = null, Action<Database, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.DatabaseDeleteCallbacks.Push(completionCallback);
            }
            if (errorCallback != null) {
                this.DatabaseDeleteErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(DatabaseStatus.Deleting)) {
                // Busy
                return;
            }

            this.Status |= DatabaseStatus.Deleting; // Add status

            if (this.IsRunning == true) {
                // The database needs to be stopped before it can be deleted

                this.StopDatabase((database) => {

                    // Database Stopped
                    this.ExecuteDeleteDatabaseCommand();

                }, (database, wasCancelled, title, message, helpLink) => {

                    this.Status &= ~DatabaseStatus.Deleting;    // Remove status

                    // Failed to stop database, we can not delete database.
                    this.DatabaseDeleteCallbacks.Clear();
                    this.InvokeActionErrorListeners(this.DatabaseDeleteErrorCallbacks, wasCancelled, title, message, helpLink);
                });
            }
            else {
                this.ExecuteDeleteDatabaseCommand();
            }
        }

        /// <summary>
        /// Execute delete database command
        /// </summary>
        private void ExecuteDeleteDatabaseCommand() {

            this.DeletingError = false;

            DeleteDatabaseCommand command;
            command = new DeleteDatabaseCommand(RootHandler.Host.Engine, this.ID, true);
            command.EnableWaiting = false;

            this.ExecuteCommand(command, (database) => {

                this.Status &= ~DatabaseStatus.Deleting;

                ServerManager.ServerInstance.InvalidateDatabases();

                this.DatabaseDeleteErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DatabaseDeleteCallbacks);
            }, (database, wasCancelled, title, message, helpLink) => {

                this.Status &= ~DatabaseStatus.Deleting;
                this.DeletingError = true;
                this.OnCommandError(title, message, helpLink);

                this.DatabaseDeleteCallbacks.Clear();
                this.InvokeActionErrorListeners(this.DatabaseDeleteErrorCallbacks, wasCancelled, title, message, helpLink);
            });
        }

        #endregion

        /// <summary>
        /// Invoke action listeners
        /// </summary>
        /// <param name="listeners"></param>
        private void InvokeActionListeners(ConcurrentStack<Action<Database>> listeners) {

            while (listeners.Count > 0) {

                Action<Database> callback;
                if (listeners.TryPop(out callback)) {
                    callback(this);
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
        private void InvokeActionErrorListeners(ConcurrentStack<Action<Database, bool, string, string, string>> listeners, bool wasCancelled, string title, string message, string helpLink) {

            while (listeners.Count > 0) {

                Action<Database, bool, string, string, string> callback;
                if (listeners.TryPop(out callback)) {
                    callback(this, wasCancelled, title, message, helpLink);
                }
                else {
                    // TODO:
                    Console.WriteLine("TryPop() failed when it should have succeeded");
                }
            }
        }

        /// <summary>
        /// Execut Command (Start/Stop)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="database"></param>
        private void ExecuteCommand(ServerCommand command, Action<Database> completionCallback = null, Action<Database, bool, string, string, string> errorCallback = null) {

            var runtime = RootHandler.Host.Runtime;

            // Execute Command
            var c = runtime.Execute(command, (commandId) => {

                if (command is StartDatabaseCommand &&
                    (this.Status.HasFlag(DatabaseStatus.Stopping) ||
                    this.Status.HasFlag(DatabaseStatus.Deleting))) {

                    return true;    // return true to cancel
                }
                else if (command is StopDatabaseCommand && this.Status.HasFlag(DatabaseStatus.Starting)) {

                    return true;    // return true to cancel
                }
                else if (command is DeleteDatabaseCommand && this.Status.HasFlag(DatabaseStatus.Starting)) {

                    return true;    // return true to cancel
                }
                else if (command is CreateDatabaseCommand && this.Status != DatabaseStatus.Creating) {

                    return true;    // return true to cancel
                }

                return false;   // return true to cancel

                //if (command is DeleteDatabaseCommand) {
                //    return this.WantDeleted == this.IsDeleted;  // return true to cancel
                //}

                //return this.WantRunning == this.IsRunning;  // return true to cancel
            }, (commandId) => {

                lock (ServerManager.ServerInstance) {

                    CommandInfo commandInfo = runtime.GetCommand(commandId);

                    this.IsRunning = this.DatabaseRunningState();

                    if (this.IsRunning) {
                        this.RunPlayList();
                    }
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
                            errorCallback(this, wasCancelled, command.Description, msg.Brief, msg.Helplink);
                        }
                    }
                    else {

                        if (completionCallback != null) {
                            completionCallback(this);
                        }
                    }
                }
            });

            this.StatusText = c.Description;

            if (c.IsCompleted) {

                CommandInfo commandInfo = runtime.GetCommand(c.CorrelatedCommandId);

                this.IsRunning = this.DatabaseRunningState();
                if (this.IsRunning) {
                    this.RunPlayList();
                }

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
                        errorCallback(this, wasCancelled, command.Description, msg.Brief, msg.Helplink);
                    }
                }
                else {
                    if (completionCallback != null) {
                        completionCallback(this);
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
        /// Check if database is running
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        private bool DatabaseRunningState() {

            DatabaseInfo databaseInfo = Program.ServerInterface.GetDatabaseByName(this.ID);
            //DatabaseInfo databaseInfo = RootHandler.Host.Runtime.GetDatabase(this.Url);
            return (databaseInfo != null && databaseInfo.Engine != null && databaseInfo.Engine.DatabaseProcessRunning && databaseInfo.Engine.HostProcessId != 0);
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
    public enum DatabaseStatus : long {
        None = 0,
        Starting = 1,
        Stopping = 2,
        Creating = 4,
        Deleting = 16
    }
}
