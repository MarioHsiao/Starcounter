using Administrator.Server.Model;
using Starcounter.Administrator.API.Handlers;
using System;
using System.IO;
using System.Text;

namespace Administrator.Server.Managers {

    /// <summary>
    /// Responsable for the playlist
    /// * Adding and removing application from 'playlist'
    /// * Get a list of all installed applications
    /// </summary>
    public class PlaylistManager {

        /// <summary>
        /// Get configuration filename
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        private static string GetConfigurationFile(string databaseName) {
            string databaseDirectory = RootHandler.Host.Runtime.GetServerInfo().Configuration.GetResolvedDatabaseDirectory();
            return Path.Combine(databaseDirectory, Path.Combine(databaseName, "applications.json"));
        }

        /// <summary>
        /// Get a list of installed applications
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="playlist"></param>
        public static void GetInstalledApplications(string databaseName, out Representations.JSON.Playlist playlist) {

            var databaseConfigPath = GetConfigurationFile(databaseName);

            ReadConfiguration(databaseConfigPath, out playlist);
        }

        /// <summary>
        /// Read configuration
        /// </summary>
        /// <param name="path"></param>
        /// <param name="playlist"></param>
        private static void ReadConfiguration(string path, out Representations.JSON.Playlist playlist) {

            playlist = new Representations.JSON.Playlist();

            //String line;
            //using (StreamReader sr = new StreamReader(path)) {
            //    line = sr.ReadToEnd();
            //}
            //playlist.PopulateFromJson(line);

            if (File.Exists(path)) {
                playlist.PopulateFromJson(File.ReadAllText(path, Encoding.UTF8));
            }

            //byte[] buffer = File.ReadAllBytes(path);
            //playlist.PopulateFromJson(buffer, buffer.Length);

        }

        /// <summary>
        /// Save configuration
        /// </summary>
        /// <param name="path"></param>
        /// <param name="playlist"></param>
        private static void SaveConfiguration(string path, Representations.JSON.Playlist playlist) {

            File.WriteAllBytes(path, playlist.ToJsonUtf8());

            //            throw new NotImplementedException("SaveConfiguration");

        }

        /// <summary>
        /// Install application
        /// Add to 'playlist'
        /// </summary>
        /// <param name="application"></param>
        public static void InstallApplication(DatabaseApplication application) {

            Representations.JSON.Playlist playlist = new Representations.JSON.Playlist();
            GetInstalledApplications(application.DatabaseName, out playlist);

            var item = playlist.Deployed.Add();
            item.Namespace = application.Namespace;
            item.Channel = application.Channel;
            item.AppName = application.AppName;
            item.Version = application.Version;
            item.Arguments = application.Arguments;

            // Save json.
            var databaseConfigPath = GetConfigurationFile(application.DatabaseName);
            SaveConfiguration(databaseConfigPath, playlist);

            application.IsInstalled = true;
        }

        /// <summary>
        /// Uninstall application
        /// Remove from 'playlist'
        /// </summary>
        /// <param name="application"></param>
        public static void UninstallApplication(DatabaseApplication application) {

            Representations.JSON.Playlist playlist = new Representations.JSON.Playlist();
            GetInstalledApplications(application.DatabaseName, out playlist);

            if (application.IsDeployed) {

                // Find application in playlist
                foreach (var item in playlist.Deployed) {

                    if (string.Equals(item.Namespace, application.Namespace, StringComparison.CurrentCultureIgnoreCase) &&
                        string.Equals(item.Channel, application.Channel, StringComparison.CurrentCultureIgnoreCase) &&
                        string.Equals(item.Version, application.Version, StringComparison.CurrentCultureIgnoreCase)) {
                        playlist.Deployed.Remove(item);
                        break;
                    }
                }
            }
            else {
                // Find application in playlist
                foreach (var item in playlist.Local) {

                    if (string.Equals(item.AppName, application.AppName, StringComparison.CurrentCultureIgnoreCase) &&
                        string.Equals(item.Executable, application.Executable, StringComparison.CurrentCultureIgnoreCase)) {
                        playlist.Local.Remove(item);
                        break;
                    }
                }
            }

            // Save json.
            var databaseConfigPath = GetConfigurationFile(application.DatabaseName);
            SaveConfiguration(databaseConfigPath, playlist);

            application.IsInstalled = false;
        }
    }
}
