using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sc.Tools.Logging;
using Starcounter;

namespace StarcounterApps3 {
//    [Json.LogEntries]
    partial class LogApp : App {


        //private bool Show_Debug;
        //private bool Show_SuccessAudit;
        //private bool Show_FailureAudit;
        //private bool Show_Notice;
        //private bool Show_Warning;
        //private bool Show_Error;
        //private bool Show_Critical;

        void Handle(Input.RefreshList action) {
            // TODO: Select from database

            this.UpdateResult();
        }

        void Handle(Input.ClearList action) {

            Db.Transaction(() => {
                Db.SlowSQL("DELETE from LogItem");
            });

            this.UpdateResult();
        }

        private string GetFilter() {
            return string.Format("WHERE Type is {0}");
        }

        public void UpdateResult() {
            int count = 4;
            // TODO: ORDER BY dosent work
            //logApp.LogEntries = Db.SQL("SELECT o FROM LogItem o ORDER BY o.SeqNumber FETCH ?", count);

            try {
                this.LogEntries = Db.SQL("SELECT o FROM LogItem o ORDER BY o.SeqNumber ASC FETCH ?", count);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }


            // Trying to use indexes
            // Db.SQL("select e from employee e OPTION INDEX (e companyIndx)"))

        }


        [Json.LogEntries]
        partial class LogEntryApp : App<LogItem> { }

    }




}