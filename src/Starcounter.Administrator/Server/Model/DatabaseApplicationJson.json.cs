using Administrator.Server.Managers;
using Starcounter;
using System;
using System.Threading;

namespace Administrator.Server.Model {

    partial class DatabaseApplicationJson : Page, IBound<DatabaseApplication> {

        void Handle(Input.Start action) {
            //this.Data.WantRunning = true;

            this.Data.StartApplication((database) => {

            }, (application, wasCancelled, title, message, helpLink) => {

            });
        }

        void Handle(Input.Stop action) {
            //this.Data.WantRunning = false;
            this.Data.StopApplication((database) => {

            }, (application, wasCancelled, title, message, helpLink) => {

            });
        }

        void Handle(Input.Install action) {

            this.Data.InstallApplication((application) => {

            }, (application, wasCancelled, title, message, helpLink) => {

            });

            //this.Data.WantInstalled = true;
        }

        void Handle(Input.Uninstall action) {
            //this.Data.WantInstalled = false;

            this.Data.UninstallApplication((application) => {

            }, (application, wasCancelled, title, message, helpLink) => {

            });

        }

        void Handle(Input.Delete action) {

            this.Data.DeleteApplication(false, (application) => {

            }, (application, wasCancelled, title, message, helpLink) => {

            });
            //this.Data.WantDeleted = true;
        }

        void Handle(Input.CanBeUninstalled action) {


            this.Data.SetCanBeUninstalledFlag(action.Value, (application) => {

            }, (application, wasCancelled, title, message, helpLink) => {

            });

            //DeployedConfigFile config = DeployManager.GetItemFromApplication(this.Data);
            //if (config != null) {
            //    config.CanBeUninstalled = action.Value;
            //    config.Save();
            //}
        }
    }
}


