using Starcounter;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Starcounter {
    internal static class StarcounterCollectionHandler {

        public static void Bootstrap(ushort port) {
            UsageCollectionHandler.Setup_POST(port);
        }

        public static Installation AssureInstallation(int installationNo, string downloadId) {


            //Installation installation = Db.SlowSQL("SELECT o FROM Installation o WHERE o.IP=? AND o.Mac = ? AND o.DownloadID = ?", ip, mac, downloadId).First;
            Installation installation = Db.SlowSQL("SELECT o FROM Installation o WHERE o.InstallationNo=? AND o.DownloadID=?", installationNo, downloadId).First;
            if (installation == null) {
                // Create installation
                Db.Transaction(() => {
                    installation = new Installation( downloadId, installationNo);
                });

            }

            return installation;
        }

        /// <summary>
        /// Get next Sequence number for a tableid
        /// </summary>
        /// <param name="tableId"></param>
        /// <returns></returns>
        public static int GetNextSequenceNo(string tableId) {

            int no = 0;
            Db.Transaction(() => {
                // Generate new sequence number for the new installation
                Sequence sequence = Db.SlowSQL("SELECT o FROM Sequence o WHERE o.TableName=?", tableId).First;
                if (sequence == null) {
                    sequence = new Sequence(tableId);
                }
                else {
                    sequence.No++;
                }
                no = sequence.No;
            });

            return no;
        }


    }
}
