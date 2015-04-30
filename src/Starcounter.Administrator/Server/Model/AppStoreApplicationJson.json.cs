using Starcounter;
using System;
using System.Threading;

namespace Administrator.Server.Model {

    partial class AppStoreApplicationJson : Page, IBound<AppStoreApplication> {

        void Handle(Input.Download action) {
            this.Data.WantDeployed = true;
        }

        void Handle(Input.Open action) {

            // TODO: Go to app page
            this.StatusText = "Not implemented";
        }
    }
}



