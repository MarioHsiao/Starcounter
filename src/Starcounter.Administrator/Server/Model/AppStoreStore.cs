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
        public string DisplayName;
        public string SourceID;         // App Store store id
        public string SourceUrl;        // App Store store source

        public Administrator.Server.Model.Database Database;

        public ObservableCollection<AppStoreApplication> Applications = new ObservableCollection<AppStoreApplication>();

        #endregion

        #region Model Changed Event Events

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        #endregion

        public AppStoreStore() {

            this.Applications.CollectionChanged += Applications_CollectionChanged;
        }

        void Applications_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    // Add listeners on the database instance
                    foreach (AppStoreApplication item in e.NewItems) {

                        item.Changed += AppStoreApplication_Changed;
                        // Connect AppStoreAppliction with DatabaseApplication (if available)
                        item.DatabaseApplication = this.Database.GetApplicationBySourceUrl(item.SourceUrl);
                        item.UpdateModel();
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:

                    // Remove listeners on the database instance
                    foreach (AppStoreApplication item in e.OldItems) {
                        item.Changed -= AppStoreApplication_Changed;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:

                    // Remove listeners on the database instance
                    foreach (AppStoreApplication item in this.Applications) {
                        item.Changed -= AppStoreApplication_Changed;
                        item.Changed += AppStoreApplication_Changed;
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
        void AppStoreApplication_Changed(object sender, EventArgs e) {

            this.OnChanged(sender, e);
        }

        private void OnChanged(object sender, EventArgs e) {

            if (Changed != null) {
                Changed(sender, e);
            }
        }
     

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="applicationJson"></param>
        //public AppStoreStoreJson ToAppStoreStore() {

        //    AppStoreStoreJson item = new AppStoreStoreJson();
        //    item.ID = this.ID;
        //    item.DisplayName = this.DisplayName;

        //    return item;
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
