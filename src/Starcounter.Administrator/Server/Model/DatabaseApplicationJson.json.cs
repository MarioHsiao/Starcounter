using Starcounter;
using System;
using System.Threading;

namespace Administrator.Server.Model {

    partial class DatabaseApplicationJson : Page, IBound<DatabaseApplication> {

        void Handle(Input.Start action) {
            this.Data.WantRunning = true;
        }

        void Handle(Input.Stop action) {
            this.Data.WantRunning = false;
        }

        void Handle(Input.Install action) {
            this.Data.WantInstalled = true;
        }

        void Handle(Input.Uninstall action) {
            this.Data.WantInstalled = false;
        }

        void Handle(Input.Delete action) {
            this.Data.WantDeleted = true;
        }
    }
}


