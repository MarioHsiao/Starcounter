using Starcounter;
using StarcounterApplicationWebSocket.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Applications.UsageTrackerApp.VersionHandler {

    /// <summary>
    /// Unpacker worker takes car of unpacking uploaded packages
    /// </summary>
    internal class UnpackerWorker {

        #region Properties
        private bool _IsRunning;
        public bool IsRunning {
            get {
                return this._IsRunning;
            }
            private set {
                this._IsRunning = value;
            }
        }
        private bool bAbort = false;
        private AutoResetEvent waitEvent = new AutoResetEvent(false);
        #endregion

        /// <summary>
        /// Start unpacker worker
        /// </summary>
        public void Start() {

            if (this.IsRunning == true) throw new InvalidOperationException("Worker is already running");
            this.bAbort = false;
            ThreadPool.QueueUserWorkItem(WorkerThread);

        }

        /// <summary>
        /// Stop unpacker worker
        /// </summary>
        public void Stop() {
            this.bAbort = true;
        }

        /// <summary>
        /// Trigger unpacker worker to check for tasks 
        /// </summary>
        public void Trigger() {
            this.waitEvent.Set();
        }

        /// <summary>
        /// Unpacker thread
        /// </summary>
        /// <param name="state"></param>
        private void WorkerThread(object state) {

            LogWriter.WriteLine("NOTICE: Unpacker worker started.");

            this.IsRunning = true;

            DbSession dbSession = new DbSession();

            while (true) {

                waitEvent.WaitOne();

                if (this.bAbort == true) {
                    break;
                }

                try {
                    dbSession.RunSync(() => {
                        this.UnpackUnpackedPackages();
                    });
                }
                catch (Exception e) {
                    LogWriter.WriteLine(string.Format("ERROR: {0}", e.ToString()));
                }

            }

            this.IsRunning = false;
            LogWriter.WriteLine("NOTICE: Unpacker worker ended.");

        }

        /// <summary>
        /// Unpack unpacked uploaded packages
        /// </summary>
        private void UnpackUnpackedPackages() {

            bool bUnpacked = false;
            LogWriter.WriteLine("NOTICE: Checking for new packages to unpack.");

            var verions = Db.SlowSQL("SELECT o FROM VersionSource o"); // TODO: Only select items where 'PackageFile' is not empty

            foreach (VersionSource item in verions) {

                if (string.IsNullOrEmpty(item.PackageFile)) {
                    continue;
                }

                // Validate the existans of the package file
                if (!File.Exists(item.PackageFile)) {
                    LogWriter.WriteLine(string.Format("ERROR: Package file {0} is missing. Please restart to trigger the cleanup process", item.PackageFile));
                    continue;
                }

                // Add channel to destination path
                string destination = Path.Combine(VersionHandlerApp.Settings.SourceFolder, item.Channel);

                // Add version to destination path
                destination = Path.Combine(destination, item.Version);

                // Validate destination folder (it should not exist)
                if (Directory.Exists(destination)) {
                    LogWriter.WriteLine(string.Format("ERROR: Package file {0} destination folder {1} already exists. Please restart to trigger the cleanup process", item.PackageFile, destination));
                    continue;
                }

                // Unpack
                bool result = PackageHandler.Unpack(item.PackageFile, destination);

                if (result == true) {
                    // Success
                    try {
                        // Delete package
                        File.Delete(item.PackageFile);
                    }
                    catch (Exception e) {
                        LogWriter.WriteLine(string.Format("ERROR: Failed to delete the uploaded package file {0}, {1}", item.PackageFile, e.Message));
                    }

                    Db.Transaction(() => {
                        // Clear packagefile
                        item.PackageFile = string.Empty;
                        item.SourceFolder = destination;
                    });

                    bUnpacked = true;

                }
                else {
                    // Error
                    // The package file can be invalid or the sourcefolder can be blocked or similar
                }
            }

            if (bUnpacked) {
                // Trigger the buildworker
                VersionHandlerApp.BuildkWorker.Trigger();
            }
        }
    }
}
