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


            this.Data.DeployApplication((deployedApplication) => {

                // Success
                deployedApplication.InstallApplication((installedApplication) => {

                    installedApplication.StartApplication((databaseapplication) => {
                        // Success
                    }, (databaseapplication, wasCanceled, title, message, helpLink) => {
                        // Error
                    });

                    // Success
                }, (installedApplication, wasCanceled, title, message, helpLink) => {

                    // Error
                });

            }, (deployedApplication, wasCanceled, title, message, helpLink) => {

                // Error
            });

            //this.Data.WantDeployed = true;
            //this.Data.WantInstalled = true;
        }

        /// <summary>
        /// Delete downloaded application
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Delete action) {

            //this.Data.WantDeployed = false;

            this.Data.DeleteApplication((deployedApplication) => {

                // Success

            }, (deployedApplication, wasCanceled, title, message, helpLink) => {

                // Error
            });

        }

        /// <summary>
        /// Download application
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Download action) {

            //this.Data.WantInstalled = false;
            //this.Data.WantDeployed = true;

            this.Data.DeployApplication((deployedApplication) => {

                // Success
            }, (deployedApplication, wasCanceled, title, message, helpLink) => {

                // Error
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Open action) {

            if (this.Data.HasDatabaseAppliction) {

                this.Data.DatabaseApplication.StartApplication((application) => {

                }, (application, wasCancelled, title, message, helpLink) => {
                    // TODO: Handle error
                });
            }


            // TODO: Go to app page
        }

        /// <summary>
        /// Upgrade application
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Upgrade action) {

            this.Data.UpgradeApplication((application) => {

                // Success
            }, (application, wasCanceled, title, message, helpLink) => {

                // Error
            });
            //            this.Data.WantDeployed = true;
        }
    }
}



