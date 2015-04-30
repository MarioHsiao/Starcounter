using Starcounter;
using System;
using System.Threading;

namespace Administrator.Server.Model {

    partial class DatabaseJson : Page, IBound<Database> {

        /// <summary>
        /// Start invoked
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Start action) {

            this.Data.WantRunning = true;
        }

        /// <summary>
        /// Stop invoked
        /// </summary>
        /// <param name="action"></param>
        void Handle(Input.Stop action) {

            this.Data.WantRunning = false;
        }

        void Handle(Input.Delete action) {

            this.Data.WantDeleted = true;
        }
    }
}


