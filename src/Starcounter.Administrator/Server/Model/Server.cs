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
using System;
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

        #region Properties

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
        private  void Database_Changed(object sender, EventArgs e) {

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

        /// <summary>
        /// Invalidate list of databases
        /// Assure that the list of databases is up-to-date
        /// </summary>
        public void InvalidateDatabases() {

            var server = RootHandler.Host.Runtime;
            var databaseInfos = server.GetDatabases();

            foreach (DatabaseInfo databaseInfo in databaseInfos) {

                Database database = GetDatabaseByUrl(databaseInfo.Uri);
                if (database == null) {
                    // Create
                    database = new Database();
                    database.ID = databaseInfo.Name;
                    database.DisplayName = databaseInfo.Name;
                    database.Url = databaseInfo.Uri;
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
                if (database.ID == id) {
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
        private Database GetDatabaseByUrl(string url) {

            foreach (var database in this.Databases) {
                if (database.Url == url) {
                    return database;
                }
            }
            return null;
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
