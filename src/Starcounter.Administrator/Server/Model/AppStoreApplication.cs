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

        private ApplicationStatus _Status;
        public ApplicationStatus Status {
            get {

                if (this.HasDatabaseAppliction) {
                    return this.DatabaseApplication.Status;
                }

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

                if (this.HasDatabaseAppliction) {
                    return this.DatabaseApplication.StatusText;
                }

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


        private bool _WantDeployed;
        public bool WantDeployed {
            get {
                return this._WantDeployed;
            }
            set {
                this._WantDeployed = value;

                // Reset error state
                this.DeployError = false;
                //this.CouldnotDelete = false;

                this.OnPropertyChanged("WantDeployed");
                this.Evaluate();
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

        private bool _WantInstalled;
        public bool WantInstalled {
            get {
                return this._WantInstalled;
            }
            set {
                this._WantInstalled = value;

                // Reset error state
                this.CouldNotInstall = false;

                this.OnPropertyChanged("WantInstalled");
                this.Evaluate();
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
                this._DatabaseApplication = value;

                if (this._DatabaseApplication != null) {
                    this._DatabaseApplication.Changed += _DatabaseApplication_Changed;
                }
                this.OnPropertyChanged("HasDatabaseAppliction");
                this.OnPropertyChanged("DatabaseApplication");
                this.OnPropertyChanged("IsRunning");
                this.OnPropertyChanged("IsInstalled");
                this.OnPropertyChanged("IsDeployed");
                this.OnPropertyChanged("ErrorMessage");
                this.OnPropertyChanged("HasErrorMessage");
                this.OnPropertyChanged("Status");
                this.OnPropertyChanged("StatusText");
            }
        }

        void _DatabaseApplication_Changed(object sender, EventArgs e) {

            if (sender is DatabaseApplication && e is PropertyChangedEventArgs) {

                //if (((PropertyChangedEventArgs)e).PropertyName == "HasErrorMessage") {
                //    this.OnPropertyChanged("HasErrorMessage");
                //}

                if (((PropertyChangedEventArgs)e).PropertyName == "ErrorMessage") {

                    // Copy app error message to the AppStore app
                    ErrorMessage err = ((DatabaseApplication)sender).ErrorMessage;

                    this.ErrorMessage.Message = err.Message;
                    this.ErrorMessage.Title = err.Title;
                    this.ErrorMessage.HelpLink = err.HelpLink;

                    this.OnPropertyChanged("ErrorMessage");
                    this.OnPropertyChanged("HasErrorMessage");
                }

                if (((PropertyChangedEventArgs)e).PropertyName == "IsRunning") {
                    this.OnPropertyChanged("IsRunning");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "IsInstalled") {
                    this.OnPropertyChanged("IsInstalled");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "IsDeployed") {
                    this.OnPropertyChanged("IsDeployed");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "Status") {
                    this.OnPropertyChanged("Status");
                }
                if (((PropertyChangedEventArgs)e).PropertyName == "StatusText") {
                    this.OnPropertyChanged("StatusText");
                }

            }

            this.OnChanged(sender, e);
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

            this.UpdateProperties();
        }

        private void UpdateProperties() {

            // TODO:
            // this.IsRunning = this.ApplicationRunningState();
        }
        #endregion

        /// <summary>
        /// Evaluate application wanting states
        /// </summary>
        private void Evaluate() {

            if (this.IsDeployed != this.WantDeployed) {

                if (this.WantDeployed) {

                    // Download
                    if (!this.DeployError && this.IsDeployed == false && !this.Status.HasFlag(ApplicationStatus.Downloading)) {
                        this.DeployApplication();
                    }
                }
                else {


                    if (this.HasDatabaseAppliction) {
                        DatabaseApplication databaseApplication = this.DatabaseApplication;
                        databaseApplication.WantDeleted = true;
                    }


                    //// Delete
                    //if (!this.DeployError && this.IsDeployed == true && !this.Status.HasFlag(ApplicationStatus.Deleting)) {
                    //    this.DeleteApplication();
                    //}
                }
            }

            //if (this.WantDeployed) {

            //    if (!this.CouldnotDeploy && this.IsDeployed == false && !this.Status.HasFlag(ApplicationStatus.Downloading)) {
            //        this.DeployApplication();
            //    }
            //}

            if (this.Status != ApplicationStatus.None) {
                // Work already in progres, when work is compleated it will call Evaluate()
                return;
            }

            if (this.IsDeployed) {
                if (this.DatabaseApplication.IsInstalled != this.WantInstalled) {
                    this.DatabaseApplication.WantRunning = this.WantInstalled;
                    this.DatabaseApplication.WantInstalled = this.WantInstalled;
                }
            }
        }

        #region Actions

        /// <summary>
        /// Deploy appkication on server
        /// Download application from appstore.
        /// </summary>
        private void DeployApplication() {

            this.ResetErrorMessage();

            this.Status |= ApplicationStatus.Downloading;
            this.StatusText = "Downloading";

            DeployManager.Download(this, (application) => {

                this.Status &= ~ApplicationStatus.Downloading; // Remove status
                this.StatusText = string.Empty;
                this.Evaluate();
            }, (message) => {

                this.DeployError = true;
                this.Status &= ~ApplicationStatus.Downloading; // Remove status
                this.StatusText = string.Empty;

                this.OnCommandError("Downloading Application", message, null);

                this.Evaluate();
            });
        }

        /// <summary>
        /// Delete deployed application from server
        /// </summary>
        //private void DeleteApplication() {

        //    this.ResetErrorMessage();

        //    this.Status |= ApplicationStatus.Deleting;
        //    this.StatusText = "Deleting";

        //    DeployManager.Delete(this.DatabaseApplication, (application) => {

        //        this.Status &= ~ApplicationStatus.Deleting; // Remove status
        //        this.StatusText = string.Empty;
        //        this.Evaluate();
        //    }, (message) => {

        //        this.DeployError = true;
        //        this.Status &= ~ApplicationStatus.Deleting; // Remove status
        //        this.StatusText = string.Empty;

        //        this.OnCommandError("Deleting Application", message, null);

        //        this.Evaluate();
        //    });
        //}

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
        public DatabaseApplicationJson ToDatabaseApplication() {

            DatabaseApplicationJson applicationJson = new DatabaseApplicationJson();
            applicationJson.ID = this.ID;
            applicationJson.IsDeployed = this.IsDeployed;
            applicationJson.DisplayName = this.DisplayName;
            applicationJson.Description = this.Description;
            applicationJson.Company = this.Company;
            applicationJson.Version = this.Version;
            applicationJson.VersionDate = this.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            applicationJson.DatabaseName = this.DatabaseName;
            applicationJson.ImageUri = string.IsNullOrEmpty(this.ImageUri) ? string.Empty : string.Format("{0}/{1}", DeployManager.GetAppImagesFolder(), this.ImageUri); // Use default image?

            return applicationJson;
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

}
