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
            //set {
            //    if (this._ErrorMessage == value) return;
            //    this._ErrorMessage = value;
            //    this.OnPropertyChanged("ErrorMessage");
            //    this.OnPropertyChanged("HasErrorMessage");
            //}
        }

        public bool HasErrorMessage {
            get {
                return !(
                string.IsNullOrEmpty(this.ErrorMessage.Title) &&
                string.IsNullOrEmpty(this.ErrorMessage.Message) &&
                string.IsNullOrEmpty(this.ErrorMessage.HelpLink));


                //return this.ErrorMessage != null;
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

        private bool _WantRunning;
        public bool WantRunning {
            get {
                return this._WantRunning;
            }
            set {
                //if (this._WantRunning == value) return;
                this._WantRunning = value;

                // Reset error state
                this.CouldnotStart = false;
                this.CouldnotStop = false;

                this.OnPropertyChanged("WantRunning");
                this.Evaluate();
            }
        }

        private bool _WantDeleted;
        public bool WantDeleted {
            get {
                return this._WantDeleted;
            }
            set {
                this._WantDeleted = value;

                // Reset error state
                this.CouldNotDelete = false;

                this.OnPropertyChanged("WantDeleted");
                this.Evaluate();
            }
        }

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

        private bool _CouldnotStart;
        public bool CouldnotStart {
            get {
                return this._CouldnotStart;
            }
            set {
                if (this._CouldnotStart == value) return;
                this._CouldnotStart = value;
                this.OnPropertyChanged("CouldnotStart");
            }
        }

        private bool _CouldnotStop;
        public bool CouldnotStop {
            get {
                return this._CouldnotStop;
            }
            set {
                if (this._CouldnotStop == value) return;
                this._CouldnotStop = value;
                this.OnPropertyChanged("CouldnotStop");
            }
        }

        private bool _CouldNotDelete;
        public bool CouldNotDelete {
            get {
                return this._CouldNotDelete;
            }
            set {
                if (this._CouldNotDelete == value) return;
                this._CouldNotDelete = value;
                this.OnPropertyChanged("CouldNotDelete");
            }
        }


        //private bool _WantAppStoreStores;
        //public bool WantAppStoreStores {
        //    get {
        //        return this._WantAppStoreStores;
        //    }
        //    set {
        //        if (this._WantAppStoreStores == value) return;
        //        this._WantAppStoreStores = value;
        //        this.OnPropertyChanged("WantAppStoreStores");

        //        this.InvalidateAppStoreStores();  // Async
        //    }
        //}

        //private bool _CouldNotGetAppStoreStores;
        //public bool CouldNotGetAppStoreStores {
        //    get {
        //        return this._CouldNotGetAppStoreStores;
        //    }
        //    set {
        //        if (this._CouldNotGetAppStoreStores == value) return;
        //        this._CouldNotGetAppStoreStores = value;
        //        this.OnPropertyChanged("CouldNotGetAppStoreStores");
        //    }
        //}

        #endregion

        // TODO: Make thread-safe
        public ObservableCollection<DatabaseApplication> Applications = new ObservableCollection<DatabaseApplication>();
        public ObservableCollection<AppStoreStore> AppStoreStores = new ObservableCollection<AppStoreStore>();

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public Database() {

            this.PropertyChanged += Database_PropertyChanged;
            this.Applications.CollectionChanged += DatabaseApplications_CollectionChanged;
            //this.AppStoreApplications.CollectionChanged += AppStoreApplications_CollectionChanged;
            this.AppStoreStores.CollectionChanged += AppStoreStores_CollectionChanged;
            this.JsonPatchInstance = new JsonPatch();

        }

        void AppStoreStores_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    foreach (AppStoreStore store in e.NewItems) {

                        store.Changed -= AppStoreStore_Changed;
                        store.Changed += AppStoreStore_Changed;

                        this.UpdateAppStoreItems(store);

                        //AppStoreManager.GetApplications(this, item, (remoteApplications) => {

                        //    item.Applications.Clear();// TODO: Merge lists.

                        //    foreach (AppStoreApplication remoteApplication in remoteApplications) {
                        //        item.Applications.Add(remoteApplication);
                        //    }

                        //}, (errorMessage) => {

                        //    this.OnCommandError("AppStore", errorMessage, null);
                        //});

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

                    break;
            }

            this.OnChanged(sender, e);
        }

        private void UpdateAppStoreItems(AppStoreStore store) {

            AppStoreManager.GetApplications(this, store, (remoteApplications) => {

                this.UpdateAppStoreApplications(store, remoteApplications);

                //store.Applications.Clear();// TODO: Merge lists.

                //foreach (AppStoreApplication remoteApplication in remoteApplications) {
                //    store.Applications.Add(remoteApplication);
                //}

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
                        AppStoreApplication appStoreApplication = this.GetAppStoreApplication(item.SourceUrl);
                        if (appStoreApplication != null) {
                            appStoreApplication.DatabaseApplication = null;
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    // Remove listeners on the database instance
                    foreach (DatabaseApplication item in this.Applications) {
                        item.Changed -= Application_Changed;
                        item.Changed += Application_Changed;
                    }
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

            // If the database was started be an app request we then also need to avaluate the app.
            // Database is not started and the app want to start, it first starts the database then the app
            foreach (DatabaseApplication application in this.Applications) {
                application.Evaluate();
            }

            // Playlist
            // TODO: Only auto-start apps if the database was started with the the administrator
            // This is due to a bug in the inner mechanism in combination with Visual Studio Starcounter Extention.
            //this.RunPlayList();
        }

        private void RunPlayList() {

            // Playlist
            foreach (DatabaseApplication application in this.Applications) {
                if (application.IsInstalled) {
                    application.WantRunning = true;
                }
            }
        }

        /// <summary>
        /// Event when database is stopped
        /// </summary>
        private void OnStopped() {

            // DatabaseApplication has been stopped
            this.InvalidateApplications();
        }

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
                // TODO: USe Uri.Compare(
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

        #region Invalidate Model

        /// <summary>
        /// Called once a when a database is added to the server database list.
        /// </summary>
        public void InvalidateModel() {

            this.IsRunning = this.DatabaseRunningState();

            this.InvalidateApplications();
        }

        /// <summary>
        /// Invalidate databases applications
        /// </summary>
        private void InvalidateApplications() {

            IList<DatabaseApplication> freshApplications;
            ApplicationManager.GetApplications(this.ID, out freshApplications);

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
        /// Invalidate app store applications
        /// </summary>
        public void InvalidateAppStoreStores() {

            this.ResetErrorMessage();

            AppStoreManager.GetStores(this, (stores) => {

                this.UpdateAppStoreList(stores);
            }, (errorMessage) => {

                this.OnCommandError("AppStore", errorMessage, null);
            });
        }

        private void UpdateAppStoreList(IList<AppStoreStore> freshStores) {

            // Add new stores
            IList<AppStoreStore> newStoresList = new List<AppStoreStore>();

            foreach (AppStoreStore freshStore in freshStores) {

                AppStoreStore store = this.GetStore(freshStore.ID);
                if (store == null) {
                    // Add store.
                    newStoresList.Add(freshStore);
                }
                else {
                    this.UpdateAppStoreItems(store);
                }
            }

            foreach (AppStoreStore freshStore in newStoresList) {
                this.AppStoreStores.Add(freshStore);
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

        private void UpdateAppStoreApplications(AppStoreStore store, IList<AppStoreApplication> freshAppStoreApplications) {

            // Add new stores
            //IList<AppStoreApplication> newList = new List<AppStoreApplication>();

            foreach (AppStoreApplication freshApp in freshAppStoreApplications) {

                AppStoreApplication app = this.GetAppStoreApplication(store, freshApp.ID);
                if (app == null) {

                    store.Applications.Add(freshApp);
                }
                else {
                    // TODO: update app
                }
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


        #endregion

        /// <summary>
        /// Evaluate database and applications states agains wanted state
        /// </summary>
        public void Evaluate() {

            if (this.Status != DatabaseStatus.None) {
                // Work already in progres, when work is compleated it will call Evaluate()
                return;
            }

            if (this.WantRunning) {

                if (!this.CouldnotStart && this.IsRunning == false && !this.Status.HasFlag(DatabaseStatus.Starting)) {
                    this.StartDatabase();
                }
            }
            else {

                if (!this.CouldnotStop && this.IsRunning == true && !this.Status.HasFlag(DatabaseStatus.Stopping)) {
                    this.StopDatabase();
                }
            }

            if (this.Status != DatabaseStatus.None) {
                // Work already in progres, when work is compleated it will call Evaluate()
                return;
            }

            if (this.WantDeleted) {

                if (this.IsRunning || this.Status.HasFlag(DatabaseStatus.Starting)) {
                    this.WantRunning = false; // Will trigger Eveluate()
                }
                else {

                    if (this._CouldNotDelete == false && this.IsDeleted == false && !this.Status.HasFlag(DatabaseStatus.Deleting)) {
                        this.DeleteDatabase(); // Async
                    }
                }
            }
        }

        #region Actions

        /// <summary>
        /// Start database
        /// </summary>
        /// <param name="database"></param>
        private void StartDatabase() {

            this.ResetErrorMessage();

            // Create Command
            StartDatabaseCommand command;
            command = new StartDatabaseCommand(RootHandler.Host.Engine, this.ID);
            // FAKE ERROR
            //command = new StartDatabaseCommand(RootHandler.Host.Engine, "FAKEDATABASEID");
            command.EnableWaiting = false;
            command.NoDb = false;
            command.LogSteps = false;
            command.CodeHostCommandLineAdditions = string.Empty;

            this.RunDatabaseCommand(command);
        }

        /// <summary>
        /// Stop database
        /// </summary>
        /// <param name="database"></param>
        private void StopDatabase() {

            this.ResetErrorMessage();

            // Create Command
            StopDatabaseCommand command;
            command = new StopDatabaseCommand(RootHandler.Host.Engine, this.ID, true);
            command.EnableWaiting = false;

            this.RunDatabaseCommand(command);
        }

        /// <summary>
        /// Delete database
        /// </summary>
        /// <param name="database"></param>
        private void DeleteDatabase() {

            this.ResetErrorMessage();

            // Create Command
            DeleteDatabaseCommand command;
            command = new DeleteDatabaseCommand(RootHandler.Host.Engine, this.ID, true);
            command.EnableWaiting = false;

            this.RunDatabaseCommand(command);
        }

        /// <summary>
        /// Run Database Command (Start/Stop)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="database"></param>
        private void RunDatabaseCommand(ServerCommand command) {

            var runtime = RootHandler.Host.Runtime;

            if (command is StartDatabaseCommand) {
                this.Status |= DatabaseStatus.Starting;  // Add Status
            }
            else if (command is StopDatabaseCommand) {
                this.Status |= DatabaseStatus.Stopping;  // Add Status
            }
            else if (command is DeleteDatabaseCommand) {
                this.Status |= DatabaseStatus.Deleting;  // Add Status
            }

            // Execute Command
            var c = runtime.Execute(command, (commandId) => {

                if (command is DeleteDatabaseCommand) {
                    return this.WantDeleted == this.IsDeleted;  // return true to cancel
                }

                return this.WantRunning == this.IsRunning;  // return true to cancel
            }, (commandId) => {

                CommandInfo commandInfo = runtime.GetCommand(commandId);

                if (command is StartDatabaseCommand) {
                    this.CouldnotStart = commandInfo.HasError;
                    this.Status &= ~DatabaseStatus.Starting; // Remove status
                }
                else if (command is StopDatabaseCommand) {
                    this.CouldnotStop = commandInfo.HasError;
                    this.Status &= ~DatabaseStatus.Stopping; // Remove status
                }
                else if (command is DeleteDatabaseCommand) {
                    this.CouldNotDelete = commandInfo.HasError;
                    this.Status &= ~DatabaseStatus.Deleting; // Remove status
                    this.IsDeleted = !commandInfo.HasError;
                }

                this.IsRunning = this.DatabaseRunningState();

                if (this.IsRunning) {
                    this.RunPlayList();
                }

                this.StatusText = string.Empty;

                if (commandInfo.HasError) {
                    ErrorInfo single = commandInfo.Errors.PickSingleServerError();
                    var msg = single.ToErrorMessage();
                    this.OnCommandError(command.Description, msg.Brief, msg.Helplink);
                }
                else {
                    //                    this.StatusText = string.Empty;
                    this.Evaluate();
                }
            });

            this.StatusText = c.Description;

            if (c.IsCompleted) {

                if (command is StartExecutableCommand) {
                    this.CouldnotStart = c.HasError;
                    this.Status &= ~DatabaseStatus.Starting; // Remove status
                }
                else if (command is StopExecutableCommand) {
                    this.CouldnotStop = c.HasError;
                    this.Status &= ~DatabaseStatus.Stopping; // Remove status
                }
                else if (command is DeleteDatabaseCommand) {
                    this.CouldNotDelete = c.HasError;
                    this.Status &= ~DatabaseStatus.Deleting; // Remove status
                }

                if (c.HasError) {
                    ErrorInfo single = c.Errors.PickSingleServerError();
                    var msg = single.ToErrorMessage();
                    this.OnCommandError(command.Description, msg.Brief, msg.Helplink);
                }
            }
        }

        /// <summary>
        /// Set status text
        /// </summary>
        /// <param name="message"></param>
        private void OnCommandError(string title, string message, string helpLink) {

            // TODO: Append errors to notification list
            //this.StatusText = message;

            this.ErrorMessage.Title = title;
            this.ErrorMessage.Message = message;
            this.ErrorMessage.HelpLink = helpLink;

            this.OnPropertyChanged("ErrorMessage");
            this.OnPropertyChanged("HasErrorMessage");

            //            this.ErrorMessage = errorMessage;
        }

        #endregion

        private void ResetErrorMessage() {
            //            this.ErrorMessage = null;
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

            DatabaseInfo databaseInfo = RootHandler.Host.Runtime.GetDatabase(this.Url);
            return (databaseInfo != null && databaseInfo.Engine != null && databaseInfo.Engine.DatabaseProcessRunning);
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
