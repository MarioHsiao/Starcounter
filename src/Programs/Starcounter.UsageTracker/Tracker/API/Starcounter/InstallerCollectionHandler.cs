using Starcounter;
using Starcounter.Applications.UsageTrackerApp.Model;
using System;

namespace Starcounter.Applications.UsageTrackerApp.API.Starcounter {
    internal static class StarcounterCollectionHandler {

        public static void Bootstrap(ushort port) {
            UsageCollectionHandler.Setup_POST(port);
        }

        public static Installation AssureInstallation(Int64 installationNo, string serial) {


            //Installation installation = Db.SlowSQL("SELECT o FROM Installation o WHERE o.IP=? AND o.Mac = ? AND o.DownloadID = ?", ip, mac, downloadId).First;
            Installation installation = Db.SlowSQL<Installation>("SELECT o FROM Installation o WHERE o.InstallationNo=? AND o.Serial=?", installationNo, serial).First;
            if (installation == null) {
                // Create installation
                Db.Transact(() => {
//                    installation = new Installation(serial, installationNo);
                    installation = new Installation();

                    installation.Serial = serial;
                    installation.Date = DateTime.UtcNow;

                    DateTime d = new DateTime(2000, 1, 1);

                    installation.InstallationNo = DateTime.UtcNow.Ticks - d.Ticks;
                    installation.PreviousInstallationNo = installationNo;


                });

            }

            return installation;
        }

        ///// <summary>
        ///// Get next Sequence number for a tableid
        ///// </summary>
        ///// <param name="tableId"></param>
        ///// <returns></returns>
        //public static Int64 GetNextSequenceNo(string tableId) {

        //    int no = 0;
        //    Db.Transaction(() => {
        //        // Generate new sequence number for the new installation
        //        Sequence sequence = Db.SlowSQL("SELECT o FROM Sequence o WHERE o.TableName=?", tableId).First;
        //        if (sequence == null) {
        //            sequence = new Sequence(tableId);
        //        }
        //        else {
        //            sequence.No++;
        //        }
        //        no = sequence.No;
        //    });

        //    return no;
        //}


    }
}
