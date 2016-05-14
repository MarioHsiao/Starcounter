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

    public class DatabaseApplication : INotifyPropertyChanged {

        #region Properties
        public string ID;
        public string Namespace;
        public string Channel;
        public string DatabaseName
        {
            get
            {
                if (this.Database != null) {
                    return this.Database.ID;
                }
                return string.Empty;
            }
        }
        public string DisplayName;
        public string AppName;
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

        public string Heading;

        private bool _IsDeployed;
        public bool IsDeployed
        {
            get
            {
                return this._IsDeployed;
            }
            set
            {
                if (this._IsDeployed == value) return;
                this._IsDeployed = value;
                this.OnPropertyChanged("IsDeployed");
            }
        }

        private bool _IsInstalled;
        public bool IsInstalled
        {
            get
            {
                return this._IsInstalled;
            }
            set
            {
                if (this._IsInstalled == value) return;
                this._IsInstalled = value;
                this.OnPropertyChanged("IsInstalled");
            }
        }

        private bool _CanBeUninstalled;
        public bool CanBeUninstalled
        {
            get
            {
                return this._CanBeUninstalled;
            }
            set
            {
                if (this._CanBeUninstalled == value) return;
                this._CanBeUninstalled = value;
                this.OnPropertyChanged("CanBeUninstalled");
            }
        }

        private ApplicationStatus _Status;
        public ApplicationStatus Status
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

        private ErrorMessage _ErrorMessage = new ErrorMessage();
        public ErrorMessage ErrorMessage
        {
            get
            {
                return this._ErrorMessage;
            }
            //set {
            //    if (this._ErrorMessage == value) return;
            //    this._ErrorMessage = value;
            //    this.OnPropertyChanged("ErrorMessage");
            //    this.OnPropertyChanged("HasErrorMessage");
            //}
        }

        public bool HasErrorMessage
        {
            get
            {
                return !(
                string.IsNullOrEmpty(this.ErrorMessage.Title) &&
                string.IsNullOrEmpty(this.ErrorMessage.Message) &&
                string.IsNullOrEmpty(this.ErrorMessage.HelpLink));


                //return this.ErrorMessage != null;
            }
        }

        #region Running
        private bool _IsRunning;
        public bool IsRunning
        {
            get
            {
                return this._IsRunning;
            }
            set
            {
                if (this._IsRunning == value) return;
                this._IsRunning = value;
                this.OnPropertyChanged("IsRunning");
            }
        }

        private bool _StartingError;
        public bool StartingError
        {
            get
            {
                return this._StartingError;
            }
            set
            {
                if (this._StartingError == value) return;
                this._StartingError = value;
                this.OnPropertyChanged("StartingError");
            }
        }

        private bool _StoppingError;
        private bool StoppingError
        {
            get
            {
                return this._StoppingError;
            }
            set
            {
                if (this._StoppingError == value) return;
                this._StoppingError = value;
                this.OnPropertyChanged("StoppingError");
            }
        }
        #endregion

        private bool _InstallingError;
        public bool InstallingError
        {
            get
            {
                return this._InstallingError;
            }
            set
            {
                if (this._InstallingError == value) return;
                this._InstallingError = value;
                this.OnPropertyChanged("InstallingError");
            }
        }

        private bool _UpgradingError;
        public bool UpgradingError
        {
            get
            {
                return this._UpgradingError;
            }
            set
            {
                if (this._UpgradingError == value) return;
                this._UpgradingError = value;
                this.OnPropertyChanged("UpgradingError");
            }
        }

        private bool _UninstallingError;
        public bool UninstallingError
        {
            get
            {
                return this._UninstallingError;
            }
            set
            {
                if (this._UninstallingError == value) return;
                this._UninstallingError = value;
                this.OnPropertyChanged("UninstallingError");
            }
        }

        private bool _DeletingError;
        public bool DeletingError
        {
            get
            {
                return this._DeletingError;
            }
            set
            {
                if (this._DeletingError == value) return;
                this._DeletingError = value;
                this.OnPropertyChanged("DeletingError");
            }
        }

        public Database Database { get; set; }

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public DatabaseApplication() {

            this.PropertyChanged += DatabaseApplication_PropertyChanged;
        }

        /// <summary>
        /// Called when a property has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DatabaseApplication_PropertyChanged(object sender, PropertyChangedEventArgs e) {

            this.OnChanged(sender, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, EventArgs e) {

            if (Changed != null) {
                Changed(sender, e);
            }
        }

        #region Update Model
        public void InvalidateModel() {

            this.IsRunning = this.ApplicationRunningState();
        }

        #endregion

        #region Actions

        #region Install
        private ConcurrentStack<Action<DatabaseApplication>> ApplicationInstallCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> ApplicationInstallErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Install application
        /// Will make the application start along with the database startup
        /// </summary>
        public void InstallApplication(Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.ApplicationInstallCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.ApplicationInstallErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Installing)) {
                // Busy
                return;
            }

            if (this.IsInstalled) {
                // Already installed
                this.ApplicationInstallErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationInstallCallbacks);
                return;
            }

            this.InstallingError = false;
            this.Status |= ApplicationStatus.Installing;

            try {
                // TODO: Make ApplicationManager.InstallApplication call async
                ApplicationManager.InstallApplication(this);
                this.Status &= ~ApplicationStatus.Installing;

                this.ApplicationInstallErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationInstallCallbacks);
            }
            catch (Exception e) {

                this.Status &= ~ApplicationStatus.Installing;
                this.InstallingError = true;
                this.OnCommandError("Install Application", e.Message, null);
                this.ApplicationInstallCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationInstallErrorCallbacks, false, "Install Application", e.Message, null);
            }
        }

        #endregion

        #region Upgrade

        private ConcurrentStack<Action<DatabaseApplication>> ApplicationUpgradeCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> ApplicationUpgradeErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        public void UpgradeApplication(DatabaseApplication deployedDatabaseApplication, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.ApplicationUpgradeCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.ApplicationUpgradeErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Upgrading)) {
                // Busy
                return;
            }

            if (this == deployedDatabaseApplication) {
                this.ApplicationUpgradeErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationUpgradeCallbacks);
                return;
            }

            // TODO: can upgrade self
            bool wasRunning = this.IsRunning;

            //if (this.StoreUrl != deployedDatabaseApplication.StoreUrl) {
            //    if (errorCallback != null) {
            //        errorCallback(deployedDatabaseApplication, false, "Upgrade Application", "Can not upgrade applications from different stores", null);
            //    }
            //    return;
            //}

            this.UpgradingError = false;
            this.Status |= ApplicationStatus.Upgrading;
            this.UpgradingError = false;

            this._Upgraded_Step_1_Stopping(deployedDatabaseApplication, wasRunning, (databaseApplication) => {

                // Success
                this.Status &= ~ApplicationStatus.Upgrading;

                this.ApplicationUpgradeErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationUpgradeCallbacks);

            }, (depoyedApplication, wasCanceled, title, message, helpLink) => {

                this.UpgradingError = true;

                // Error, Failed to revert to previous state, Failed to remove downloaded application.
                this._Upgrade_Revert(depoyedApplication, wasRunning, wasCanceled, title, message, helpLink, (depoyedApplication2, wasCanceled2, title2, message2, helpLink2) => {
                    // Error reverting

                    this.Status &= ~ApplicationStatus.Upgrading;    // Remove status

                    // Failed to stop database, we can not delete database.
                    this.ApplicationUpgradeCallbacks.Clear();
                    this.InvokeActionErrorListeners(this.ApplicationUpgradeErrorCallbacks, wasCanceled, title2, message2, helpLink2);
                });
            });
        }

        private void _Upgraded_Step_1_Stopping(DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (this.IsRunning) {

                this.StopApplication((stoppedCurrentApplication) => {

                    this._Upgraded_Step_2_Install(deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);

                }, errorCallback);
            }
            else {
                this._Upgraded_Step_2_Install(deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_2_Install(DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (this.IsInstalled) {

                deployedDatabaseApplication.InstallApplication((installedApplication) => {

                    this._Upgraded_Step_3_LockFlag(deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
                }, errorCallback);
            }
            else {

                this._Upgraded_Step_3_LockFlag(deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_3_LockFlag(DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (this.CanBeUninstalled != deployedDatabaseApplication.CanBeUninstalled) {

                deployedDatabaseApplication.SetCanBeUninstalledFlag(this.CanBeUninstalled, (application) => {

                    this._Upgraded_Step_4_Restart(deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);

                }, errorCallback);
            }
            else {
                this._Upgraded_Step_4_Restart(deployedDatabaseApplication, wasRunning, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_4_Restart(DatabaseApplication deployedDatabaseApplication, bool wasRunning, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (wasRunning) {
                deployedDatabaseApplication.StartApplication((depoyedApplication) => {

                    this._Upgraded_Step_5_Finalize(deployedDatabaseApplication, completionCallback, errorCallback);

                }, errorCallback);
            }
            else {

                this._Upgraded_Step_5_Finalize(deployedDatabaseApplication, completionCallback, errorCallback);
            }
        }

        private void _Upgraded_Step_5_Finalize(DatabaseApplication deployedDatabaseApplication, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            // Delete previous version
            this.DeleteApplication(true, (databaseApplication) => {

                if (completionCallback != null) {
                    completionCallback(deployedDatabaseApplication);
                }

            }, errorCallback);
        }

        private void _Upgrade_Revert(DatabaseApplication deployedDatabaseApplication, bool wasRunning, bool wasCanceled, string title, string message, string helpLink, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            if (wasRunning) {
                // Restart the previous app again
                this.StartApplication((databaseApplication) => {

                    // Delete downloaded app
                    deployedDatabaseApplication.DeleteApplication(true, (depoyedApplication) => {
                        // Successfully reverted to previous application
                        if (errorCallback != null) {
                            errorCallback(depoyedApplication, wasRunning, title, message, helpLink);
                        }
                    }, errorCallback);

                }, (databaseApplication, wasCanceled2, title2, message2, helpLink2) => {

                    // Error, Failed to revert to previous state

                    // Delete downloaded app
                    deployedDatabaseApplication.DeleteApplication(true, (depoyedApplication) => {

                        // Error, Failed to revert to previous state, Failed to restart previous application.
                        if (errorCallback != null) {
                            errorCallback(depoyedApplication, wasRunning, title + ":" + title2, message + " " + message2, helpLink);
                        }
                    }, errorCallback);
                });
            }
            else {

                // Delete downloaded app
                deployedDatabaseApplication.DeleteApplication(true, (depoyedApplication2) => {

                    // Successfully reverted to previous application
                    if (errorCallback != null) {
                        errorCallback(depoyedApplication2, wasRunning, title, message, helpLink);
                    }

                }, (depoyedApplication, wasCanceled2, title2, message2, helpLink2) => {

                    // Error, Failed to revert to previous state, Failed to remove downloaded application
                    if (errorCallback != null) {
                        errorCallback(depoyedApplication, wasRunning, title + ":" + title2, message + " " + message2, helpLink);
                    }
                });
            }
        }
        #endregion

        #region Uninstall
        private ConcurrentStack<Action<DatabaseApplication>> ApplicationUninstallCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> ApplicationUninstallErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Uninstall application
        /// Application will not be started along with the database startup
        /// </summary>
        public void UninstallApplication(Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.ApplicationUninstallCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.ApplicationUninstallErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Uninstalling)) {
                // Busy
                return;
            }

            if (this.IsInstalled == false) {
                // Already uninstalled
                this.ApplicationUninstallErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationUninstallCallbacks);
                return;
            }

            this.UninstallingError = false;
            this.Status |= ApplicationStatus.Uninstalling;

            try {
                // TODO: Make ApplicationManager.UninstallApplication call async
                ApplicationManager.UninstallApplication(this);
                this.Status &= ~ApplicationStatus.Uninstalling;

                this.ApplicationUninstallErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationUninstallCallbacks);
            }
            catch (Exception e) {

                this.Status &= ~ApplicationStatus.Uninstalling;
                this.UninstallingError = true;
                this.OnCommandError("Uninstall Application", e.Message, null);
                this.ApplicationUninstallCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationUninstallErrorCallbacks, false, "Uninstall Application", e.Message, null);
            }

            //try {
            //    this.Status |= ApplicationStatus.Uninstalling;
            //    ApplicationManager.UninstallApplication(this);
            //    this.Status &= ~ApplicationStatus.Uninstalling;

            //    if (completionCallback != null) {
            //        completionCallback(this);
            //    }

            //}
            //catch (Exception e) {
            //    this.Status &= ~ApplicationStatus.Uninstalling;
            //    this.CouldNotUninstall = true;
            //    //this.OnCommandError("Uninstall Application", e.Message, null);
            //    if (errorCallback != null) {
            //        errorCallback("Install Application", e.Message, null);
            //    }

            //}
        }
        #endregion

        #region Delete
        private ConcurrentStack<Action<DatabaseApplication>> ApplicationDeleteCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> ApplicationDeleteErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Delete deployed application from server
        /// TODO: Also Uninstall application if it's installed
        /// </summary>
        public void DeleteApplication(bool forceDelete, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.ApplicationDeleteCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.ApplicationDeleteErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Deleting)) {
                // Busy
                return;
            }

            this.Status |= ApplicationStatus.Deleting;

            if (this.IsRunning) {

                this.StopApplication((application) => {

                    // Application stopped
                    this.ExecuteDeleteApplicationCommand(forceDelete);

                }, (application, wasCancelled, title, message, helpLink) => {

                    this.Status &= ~ApplicationStatus.Deleting;    // Remove status

                    // Failed to stop database, we can not delete database.
                    this.ApplicationDeleteCallbacks.Clear();
                    this.InvokeActionErrorListeners(this.ApplicationDeleteErrorCallbacks, wasCancelled, title, message, helpLink);
                });

            }
            else {
                this.ExecuteDeleteApplicationCommand(forceDelete);
            }


        }

        /// <summary>
        /// Execute delete application command
        /// </summary>
        private void ExecuteDeleteApplicationCommand(bool forceDelete) {

            this.DeletingError = false;

            DeployManager.Delete(this, forceDelete, (application) => {

                this.Status &= ~ApplicationStatus.Deleting;

                this.ApplicationDeleteErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationDeleteCallbacks);

            }, (message) => {

                this.Status &= ~ApplicationStatus.Deleting;
                this.DeletingError = true;
                this.OnCommandError("Delete Application", message, null);
                this.ApplicationDeleteCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationDeleteErrorCallbacks, false, "Delete Application", message, null);
            });

            //this.StoppingError = false;

            //DatabaseApplication app = ApplicationManager.GetApplication(this.DatabaseName, this.ID);
            //if (app == null) {

            //    this.StartingError = true;
            //    this.ApplicationStartErrorCallbacks.Clear();
            //    this.InvokeActionErrorListeners(this.ApplicationStartErrorCallbacks, false, "Start Application", "Could not find application", null);
            //    return;
            //}

            //string[] arguments = new string[0];
            //// TODO: Use arguments from application (String need to be split to array
            //AppInfo appinfo = new AppInfo(app.AppName, app.Executable, app.Executable, app.ResourceFolder, arguments, "");

            //// Create Command
            //StartExecutableCommand command;
            //command = new StartExecutableCommand(RootHandler.Host.Engine, this.DatabaseName, appinfo);
            //command.EnableWaiting = false;
            //command.RunEntrypointAsynchronous = false;  // ?

            //this.ExecuteCommand(command, (database) => {

            //    this.Status &= ~ApplicationStatus.Starting;

            //    this.ApplicationStartErrorCallbacks.Clear();
            //    this.InvokeActionListeners(this.ApplicationStartCallbacks);
            //}, (database, wasCancelled, title, message, helpLink) => {

            //    this.Status &= ~ApplicationStatus.Starting;
            //    this.StartingError = true;

            //    this.OnCommandError(title, message, helpLink);

            //    this.ApplicationStartCallbacks.Clear();
            //    this.InvokeActionErrorListeners(this.ApplicationStartErrorCallbacks, wasCancelled, title, message, helpLink);
            //});
        }

        #endregion

        #region Start Application
        private ConcurrentStack<Action<DatabaseApplication>> ApplicationStartCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> ApplicationStartErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Start Application
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public void StartApplication(Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.ApplicationStartCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.ApplicationStartErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Starting)) {
                // Busy
                return;
            }

            if (this.IsRunning) {
                // Already running
                this.ApplicationStartErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationStartCallbacks);
                return;
            }

            this.Status |= ApplicationStatus.Starting;

            if (this.Database.IsRunning == false) {

                this.Database.StartDatabase((database) => {

                    // Database started
                    this.ExecuteStartApplicationCommand();

                }, (database, wasCancelled, title, message, helpLink) => {

                    this.Status &= ~ApplicationStatus.Starting;    // Remove status

                    // Failed to stop database, we can not delete database.
                    this.ApplicationStartCallbacks.Clear();
                    this.InvokeActionErrorListeners(this.ApplicationStartErrorCallbacks, wasCancelled, title, message, helpLink);
                });

            }
            else {
                this.ExecuteStartApplicationCommand();
            }
        }

        /// <summary>
        /// Execute Start application command
        /// </summary>
        private void ExecuteStartApplicationCommand() {

            this.StartingError = false;

            DatabaseApplication app = ApplicationManager.GetApplication(this.DatabaseName, this.ID);
            if (app == null) {

                this.StartingError = true;
                this.ApplicationStartErrorCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationStartErrorCallbacks, false, "Start Application", "Could not find application", null);
                return;
            }

            string[] arguments = new string[0];
            // TODO: Use arguments from application (String need to be split to array
            AppInfo appinfo = new AppInfo(app.AppName, app.Executable, app.Executable, Path.GetDirectoryName(app.Executable), arguments, "");
            if (!string.IsNullOrEmpty(app.ResourceFolder)) {
                appinfo.ResourceDirectories.Add(app.ResourceFolder);
            }

            // Create Command
            StartExecutableCommand command;
            command = new StartExecutableCommand(RootHandler.Host.Engine, this.DatabaseName, appinfo);
            command.EnableWaiting = false;
            command.RunEntrypointAsynchronous = false;  // ?

            this.ExecuteCommand(command, (database) => {

                this.Status &= ~ApplicationStatus.Starting;

                this.ApplicationStartErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationStartCallbacks);
            }, (database, wasCancelled, title, message, helpLink) => {

                this.Status &= ~ApplicationStatus.Starting;
                this.StartingError = true;

                this.OnCommandError(title, message, helpLink);

                this.ApplicationStartCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationStartErrorCallbacks, wasCancelled, title, message, helpLink);
            });
        }

        #endregion

        #region Stop Application

        private ConcurrentStack<Action<DatabaseApplication>> ApplicationStopCallbacks = new ConcurrentStack<Action<DatabaseApplication>>();
        private ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> ApplicationStopErrorCallbacks = new ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>>();

        /// <summary>
        /// Stop Database
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public void StopApplication(Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            if (completionCallback != null) {
                this.ApplicationStopCallbacks.Push(completionCallback);
            }

            if (errorCallback != null) {
                this.ApplicationStopErrorCallbacks.Push(errorCallback);
            }

            if (this.Status.HasFlag(ApplicationStatus.Stopping)) {
                // Busy
                return;
            }

            if (this.IsRunning == false) {
                // Already stopped
                this.ApplicationStopErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationStopCallbacks);
                return;
            }

            DatabaseApplication app = ApplicationManager.GetApplication(this.DatabaseName, this.ID);
            if (app == null) {

                this.StoppingError = true;
                this.ApplicationStopErrorCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationStopErrorCallbacks, false, "Stop Application", "Could not find application", null);
                return;
            }

            this.StoppingError = false;
            this.Status |= ApplicationStatus.Stopping;

            // We can not stop the application by using the appName, we need to use the App Key or Executable full path.
            // There is an flaw when using the Executable full path for stopping apps. (multiple apps can have the same filename but not the same appname)
            AppInfo appInfo = this.GetApplicationAppInfo();
            string id;
            if (appInfo != null) {
                id = appInfo.Key;
            }
            else {
                id = app.Executable;
            }

            var command = new StopExecutableCommand(RootHandler.Host.Engine, app.DatabaseName, id);
            command.EnableWaiting = false;

            this.ExecuteCommand(command, (database) => {

                this.Status &= ~ApplicationStatus.Stopping;
                this.ApplicationStopErrorCallbacks.Clear();
                this.InvokeActionListeners(this.ApplicationStopCallbacks);

            }, (database, wasCancelled, title, message, helpLink) => {

                this.Status &= ~ApplicationStatus.Stopping;
                this.StoppingError = true;
                this.OnCommandError(title, message, helpLink);
                this.ApplicationStopCallbacks.Clear();
                this.InvokeActionErrorListeners(this.ApplicationStopErrorCallbacks, wasCancelled, title, message, helpLink);
            });
        }

        #endregion

        #region Set CanBeUninstalled Flag

        public void SetCanBeUninstalledFlag(bool status, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            this.ResetErrorMessage();

            DeployedConfigFile config = DeployManager.GetItemFromApplication(this);

            if (config == null) {
                if (errorCallback != null) {
                    errorCallback(this, false, "Setting uninstallation lock", "Failed to find application configuration file", null);
                }
            }

            try {
                config.CanBeUninstalled = status;
                config.Save();

                this.CanBeUninstalled = config.CanBeUninstalled;

                if (completionCallback != null) {
                    completionCallback(this);
                }

            }
            catch (Exception e) {
                if (errorCallback != null) {
                    errorCallback(this, false, "Setting uninstallation lock", e.Message, null);
                }
            }


        }
        #endregion

        /// <summary>
        /// Invoke action listeners½
        /// </summary>
        /// <param name="listeners"></param>
        private void InvokeActionListeners(ConcurrentStack<Action<DatabaseApplication>> listeners) {

            while (listeners.Count > 0) {

                Action<DatabaseApplication> callback;
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
        private void InvokeActionErrorListeners(ConcurrentStack<Action<DatabaseApplication, bool, string, string, string>> listeners, bool wasCancelled, string title, string message, string helpLink) {

            while (listeners.Count > 0) {

                Action<DatabaseApplication, bool, string, string, string> callback;
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
        private void ExecuteCommand(ServerCommand command, Action<DatabaseApplication> completionCallback = null, Action<DatabaseApplication, bool, string, string, string> errorCallback = null) {

            var runtime = RootHandler.Host.Runtime;

            // Execute Command
            var c = runtime.Execute(command, (commandId) => {

                if (command is StartExecutableCommand &&
                    (this.Status.HasFlag(ApplicationStatus.Stopping) ||
                    this.Status.HasFlag(ApplicationStatus.Deleting))) {

                    return true;    // return true to cancel
                }
                else if (command is StopExecutableCommand && this.Status.HasFlag(ApplicationStatus.Starting)) {

                    return true;    // return true to cancel
                }


                return false;   // return true to cancel


            }, (commandId) => {

                CommandInfo commandInfo = runtime.GetCommand(commandId);

                this.IsRunning = this.ApplicationRunningState();

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
            });

            this.StatusText = c.Description;

            if (c.IsCompleted) {

                CommandInfo commandInfo = runtime.GetCommand(c.CorrelatedCommandId);

                this.IsRunning = this.ApplicationRunningState();
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
        /// 
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
        /// <param name="application"></param>
        /// <returns></returns>
        private bool ApplicationRunningState() {

            return this.GetApplicationAppInfo() != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private AppInfo GetApplicationAppInfo() {

            // Get running apps
            DatabaseInfo databaseInfo = RootHandler.Host.Runtime.GetDatabaseByName(this.DatabaseName);

            if (databaseInfo == null ||
                databaseInfo.Engine == null ||
                databaseInfo.Engine.HostProcessId == 0 ||
                databaseInfo.Engine.HostedApps == null)
                return null;

            foreach (AppInfo appInfo in databaseInfo.Engine.HostedApps) {

                if (string.Equals(Path.GetFullPath(appInfo.FilePath), this.Executable, StringComparison.CurrentCultureIgnoreCase)) {
                    return appInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="applicationJson"></param>
        public DatabaseApplicationJson ToDatabaseApplication() {

            DatabaseApplicationJson applicationJson = new DatabaseApplicationJson();
            applicationJson.ID = this.ID;
            applicationJson.IsDeployed = this.IsDeployed;
            applicationJson.IsRunning = this.IsRunning;
            applicationJson.IsInstalled = this.IsInstalled;
            applicationJson.DisplayName = this.DisplayName;
            applicationJson.Description = this.Description;
            applicationJson.Company = this.Company;
            applicationJson.Namespace = this.Namespace;
            applicationJson.Channel = this.Channel;
            applicationJson.Version = this.Version;
            applicationJson.VersionDate = this.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            applicationJson.DatabaseName = this.DatabaseName;
            applicationJson.ImageUri = string.IsNullOrEmpty(this.ImageUri) ? string.Empty : string.Format("{0}/{1}", DeployManager.GetAppImagesFolder(), this.ImageUri); // Use default image?
            applicationJson.CanBeUninstalled = this.CanBeUninstalled;
            return applicationJson;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static DatabaseApplication ToApplication(Representations.JSON.PlayListLocalItem item, string databaseName) {

            var versionInfo = FileVersionInfo.GetVersionInfo(item.Executable);

            Database database = ServerManager.ServerInstance.GetDatabase(databaseName);

            Trace.Assert(database != null, "Database not found: " + databaseName);

            DatabaseApplication application = new DatabaseApplication();
            application.Database = database;
            application.Namespace = string.Empty;
            application.Channel = string.Empty;
            application.DisplayName = item.AppName;
            application.AppName = item.AppName;
            application.Description = versionInfo.FileDescription;
            application.Version = versionInfo.FileVersion;
            application.VersionDate = File.GetLastWriteTimeUtc(item.Executable);
            application.ResourceFolder = item.Resourcefolder;
            application.Company = versionInfo.CompanyName;
            application.ImageUri = string.Empty; // TODO: Use default image?
            application.Executable = item.Executable;
            application.Arguments = string.Empty;
            application.SourceID = string.Empty;
            application.SourceUrl = string.Empty;   // TODO: Maybe use file://abc/123.exe ?
            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(databaseName + Path.GetFullPath(application.Executable));
            application.CanBeUninstalled = false;
            return application;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static DatabaseApplication ToApplication(DeployedConfigFile item, string databaseName) {

            Database database = ServerManager.ServerInstance.GetDatabase(databaseName);

            Trace.Assert(database != null, "Database not found: " + databaseName);

            DatabaseApplication application = new DatabaseApplication();
            application.Database = database;
            application.Namespace = item.Namespace;
            application.Channel = item.Channel;
            application.DisplayName = item.DisplayName;
            application.AppName = item.AppName;
            application.Description = item.Description;
            application.Version = item.Version;
            application.VersionDate = item.VersionDate;
            application.ResourceFolder = item.GetResourceFullPath(DeployManager.GetDeployFolder(database.ID));
            application.Company = item.Company;
            application.ImageUri = string.IsNullOrEmpty(item.ImageUri) ? string.Empty : string.Format("{0}/{1}", DeployManager.GetAppImagesFolder(), item.ImageUri); // Use default image?
            application.Executable = item.GetExecutableFullPath(DeployManager.GetDeployFolder(database.ID));
            application.Arguments = string.Empty;
            application.SourceID = item.SourceID;
            application.SourceUrl = item.SourceUrl;

            application.CanBeUninstalled = item.CanBeUninstalled;
            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(application.DatabaseName + item.GetExecutableFullPath(DeployManager.GetRawDeployFolder(database.ID)));
            return application;
        }

        /// <summary>
        /// Generates a DatabaseApplication from an AppInfo instance
        /// </summary>
        /// <param name="item"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static DatabaseApplication ToApplication(AppInfo item, string databaseName) {

            Database database = ServerManager.ServerInstance.GetDatabase(databaseName);

            Trace.Assert(database != null, "Database not found: " + databaseName);

            DatabaseApplication application = new DatabaseApplication();
            application.Database = database;
            application.Namespace = string.Empty;
            application.Channel = string.Empty;
            application.DisplayName = item.Name;
            application.AppName = item.Name;

            try {
                var versionInfo = FileVersionInfo.GetVersionInfo(item.FilePath);
                application.Description = versionInfo.FileDescription;
                application.Version = versionInfo.FileVersion;
                application.Company = versionInfo.CompanyName;
            }
            catch (Exception) {
                application.Description = string.Empty;
                application.Version = string.Empty;
                application.Company = string.Empty;
            }

            application.VersionDate = File.GetLastWriteTimeUtc(item.FilePath);
            application.ResourceFolder = item.WorkingDirectory;
            application.ImageUri = string.Empty; // TODO: Use default image?
            application.Executable = item.FilePath;
            application.Arguments = string.Empty; // TODO:
            application.SourceID = string.Empty;
            application.SourceUrl = string.Empty; // TODO: Maybe use file://abc/123.exe ?

            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(databaseName + Path.GetFullPath(application.Executable));
            application.CanBeUninstalled = false;
            return application;
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
    public enum ApplicationStatus : long {
        None = 0,
        Starting = 1,
        Stopping = 2,
        Installing = 4,
        Uninstalling = 16,
        Downloading = 32,
        Deleting = 64,
        Upgrading = 128
    }
}
