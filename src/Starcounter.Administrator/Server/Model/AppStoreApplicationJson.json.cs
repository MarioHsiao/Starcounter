using Starcounter;
using System;
using System.Threading;

namespace Administrator.Server.Model {

    partial class AppStoreApplicationJson : Page, IBound<AppStoreApplication> {

        /// <summary>
        /// Download, Install and Start application
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Install action) {

            this.Data.WantDeployed = true;
            this.Data.WantInstalled = true;
        }

        /// <summary>
        /// Delete downloaded application
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Delete action) {

            this.Data.WantDeployed = false;
        }

        /// <summary>
        /// Download application
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Download action) {

            this.Data.WantInstalled = false;
            this.Data.WantDeployed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Open action) {

            if (this.Data.HasDatabaseAppliction) {
                this.Data.DatabaseApplication.WantRunning = true;
            }


            // TODO: Go to app page
//            this.StatusText = "Not implemented";
        }
    }
}



