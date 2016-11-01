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

    public class AppStoreStore : INotifyPropertyChanged {

        #region Properties
        public string ID;               // SourceUrl + SourceID
        public string DepotKey;         // Store Key from warehouse
        private string _DisplayName;
        public string DisplayName {
            get {
                return this._DisplayName;
            }
            internal set {
                if (this._DisplayName == value) return;
                this._DisplayName = value;
                this.OnPropertyChanged("DisplayName");
            }
        }

        private string _Description;
        public string Description {
            get {
                return this._Description;
            }
            internal set {
                if (this._Description == value) return;
                this._Description = value;
                this.OnPropertyChanged("Description");
            }
        }
        public string SourceID;         // App Store store id
        public string SourceUrl;        // App Store store source
        public bool ShowCompatibleVersions; 
        public Administrator.Server.Model.Database Database;

        public RangeEnabledObservableCollection<AppStoreApplication> Applications = new RangeEnabledObservableCollection<AppStoreApplication>();

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public AppStoreStore() {

            this.Applications.CollectionChanged += Applications_CollectionChanged;
            this.PropertyChanged += AppStoreStore_PropertyChanged;
        }

        private void AppStoreStore_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            this.OnChanged(sender, e);
        }

        void Applications_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    // Add listeners on the database instance
                    foreach (AppStoreApplication item in e.NewItems) {

                        //item.Database = this.Database;
                        item.Changed += AppStoreApplication_Changed;

                        // Connect AppStoreAppliction with DatabaseApplication (if available)
                        item.DatabaseApplication = this.Database.GetApplicationBySourceUrl(item.SourceUrl);
                        item.UpdateModel();
                        item.UpdateUpgradeFlag();
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                    // Remove listeners on the database instance
                    foreach (AppStoreApplication item in e.OldItems) {
                        item.UpdateUpgradeFlag();
                        item.Changed -= AppStoreApplication_Changed;
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    IList<AppStoreApplication> list = (IList<AppStoreApplication>)sender;

                    // Remove listeners on the database instance
                    foreach (AppStoreApplication item in list) {
                        item.Changed -= AppStoreApplication_Changed;
                        item.Changed += AppStoreApplication_Changed;
                        item.DatabaseApplication = this.Database.GetApplicationBySourceUrl(item.SourceUrl);
                        item.UpdateUpgradeFlag();
                    }

                    break;
            }

            this.OnChanged(sender, e);
        }

        public AppStoreApplication GetLatestVersion(string nameSpace, string channel) {

            AppStoreApplication latestVersion = null;
            DateTime aDate = DateTime.MinValue;

            IList<AppStoreApplication> apps = this.GetAppStoreApplications(nameSpace, channel);
            foreach (AppStoreApplication app in apps) {

                if (app.VersionDate >= aDate) {
                    aDate = app.VersionDate;
                    latestVersion = app;
                }
            }

            return latestVersion;
        }


        public IList<AppStoreApplication> GetAppStoreApplications(string nameSpace, string channel) {

            List<AppStoreApplication> result = new List<AppStoreApplication>();

            foreach (AppStoreApplication app in this.Applications) {

                if (app.Namespace == nameSpace && app.Channel == channel) {
                    result.Add(app);
                }
            }

            return result;
        }

        /// <summary>
        /// Called when an application property has been changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AppStoreApplication_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        private void OnChanged(object sender, EventArgs e) {

            if (Changed != null) {
                Changed(sender, e);
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

}
