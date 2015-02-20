using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using System.Collections.Generic;

namespace StarcounterApplicationWebSocket.API.Versions {
    internal class Utils {
        public static void BootStrap(ushort port) {

            //Handle.GET(port, "/reset", (Request request) => {

            //    Db.Transact(() => {

            //        LogWriter.WriteLine(string.Format("Resetting database."));

            //        SqlResult<VersionSource> versionSources = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o");

            //        foreach (VersionSource item in versionSources) {
            //            item.Delete();
            //        }

            //        SqlResult<VersionBuild> versionBuilds = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o");
            //        foreach (VersionBuild item in versionBuilds) {
            //            item.Delete();
            //        }

            //        SqlResult<Somebody> sombodies = Db.SlowSQL<Somebody>("SELECT o FROM Somebody o");
            //        foreach (Somebody item in sombodies) {
            //            item.Delete();
            //        }

            //        // Reset settings
            //        VersionHandlerSettings settings = VersionHandlerApp.Settings;
            //        settings.Delete();
            //        VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();
            //        LogWriter.Init(VersionHandlerApp.Settings.LogFile);

            //    });

            //    if (AssureEmails() == false) {
            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = "Could not generate unique id, email import aborted" };
            //    }

            //    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            //});


            Handle.GET(port, "/refresh", (Request request) => {

                LogWriter.WriteLine(string.Format("NOTICE: Refresh environment (database and files)."));

                VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();

                // Set log filename to logwriter
                LogWriter.Init(VersionHandlerApp.Settings.LogFile);

                SyncData.Start();

                VersionHandlerApp.UnpackWorker.Trigger();
                VersionHandlerApp.BuildkWorker.Trigger();
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });


        }


        /// <summary>
        /// Generate unique download key
        /// </summary>
        /// <returns>key otherwise null</returns>
        private static string GetUniqueDownloadKey() {
            string key;

            for (int i = 0; i < 50; i++) {
                key = DownloadID.GenerateNewUniqueDownloadKey();
                var result = Db.SlowSQL("SELECT o FROM Somebody o WHERE o.DownloadKey=?", key).First;
                if (result == null) {
                    return key;
                }
            }
            return null;
        }


        /// <summary>
        /// Check if a directory is empty
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if directory is empty otherwise false</returns>
        public static bool IsDirectoryEmpty(string path) {
            //    return !Directory.EnumerateFileSystemEntries(path).Any();
            IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
            using (IEnumerator<string> en = items.GetEnumerator()) {
                return !en.MoveNext();
            }
        }
    }
}
