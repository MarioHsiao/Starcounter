using Starcounter;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Starcounter.Applications.UsageTrackerApp.VersionHandler {

    /// <summary>
    /// Buildworker take care of building missing builds for all versions
    /// </summary>
    internal class BuildWorker {

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
        /// Start Build worker
        /// </summary>
        public void Start() {

            if (this.IsRunning == true) throw new InvalidOperationException("Worker is already running");
            this.bAbort = false;
            ThreadPool.QueueUserWorkItem(WorkerThread);

        }

        /// <summary>
        /// Stop Build worker
        /// </summary>
        public void Stop() {
            this.bAbort = true;
        }

        /// <summary>
        /// Trigger Build worker to check for tasks 
        /// </summary>
        public void Trigger() {
            this.waitEvent.Set();
        }

        /// <summary>
        /// Build worker thread
        /// </summary>
        private void WorkerThread(object state) {

            Console.WriteLine("NOTICE: Build worker started");

            this.IsRunning = true;

            DbSession dbSession = new DbSession();

            while (true) {

                waitEvent.WaitOne();

                if (this.bAbort == true) {
                    break;
                }

                dbSession.RunSync(() => {
                    this.BuildMissingBuilds();
                });
            }

            this.IsRunning = false;
            Console.WriteLine("NOTICE: Build worker ended");

        }

        /// <summary>
        /// Build missing unique builds
        /// </summary>
        private void BuildMissingBuilds() {

            Console.WriteLine("NOTICE: Checking for versions to build");

            var sources = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.BuildError=?", false);

            VersionHandlerSettings settings = VersionHandlerSettings.GetSettings();

            // Check needed builds for each versions source
            foreach (VersionSource source in sources) {

                // Validate the source folder
                if (string.IsNullOrEmpty(source.SourceFolder)) {
                    // Not unpacked yet
                    continue;
                }

                // Validate the source folder
                if (!Directory.Exists(source.SourceFolder)) {
                    Console.WriteLine("ERROR: Source folder {0} is missing. Please restart to trigger the cleanup process", source.SourceFolder);
                    continue;
                }

                while (source.BuildError == false) {

                    // Get number of available builds
                    Int64 num = Db.SlowSQL("SELECT count(o) FROM VersionBuild o WHERE o.HasBeenDownloaded=? AND o.Version=?", false, source.Version).First;

                    Int64 neededVersions = settings.MaximumBuilds - num;
                    if (neededVersions <= 0) {
                        // No new version build needed
                        break;
                    }

                    // Build destination folder path (unique per build)
                    // ..\stable\2.0.2345.2\xdfe4lvrlkmv
                    string destinationFolder = string.Empty;
                    for (int i = 0; i < 100; i++) {
                        destinationFolder = settings.VersionFolder;
                        destinationFolder = System.IO.Path.Combine(destinationFolder, source.Channel);
                        destinationFolder = System.IO.Path.Combine(destinationFolder, source.Version);
                        destinationFolder = System.IO.Path.Combine(destinationFolder, System.IO.Path.GetRandomFileName());
                        if (!Directory.Exists(destinationFolder)) {
                            break;
                        }
                    }

                    if (Directory.Exists(destinationFolder)) {
                        Console.WriteLine("ERROR: Can not generate a unique destination folder.");
                        break;
                    }


                    string file;

                    // Get serial id
                    string serialId = GetUniqueSerialId();
                    if (serialId == null) {
                        Console.WriteLine("ERROR: Could not generate a unique serial id");
                        Db.Transaction(() => {
                            source.BuildError = true;
                        });
                        break;
                    }

                    // Build
                    bool result = this.Build(source.SourceFolder, destinationFolder, serialId, settings.CertificationFile, out file);
                    if (result == false) {

                        // Build failed, mark the source as a fail to build
                        Db.Transaction(() => {
                            source.BuildError = true;
                        });

                        try {
                            // Cleanup
                            if (Directory.Exists(destinationFolder)) {
                                Directory.Delete(destinationFolder, true);
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine("ERROR: Could not cleanup destination folder {0}. {1}", destinationFolder, e.Message);
                        }

                    }
                    else {

                        // Successfull build
                        Db.Transaction(() => {

                            // Create VersionBuild instance
                            VersionBuild build = new VersionBuild();
                            build.BuildDate = DateTime.UtcNow;
                            build.File = file;
                            build.Serial = serialId;
                            build.DownloadDate = DateTime.MinValue;
                            build.Version = source.Version;
                            build.Channel = source.Channel;
                            build.Source = source;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Generate unique serial id
        /// </summary>
        /// <returns>Serial otherwice null</returns>
        private static string GetUniqueSerialId() {
            string serial;

            for (int i = 0; i < 50; i++) {
                serial = DownloadID.GenerateNewUniqueDownloadKey();
                var result = Db.SlowSQL("SELECT o FROM VersionBuild o WHERE o.Serial=?", serial).First;
                if (result == null) {
                    return serial;
                }
            }
            return null;
        }


        /// <summary>
        /// Build version
        /// </summary>
        /// <param name="sourceFolder">Source folder</param>
        /// <param name="destinationFolder">Destination Folder</param>
        /// <param name="serialId">Unique serialid</param>
        /// <param name="certFile">Fullpath to a certificat file</param>
        /// <param name="file">Output file</param>
        /// <returns>True if successfull, otherwice false</returns>
        private bool Build(string sourceFolder, string destinationFolder, string serialId, string certFile, out string file) {

            file = string.Empty; // TODO: Remove

            // Assure destination folder
            if (!Directory.Exists(destinationFolder)) {
                Directory.CreateDirectory(destinationFolder);
            }

            Console.WriteLine("NOTICE: Building {0} to {1}", sourceFolder, destinationFolder);

            string builderTool = Path.Combine(sourceFolder, "GenerateInstaller.exe");


            // Start the Building tool process
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = builderTool;
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.RedirectStandardOutput = true;
            startInfo.Arguments = "UniqueDownloadKey=" + serialId + " " +
                "DestinationDir=" + "\"" + destinationFolder + "\"" + " " +
                "PathToCertificateFile=" + "\"" + certFile + "\"";
            startInfo.WorkingDirectory = sourceFolder;

            try {
                using (Process exeProcess = Process.Start(startInfo)) {

                    Console.WriteLine(exeProcess.StandardOutput.ReadToEnd());

                    exeProcess.WaitForExit();
                    if (exeProcess.ExitCode != 0) {
                        Console.WriteLine("ERROR: Building tool exited with error code {0}", exeProcess.ExitCode);
                        return false;
                    }

                    string[] files = Directory.GetFiles(destinationFolder);
                    if (files == null || files.Length == 0) {
                        Console.WriteLine("ERROR: Building tool did not generate an output file in destination folder {0}", destinationFolder);
                        return false;
                    }
                    file = files[0];
                    return true;
                }
            }
            catch (Exception e) {
                Console.WriteLine("ERROR: Can not start the building tool {0}. {1}", builderTool, e.Message);
                return false;
            }
        }


    }
}
