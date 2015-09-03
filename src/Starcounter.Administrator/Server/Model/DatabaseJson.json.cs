using Starcounter;
using System;
using System.Threading;

namespace Administrator.Server.Model {

    partial class DatabaseJson : Page, IBound<Database> {

        /// <summary>
        /// Start 
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Start action) {

            this.Data.StartDatabase((database) => {

            }, (database, wasCancelled, title, message, helpLink) => {

            });

            //this.Data.StartDatabase((database) => {

            //}, (database, wasCancelled, title, message, helpLink) => {

            //});
            //this.Data.WantRunning = true;
        }

        /// <summary>
        /// Stop 
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Stop action) {

            this.Data.StopDatabase((database) => {

            }, (database, wasCancelled, title, message, helpLink) => {

            });

            //this.Data.StartDatabase((database) => {

            //}, (database, wasCancelled, title, message, helpLink) => {

            //});

            //this.Data.StopDatabase((database) => {

            //}, (database, wasCancelled, title, message, helpLink) => {

            //});
            //this.Data.WantRunning = false;
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Delete action) {

            //this.Data.WantDeleted = true;

            this.Data.DeleteDatabase((database) => {

            }, (database, wasCancelled, title, message, helpLink) => {

            });

        }

        /// <summary>
        /// Refresh appstore stores
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.RefreshAppStoreStores action) {

            this.Data.InvalidateAppStoreStores();
        }
    }
}
