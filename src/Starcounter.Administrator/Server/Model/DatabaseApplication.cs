using Administrator.Server.Managers;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
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
        public string DatabaseName {
            get {
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
        public string Heading;

        private bool _IsDeployed;
        public bool IsDeployed {
            get {
                return this._IsDeployed;
            }
            set {
                if (this._IsDeployed == value) return;
                this._IsDeployed = value;
                this.OnPropertyChanged("IsDeployed");
            }
        }
        private bool _IsInstalled;
        public bool IsInstalled {
            get {
                return this._IsInstalled;
            }
            set {
                if (this._IsInstalled == value) return;
                this._IsInstalled = value;
                this.OnPropertyChanged("IsInstalled");
            }
        }

        private bool _CanBeUninstalled;
        public bool CanBeUninstalled {
            get {
                return this._CanBeUninstalled;
            }
            set {
                if (this._CanBeUninstalled == value) return;
                this._CanBeUninstalled = value;
                this.OnPropertyChanged("CanBeUninstalled");
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

        private ApplicationStatus _Status;
        public ApplicationStatus Status {
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

        private bool _WantRunning;
        public bool WantRunning {
            get {
                return this._WantRunning;
            }
            set {
                //if (this._WantRunning == value) return;
                this._WantRunning = value;

                // Reset error state
                this.CouldNotStart = false;
                this.CouldNotStop = false;

                this.ResetErrorMessage();

                this.OnPropertyChanged("WantRunning");
                this.Evaluate();
            }
        }

        private bool _WantInstalled;
        public bool WantInstalled {
            get {
                return this._WantInstalled;
            }
            set {
                //if (this._WantInstalled == value) return;
                this._WantInstalled = value;

                // Reset error state
                this.CouldNotInstall = false;
                this.CouldNotUninstall = false;
                this.ResetErrorMessage();

                this.OnPropertyChanged("WantInstalled");
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
                this._CouldNotDelete = false;
                this._CouldNotStop = false;
                this.ResetErrorMessage();

                this.OnPropertyChanged("WantDeleted");
                this.Evaluate();
            }
        }

        private bool _CouldNotStart;
        public bool CouldNotStart {
            get {
                return this._CouldNotStart;
            }
            set {
                if (this._CouldNotStart == value) return;
                this._CouldNotStart = value;
                this.OnPropertyChanged("CouldNotStart");
            }
        }

        private bool _CouldNotStop;
        public bool CouldNotStop {
            get {
                return this._CouldNotStop;
            }
            set {
                if (this._CouldNotStop == value) return;
                this._CouldNotStop = value;
                this.OnPropertyChanged("CouldNotStop");
            }
        }

        private bool _CouldNotInstall;
        public bool CouldNotInstall {
            get {
                return this._CouldNotInstall;
            }
            set {
                if (this._CouldNotInstall == value) return;
                this._CouldNotInstall = value;
                this.OnPropertyChanged("CouldNotInstall");
            }
        }

        private bool _CouldNotUninstall;
        public bool CouldNotUninstall {
            get {
                return this._CouldNotUninstall;
            }
            set {
                if (this._CouldNotUninstall == value) return;
                this._CouldNotUninstall = value;
                this.OnPropertyChanged("CouldNotUninstall");
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

        /// <summary>
        /// Evaluate application wanting states
        /// </summary>
        public void Evaluate() {

            if (this.Status != ApplicationStatus.None) {
                // Work already in progres, when work is compleated it will call Evaluate()
                return;
            }

            if (this.WantRunning) {

                if (!this.Database.CouldnotStart && this.Database.IsRunning == false && !this.Database.Status.HasFlag(DatabaseStatus.Starting)) {
                    this.Database.WantRunning = true;
                }
                else {
                    if (!this.CouldNotStart && this.IsRunning == false && !this.Status.HasFlag(ApplicationStatus.Starting)) {
                        this.StartApplication(); // Async
                    }
                }
            }
            else {

                if (!this.CouldNotStop && this.IsRunning == true && !this.Status.HasFlag(ApplicationStatus.Stopping)) {
                    this.StopApplication();  // Async
                }
            }

            if (this.Status != ApplicationStatus.None) {
                // Work already in progres, when work is compleated it will call Evaluate()
                return;
            }

            if (this.WantDeleted && !this.CouldNotStop) {

                // If application is running or starting then try to stop it.
                if (this.IsRunning || this.Status.HasFlag(ApplicationStatus.Starting)) {
                    this.WantRunning = false; // Will trigger Evaluate()
                }
                else {

                    if (this.CouldNotDelete == false && this.IsDeployed == true && !this.Status.HasFlag(ApplicationStatus.Deleting)) {
                        this.DeleteApplication(); // Async
                    }
                }
            }


            if (this.Status != ApplicationStatus.None) {
                // Work already in progres, when work is compleated it will call Evaluate()
                return;
            }

            if (this.WantInstalled) {

                if (!this.CouldNotInstall && this.IsInstalled == false && !this.Status.HasFlag(ApplicationStatus.Installing)) {
                    this.InstallApplication();   // Sync
                }
            }
            else {

                if (!this._CouldNotUninstall && this.IsInstalled == true && !this.Status.HasFlag(ApplicationStatus.Uninstalling)) {
                    this.UninstallApplication(); // Sync
                }
            }
        }

        #region Actions

        /// <summary>
        /// Install application
        /// Will make the application start along with the database startup
        /// </summary>
        private void InstallApplication() {

            //this.ResetErrorMessage();

            try {
                this.Status |= ApplicationStatus.Installing;
                ApplicationManager.InstallApplication(this);
                this.Status &= ~ApplicationStatus.Installing;

                this.Evaluate();
            }
            catch (Exception e) {

                this.Status &= ~ApplicationStatus.Installing;
                this.CouldNotInstall = true;
                this.OnCommandError("Install Application", e.Message, null);
            }
        }

        /// <summary>
        /// Uninstall application
        /// Application will not be started along with the database startup
        /// </summary>
        private void UninstallApplication() {

            //this.ResetErrorMessage();

            try {
                this.Status |= ApplicationStatus.Uninstalling;
                ApplicationManager.UninstallApplication(this);
                this.Status &= ~ApplicationStatus.Uninstalling;

                this.Evaluate();
            }
            catch (Exception e) {
                this.Status &= ~ApplicationStatus.Uninstalling;
                this.CouldNotUninstall = true;
                this.OnCommandError("Uninstall Application", e.Message, null);
            }
        }

        /// <summary>
        /// Start application
        /// </summary>
        /// <param name="application"></param>
        private void StartApplication() {

            //this.ResetErrorMessage();

            DatabaseApplication app = ApplicationManager.GetApplication(this.DatabaseName, this.ID);
            if (app == null) {
                // TODO: 500: Internal Server Error
                this.OnCommandError("Start Application", "Could not find application", null);
                this.CouldNotStart = true;
                this.Evaluate();
                return;
            }

            string[] arguments = new string[0];
            // TODO: Use arguments from application (String need to be split to array

            AppInfo appinfo = new AppInfo(app.AppName, app.Executable, app.Executable, app.ResourceFolder, arguments, "");

            // Create Command
            StartExecutableCommand command;
            command = new StartExecutableCommand(RootHandler.Host.Engine, this.DatabaseName, appinfo);
            command.EnableWaiting = false;
            command.RunEntrypointAsynchronous = false;  // ?

            RunApplicationCommand(command);
        }

        /// <summary>
        /// Stop application
        /// </summary>
        /// <param name="application"></param>
        private void StopApplication() {

            //this.ResetErrorMessage();

            DatabaseApplication app = ApplicationManager.GetApplication(this.DatabaseName, this.ID);
            if (app == null) {
                // TODO: 500: Internal Server Error
                this.OnCommandError("Stop Application", "Could not find application", null);
                this.CouldNotStart = true;
                this.Evaluate();
                return;
            }

            string id;

            // We can not stop the application by using the appName, we need to use the App Key or Executable full path.
            // There is an flaw when using the Executable full path for stopping apps. (multiple apps can have the same filename but not the same appname)
            AppInfo appInfo = this.GetApplicationAppInfo();
            if (appInfo != null) {
                id = appInfo.Key;
            }
            else {
                id = app.Executable;
            }

            var command = new StopExecutableCommand(RootHandler.Host.Engine, app.DatabaseName, id);
            command.EnableWaiting = false;

            RunApplicationCommand(command);
        }

        /// <summary>
        /// Delete deployed application from server
        /// </summary>
        public void DeleteApplication() {

            //this.ResetErrorMessage();

            this.Status |= ApplicationStatus.Deleting;
            this.StatusText = "Deleting";

            DeployManager.Delete(this, (application) => {

                this.Status &= ~ApplicationStatus.Deleting; // Remove status
                this.StatusText = string.Empty;
                this.Evaluate();
            }, (message) => {

                this.CouldNotDelete = true;
                this.Status &= ~ApplicationStatus.Deleting; // Remove status
                this.StatusText = message;
                this.Evaluate();
            });
        }

        /// <summary>
        /// Run Application Command (Start/Stop)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="database"></param>
        private void RunApplicationCommand(ServerCommand command) {

            var runtime = RootHandler.Host.Runtime;

            if (command is StartExecutableCommand) {
                this.Status |= ApplicationStatus.Starting;  // Add Status
            }
            else if (command is StopExecutableCommand) {
                this.Status |= ApplicationStatus.Stopping;  // Add Status
            }

            // Execute Command
            var c = runtime.Execute(command, (commandId) => {

                return this.WantRunning == this.IsRunning;  // return true to cancel
            }, (commandId) => {

                CommandInfo commandInfo = runtime.GetCommand(commandId);

                if (command is StartExecutableCommand) {
                    this.CouldNotStart = commandInfo.HasError;
                    this.Status &= ~ApplicationStatus.Starting; // Remove status
                }
                else if (command is StopExecutableCommand) {
                    this.CouldNotStop = commandInfo.HasError;
                    this.Status &= ~ApplicationStatus.Stopping; // Remove status
                }

                this.IsRunning = this.ApplicationRunningState();
                this.StatusText = string.Empty;

                if (commandInfo.HasError) {
                    ErrorInfo single = commandInfo.Errors.PickSingleServerError();
                    var msg = single.ToErrorMessage();
                    this.OnCommandError(command.Description, msg.Brief, msg.Helplink);
                }
                else {
                    //this.Evaluate();
                }
                this.Evaluate();

            });

            this.StatusText = c.Description;

            if (c.IsCompleted) {

                if (command is StartExecutableCommand) {
                    this.CouldNotStart = c.HasError;
                    this.Status &= ~ApplicationStatus.Starting; // Remove status
                }
                else if (command is StopExecutableCommand) {
                    this.CouldNotStop = c.HasError;
                    this.Status &= ~ApplicationStatus.Stopping; // Remove status
                }

                if (c.HasError) {
                    ErrorInfo single = c.Errors.PickSingleServerError();
                    var msg = single.ToErrorMessage();
                    this.OnCommandError(command.Description, msg.Brief, msg.Helplink);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnCommandError(string title, string message, string helpLink) {
            // TODO: Append errors to notification list
            //    this.StatusText = message;

            this.ErrorMessage.Title = title;
            this.ErrorMessage.Message = message;
            this.ErrorMessage.HelpLink = helpLink;

            this.OnPropertyChanged("ErrorMessage");
            this.OnPropertyChanged("HasErrorMessage");

        }

        private void ResetErrorMessage() {
            //            this.ErrorMessage = null;
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
            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(application.DatabaseName + item.GetExecutableFullPath(DeployManager.GetDeployFolder(database.ID)));
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
        Deleting = 64
    }
}
