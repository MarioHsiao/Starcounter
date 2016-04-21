using Administrator.Server.Managers;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Model {

    public class AppStoreApplication : INotifyPropertyChanged {

        #region Properties
        public string ID;
        public string Namespace;
        public string Channel;
        public string DatabaseName {
            get {
                if (this.Database != null) {
                    return this.Database.ID;
                }
                return string.Empty;
            }
        }
        public string DisplayName;
        public string AppName {
            get {

                if (this.HasDatabaseAppliction) {
                    return this.DatabaseApplication.AppName;
                }

                return string.Empty;
            }
        }
        public string Description;
        public string Version;
        public DateTime VersionDate;
        public string ResourceFolder;
        public string Company;
        public string ImageUri;
        public string Executable;
        public string Arguments;

        public string SourceID;         // App Store item id
        public string SourceUrl;        // App Store item source

        public string StoreID;         // Store id
        public string StoreUrl;        // Store source

        public Dictionary<string, string> Dependencies;

        public string Heading;
        public long Rating;

        public bool IsDeployed {
            get {
                return this.HasDatabaseAppliction;
            }
        }
        public bool IsRunning {
            get {
                return this.HasDatabaseAppliction && this.DatabaseApplication.IsRunning;
            }
        }
        public bool IsInstalled {
            get {
                return this.HasDatabaseAppliction && this.DatabaseApplication.IsInstalled;
            }
        }
        public bool CanBeUninstalled {
            get {
                return this.HasDatabaseAppliction && this.DatabaseApplication.CanBeUninstalled;
            }
        }
        // Version exist (Is deployed) and versionDate is newer
        private bool _CanUpgrade;
        public bool CanUpgrade {
            get {
                return this._CanUpgrade;
            }
            set {
                if (this._CanUpgrade == value) return;
                this._CanUpgrade = value;
                this.OnPropertyChanged("CanUpgrade");
            }
        }

        private ApplicationStatus _Status;
        public ApplicationStatus Status {
            get {

                //if (this.HasDatabaseAppliction) {
                //    return this.DatabaseApplication.Status;
                //}

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

                //if (this.HasDatabaseAppliction) {
                //    return this.DatabaseApplication.StatusText;
                //}

                return this._StatusText;
            }
            set {
                if (this._StatusText == value) return;
                this._StatusText = value;
                this.OnPropertyChanged("StatusText");
            }
        }

        private ErrorMessage _ErrorMessage = new ErrorMessage();
        public ErrorMessage ErrorMessage {
            get {

                //if (this.HasDatabaseAppliction) {
                //    return this.DatabaseApplication.ErrorMessage;
                //}

                return this._ErrorMessage;
            }
        }

        public bool HasErrorMessage {
            get {

                //if (this.HasDatabaseAppliction) {
                //    return this.DatabaseApplication.HasErrorMessage;
                //}

                return !(
                string.IsNullOrEmpty(this._ErrorMessage.Title) &&
                string.IsNullOrEmpty(this._ErrorMessage.Message) &&
                string.IsNullOrEmpty(this._ErrorMessage.HelpLink));
            }
        }

        private bool _DeployError;
        public bool DeployError {
            get {
                return this._DeployError;
            }
            set {
                if (this._DeployError == value) return;
                this._DeployError = value;
                this.OnPropertyChanged("DeployError");
            }
        }

        private bool _DeleteError;
        public bool DeleteError {
            get {
                return this._DeleteError;
            }
            set {
                if (this._DeleteError == value) return;
                this._DeleteError = value;
                this.OnPropertyChanged("DeleteError");
            }
        }

        private bool _UpgradeError;
        public bool UpgradeError {
            get {
                return this._UpgradeError;
            }
            set {
                if (this._UpgradeError == value) return;
                this._UpgradeError = value;
                this.OnPropertyChanged("UpgradeError");
            }
        }

        public Database Database { get; set; }

        public bool HasDatabaseAppliction {
            get {
                return this.DatabaseApplication != null;
            }
        }

        private DatabaseApplication _DatabaseApplication;
        public DatabaseApplication DatabaseApplication {
            get {
                return this._DatabaseApplication;
            }
            internal set {
                if (this._DatabaseApplication != null) {
                    this._DatabaseApplication.Changed -= _DatabaseApplication_Changed;
                }

                // Remember old values
                bool oldHasDatabaseAppliction = this.HasDatabaseAppliction;
                bool oldIsRunning = this.IsRunning;
                bool oldIsInstalled = this.IsInstalled;
                bool oldCanBeUninstalled = this.CanBeUninstalled;
                bool oldIsDeployed = this.IsDeployed;

                DatabaseApplication oldDatabaseApplication = this.DatabaseApplication;

                this._DatabaseApplication = value;

                if (this._DatabaseApplication != null) {
                    this._DatabaseApplication.Changed += _DatabaseApplication_Changed;
                }

                // Send events if something changed
                if (oldHasDatabaseAppliction != this.HasDatabaseAppliction) {
                    this.OnPropertyChanged("HasDatabaseAppliction");
                }

                if (oldDatabaseApplication != this.DatabaseApplication) {
                    this.OnPropertyChanged("DatabaseApplication");
                }

                if (oldIsRunning != this.IsRunning) {
                    this.OnPropertyChanged("IsRunning");
                }

                if (oldIsInstalled != this.IsInstalled) {
                    this.OnPropertyChanged("IsInstalled");
                }
                if (oldCanBeUninstalled != this.CanBeUninstalled) {
                    this.OnPropertyChanged("CanBeUninstalled");
                }
                if (oldIsDeployed != this.IsDeployed) {
                    this.OnPropertyChanged("IsDeployed");
                }
            }
        }

        void _DatabaseApplication_Changed(object sender, EventArgs e) {

            if (sender is DatabaseApplication && e is PropertyChangedEventArgs) {

                if (((PropertyChangedEventArgs)e).PropertyName == "IsRunning") {
                    this.OnPropertyChanged("IsRunning");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "IsInstalled") {
                    this.OnPropertyChanged("IsInstalled");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "CanBeUninstalled") {
                    this.OnPropertyChanged("CanBeUninstalled");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "IsDeployed") {
                    this.OnPropertyChanged("IsDeployed");
                }
            }

            //this.OnChanged(sender, e);
        }

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public AppStoreApplication() {

            this.PropertyChanged += AppStoreApplication_PropertyChanged;
        }

        void AppStoreApplication_PropertyChanged(object sender, PropertyChangedEventArgs e) {

            this.OnChanged(sender, e);
        }

        private void OnChanged(object sender, EventArgs e) {

            if (Changed != null) {
                Changed(sender, e);
            }
        }

        #region Update Model
        public void UpdateModel() {

        }

        /// <summary>
        /// Called when: 
        /// * An application has been connected to an appstore application
        /// * An application has been disconnected from an appstore application (application deleted)
        /// * An appstore application has been added
        /// * An appstore application has been remove
        /// </summary>
        public void UpdateUpgradeFlag() {

            DatabaseApplication deployedApplication = this.Database.GetLatestApplication(this.Namespace, this.Channel);

            AppStoreStore store = this.GetApplicationStore();
            if (store != null) {
                IList<AppStoreApplication> appStoreApplications = store.GetAppStoreApplications(this.Namespace, this.Channel);
                foreach (AppStoreApplication item in appStoreApplications) {

                    item.CanUpgrade = deployedApplication != null && item.VersionDate > deployedApplication.VersionDate;
                }
            }
        }

        private AppStoreStore GetApplicationStore() {

            foreach (AppStoreStore store in this.Database.AppStoreStores) {

                foreach (AppStoreApplication app in store.Applications) {
                    // TODO: USe Uri.Compare(
                    if (String.Equals(app.SourceUrl, this.SourceUrl, StringComparison.OrdinalIgnoreCase)) {
                        return store;
                    }
                }
            }
            return null;
        }

        #endregion

        #region Actions

        #region DeployApplication
        private ConcurrentStack<Action<DatabaseApplication>> DeployApplicationCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> DeployApplicationErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Deploy application
        /// Download application from appstore and unpack it to the database
        /// NOTE: The application is not "Installed" auto-started.
        /// </summary>
        public void DeployApplication(Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.DeployApplicationCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.DeployApplicationErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Installing)) {
                // Busy
                return;
            }

            if (this.IsDeployed) {
                // Already deployed
                this.DeployApplicationErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DeployApplicationCallbacks);
                return;
            }

            this.DeployError = false;
            this.Status |= ApplicationStatus.Installing;

            DeployManager.Download(this.SourceUrl, this.Database, true, (application) => {

                this.Status &= ~ApplicationStatus.Installing;

                this.DeployApplicationErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DeployApplicationCallbacks);

            }, (message) => {

                this.Status &= ~ApplicationStatus.Installing;
                this.DeployError = true;
                this.OnCommandError("Deploy application", message, null);
                this.DeployApplicationCallbacks.Clear();
                this.InvokeActionErrorListeners(this.DeployApplicationErrorCallbacks, false, "Deploy application", message, null);
            });
        }

        #endregion

        #region DeleteApplication
        private ConcurrentStack<Action<DatabaseApplication>> DeleteApplicationCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> DeleteApplicationErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Delete application
        /// Delete deployed application from disk.
        /// </summary>
        public void DeleteApplication(Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.DeleteApplicationCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.DeleteApplicationErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Deleting)) {
                // Busy
                return;
            }

            if (!this.HasDatabaseAppliction) {

                this.DeleteError = true;
                this.DeleteApplicationErrorCallbacks.Clear();
                this.InvokeActionErrorListeners(this.DeleteApplicationErrorCallbacks, false, "Delete Application", "Could not find application", null);
                return;
            }

            if (this.IsDeployed == false) {
                // Not deployed
                this.DeleteApplicationErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DeleteApplicationCallbacks);
                return;
            }

            this.DeleteError = false;
            this.Status |= ApplicationStatus.Deleting;

            this.DatabaseApplication.DeleteApplication(false, (application) => {

                this.Status &= ~ApplicationStatus.Deleting;

                this.DeleteApplicationErrorCallbacks.Clear();
                this.InvokeActionListeners(this.DeleteApplicationCallbacks);

            }, (application, wasCanceled, title, message, helpLink) => {

                this.Status &= ~ApplicationStatus.Deleting;
                this.DeleteError = true;
                this.OnCommandError(title, message, helpLink);
                this.DeleteApplicationCallbacks.Clear();
                this.InvokeActionErrorListeners(this.DeleteApplicationErrorCallbacks, false, title, message, helpLink);
            });
        }

        #endregion

        #region UpgradeApplication
        private ConcurrentStack<Action<DatabaseApplication>> UpgradeApplicationCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> UpgradeApplicationErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Upgrade application
        /// 
        /// </summary>
        public void UpgradeApplication(DatabaseApplication currentDatabaseApplication, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.UpgradeApplicationCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.UpgradeApplicationErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Upgrading)) {
                // Busy
                return;
            }

            if (this.IsDeployed == true) {
                // Already deployed
                this.UpgradeApplicationErrorCallbacks.Clear();
                this.InvokeActionListeners(this.UpgradeApplicationCallbacks);
                return;
            }

            if (currentDatabaseApplication == null) {
                this.UpgradeError = true;
                this.OnCommandError("Upgrade Application", "Failed to find the application", null);
                this.UpgradeApplicationCallbacks.Clear();
                this.InvokeActionErrorListeners(this.UpgradeApplicationErrorCallbacks, false, "Upgrade Application", "Failed to find the application", null);
                return;
            }

            // Check store
            if (this.HasDatabaseAppliction && currentDatabaseApplication.StoreUrl != this.DatabaseApplication.StoreUrl) {
                // Can not upgrade applications from different sources
                this.UpgradeError = true;
                this.OnCommandError("Upgrade Application", "Can not upgrade applications from different stores", null);
                this.UpgradeApplicationCallbacks.Clear();
                this.InvokeActionErrorListeners(this.UpgradeApplicationErrorCallbacks, false, "Upgrade Application", "Can not upgrade applications from different stores", null);
                return;
            }

            this.UpgradeError = false;
            this.Status |= ApplicationStatus.Upgrading;

            bool wasRunning = currentDatabaseApplication.IsRunning;

            this.DeployApplication((deployedDatabaseApplication) => {

                this._Upgraded_Step_1_Stopping(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, (databaseApplication) => {

                    // Successfully upgrade application
                    this.OnUpgradeSuccess();

                }, (depoyedApplication, wasCanceled, title, message, helpLink) => {
                    // Error
                    this._Upgrade_Revert(currentDatabaseApplication, depoyedApplication, wasRunning, wasCanceled, title, message, helpLink);
                });

            }, (depoyedApplication, wasCanceled, title, message, helpLink) => {
                // Error

                // Failed to upgrade
                this._Upgrade_Revert(currentDatabaseApplication, depoyedApplication, wasRunning, wasCanceled, title, message, helpLink);
            });
        }

        private void _Upgraded_Step_1_Stopping(DatabaseApplication currentDatabaseApplication, DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (currentDatabaseApplication.IsRunning) {

                currentDatabaseApplication.StopApplication((stoppedCurrentApplication) => {

                    this._Upgraded_Step_2_Install(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);

                }, errorCallback);
            }
            else {
                this._Upgraded_Step_2_Install(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_2_Install(DatabaseApplication currentDatabaseApplication, DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (currentDatabaseApplication.IsInstalled) {

                deployedDatabaseApplication.InstallApplication((installedApplication) => {

                    this._Upgraded_Step_3_LockFlag(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
                }, errorCallback);
            }
            else {

                this._Upgraded_Step_3_LockFlag(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_3_LockFlag(DatabaseApplication currentDatabaseApplication, DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (currentDatabaseApplication.CanBeUninstalled != deployedDatabaseApplication.CanBeUninstalled) {

                deployedDatabaseApplication.SetCanBeUninstalledFlag(currentDatabaseApplication.CanBeUninstalled, (application) => {

                    this._Upgraded_Step_4_Restart(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);

                }, errorCallback);
            }
            else {
                this._Upgraded_Step_4_Restart(currentDatabaseApplication, deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_4_Restart(DatabaseApplication currentDatabaseApplication, DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (wasRunning) {
                deployedDatabaseApplication.StartApplication((depoyedApplication) => {

                    this._Upgraded_Step_5_Finalize(currentDatabaseApplication, deployedDatabaseApplication, completionCallback, errorCallback);

                }, errorCallback);
            }
            else {

                this._Upgraded_Step_5_Finalize(currentDatabaseApplication, deployedDatabaseApplication, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_5_Finalize(DatabaseApplication currentDatabaseApplication, DatabaseApplication deployedDatabaseApplication, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            // Delete previous version
            currentDatabaseApplication.DeleteApplication(true, completionCallback, errorCallback);
        }

        private void _Upgrade_Revert(DatabaseApplication currentDatabaseApplication, DatabaseApplication deployedDatabaseApplication, bool wasRunning, bool wasCanceled, string title, string message, string helpLink) {

            if (wasRunning) {
                // Restart the previous app again
                currentDatabaseApplication.StartApplication((databaseApplication) => {

                    // Delete downloaded app
                    deployedDatabaseApplication.DeleteApplication(true, (depoyedApplication2) => {
                        // Successfully reverted to previous application

                        this.OnUpgradeError(title, message, helpLink);
                    }, (depoyedApplication, wasCanceled2, title2, message2, helpLink2) => {

                        // Error, Failed to revert to previous state, Failed to remove downloaded application.
                        this.OnUpgradeError(title + ":" + title2, message + " " + message2, helpLink);
                    });

                }, (databaseApplication, wasCanceled2, title2, message2, helpLink2) => {

                    // Error, Failed to revert to previous state

                    // Delete downloaded app
                    deployedDatabaseApplication.DeleteApplication(true, (depoyedApplication2) => {

                        // Error, Failed to revert to previous state, Failed to restart previous application.
                        this.OnUpgradeError(title + ":" + title2, message + " " + message2, helpLink);
                    }, (depoyedApplication, wasCanceled3, title3, message3, helpLink3) => {

                        // Error, Failed to revert to previous state, Failed to remove downloaded application and to restart previous application.
                        this.OnUpgradeError(title + ":" + title3, message + " " + message3, helpLink);
                    });
                });
            }
            else {

                // Delete downloaded app
                deployedDatabaseApplication.DeleteApplication(true, (depoyedApplication2) => {

                    // Successfully reverted to previous application
                    this.OnUpgradeError(title, message, helpLink);
                }, (depoyedApplication, wasCanceled2, title2, message2, helpLink2) => {

                    // Error, Failed to revert to previous state, Failed to remove downloaded application
                    this.OnUpgradeError(title + ":" + title2, message + " " + message2, helpLink);
                });
            }
        }
        #endregion

        /// <summary>
        /// Called when upgrade was successfully executed
        /// </summary>
        private void OnUpgradeSuccess() {

            this.Status &= ~ApplicationStatus.Upgrading;
            this.UpgradeApplicationErrorCallbacks.Clear();
            this.InvokeActionListeners(this.UpgradeApplicationCallbacks);
        }

        /// <summary>
        /// Called when an upgrade failed
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="helpLink"></param>
        private void OnUpgradeError(string title, string message, string helpLink) {

            // TODO: If current app was stopped, then we need to restart it
            // TODO: If current app was uninstalled then we need to reinstalled it
            // TODO: If current app had the canbeuninstalled flag changed then we need to restore to previous value

            this.Status &= ~ApplicationStatus.Upgrading;
            this.UpgradeError = true;
            this.OnCommandError(title, message, helpLink);
            this.UpgradeApplicationCallbacks.Clear();
            this.InvokeActionErrorListeners(this.UpgradeApplicationErrorCallbacks, false, title, message, helpLink);
        }

        /// <summary>
        /// Invoke action listeners½
        /// </summary>
        /// <param name="listeners"></param>
        private void InvokeActionListeners(ConcurrentStack<Action<DatabaseApplication>> listeners) {

            while (listeners.Count > 0) {

                Action<DatabaseApplication> callback;
                if (listeners.TryPop(out callback)) {
                    callback(this.DatabaseApplication);
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
        private void InvokeActionErrorListeners(ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> listeners, bool wasCancelled, string title, string message, string helpLink) {

            while (listeners.Count > 0) {

                Action<DatabaseApplication, bool, string, string, string> callback;
                if (listeners.TryPop(out callback)) {
                    callback(this.DatabaseApplication, wasCancelled, title, message, helpLink);
                }
                else {
                    // TODO:
                    Console.WriteLine("TryPop() failed when it should have succeeded");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnCommandError(string title, string message, string helpLink) {

            this.ErrorMessage.Title = title;
            this.ErrorMessage.Message = message;
            this.ErrorMessage.HelpLink = helpLink;

            this.OnPropertyChanged("ErrorMessage");
            this.OnPropertyChanged("HasErrorMessage");
        }

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
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="applicationJson"></param>
        //public DatabaseApplicationJson ToDatabaseApplication() {

        //    DatabaseApplicationJson applicationJson = new DatabaseApplicationJson();
        //    applicationJson.ID = this.ID;
        //    applicationJson.IsDeployed = this.IsDeployed;
        //    applicationJson.DisplayName = this.DisplayName;
        //    applicationJson.Description = this.Description;
        //    applicationJson.Company = this.Company;
        //    applicationJson.Version = this.Version;
        //    applicationJson.VersionDate = this.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        //    applicationJson.DatabaseName = this.DatabaseName;
        //    applicationJson.ImageUri = string.IsNullOrEmpty(this.ImageUri) ? string.Empty : string.Format("{0}/{1}", DeployManager.GetAppImagesFolder(), this.ImageUri); // Use default image?
        //    applicationJson.CanBeUninstalled = this.CanBeUninstalled;

        //    return applicationJson;
        //}

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }

        #endregion
    }
}
