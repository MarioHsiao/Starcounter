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
        public long Rating;

        //private bool _IsDeployed;
        public bool IsDeployed {
            get {
                return this.HasDatabaseAppliction;

                //return this._IsDeployed;
            }
            //set {
            //    if (this._IsDeployed == value) return;
            //    this._IsDeployed = value;
            //    this.OnPropertyChanged("IsDeployed");
            //}
        }

        public bool IsRunning {
            get {
                return this.HasDatabaseAppliction && this.DatabaseApplication.IsRunning;
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

        private bool _WantDeployed;
        public bool WantDeployed {
            get {
                return this._WantDeployed;
            }
            set {
                this._WantDeployed = value;

                // Reset error state
                this.CouldnotDeploy = false;
                //this.CouldnotDelete = false;

                this.OnPropertyChanged("WantDeployed");
                this.Evaluate();
            }
        }

        private bool _CouldnotDeploy;
        public bool CouldnotDeploy {
            get {
                return this._CouldnotDeploy;
            }
            set {
                if (this._CouldnotDeploy == value) return;
                this._CouldnotDeploy = value;
                this.OnPropertyChanged("CouldnotDeploy");
            }
        }

        public Database Database { get; set; }

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
                this.OnPropertyChanged("DatabaseApplication");
                this.OnPropertyChanged("IsRunning");
                this.OnPropertyChanged("IsDeployed");
            }
        }

        void _DatabaseApplication_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        public bool HasDatabaseAppliction {
            get {
                return this.DatabaseApplication != null;
            }
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

            if (this.WantDeployed) {

                if (!this.CouldnotDeploy && this.IsDeployed == false && !this.Status.HasFlag(ApplicationStatus.Downloading)) {
                    this.DeployApplication();
                }
            }
        }

        #region Actions

        /// <summary>
        /// Deploy appkication on server
        /// Download application from appstore.
        /// </summary>
        private void DeployApplication() {

            this.Status |= ApplicationStatus.Downloading;
            this.StatusText = "Downloading";

            DeployManager.Download(this, (application) => {

                this.Status &= ~ApplicationStatus.Downloading; // Remove status
                this.StatusText = string.Empty;
                this.Evaluate();
            }, (message) => {

                this.CouldnotDeploy = true;
                this.Status &= ~ApplicationStatus.Downloading; // Remove status
                this.StatusText = message;
                this.Evaluate();
            });
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
