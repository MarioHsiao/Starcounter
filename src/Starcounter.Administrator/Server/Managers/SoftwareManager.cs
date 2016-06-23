using Administrator.Server.Model;
using Starcounter.Administrator.API.Handlers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Administrator.Server.Managers {

    public class SoftwareManager {

        private static Semaphore Semaphore = new Semaphore(1, 1);
        public const string SoftwareReferenceFileName = "software.ref";
        public const string InstalledSoftwareFileName = "software.json";

        #region Installed software Configuration

        /// <summary>
        /// Get Software configuration file
        /// </summary>
        /// <param name="databaseName">Database name/id</param>
        /// <returns></returns>
        private static string GetSoftwareConfigurationFile(string databaseName) {

            string databaseDirectory = RootHandler.Host.Runtime.GetServerInfo().Configuration.GetResolvedDatabaseDirectory();
            return Path.Combine(databaseDirectory, Path.Combine(databaseName, InstalledSoftwareFileName));
        }

        /// <summary>
        /// Get Installed Software
        /// </summary>
        /// <param name="database">Database</param>
        public static Representations.JSON.InstalledSoftwareItems GetInstalledSoftware(Database database) {

            if (database == null) throw new ArgumentNullException("database");

            Representations.JSON.InstalledSoftwareItems installedSoftwareItems;

            var databaseConfigPath = GetSoftwareConfigurationFile(database.ID);
            LoadSoftwareConfiguration(databaseConfigPath, out installedSoftwareItems);

            return installedSoftwareItems;
        }

        /// <summary>
        /// Get Installed Software
        /// </summary>
        /// <param name="database"></param>
        /// <param name="id">Software id</param>
        /// <returns></returns>
        public static Representations.JSON.InstalledSoftware GetInstalledSoftware(Database database, string id) {

            if (database == null) throw new ArgumentNullException("database");
            if (string.IsNullOrEmpty(id)) throw new NullReferenceException("id");

            Representations.JSON.InstalledSoftwareItems installedSoftwareItems;
            LoadSoftwareConfiguration(GetSoftwareConfigurationFile(database.ID), out installedSoftwareItems);

            foreach (var software in installedSoftwareItems.Items) {
                if (software.ID == id) {
                    return software;
                }
            }

            return null;
        }

        /// <summary>
        /// Get Installed Software
        /// </summary>
        /// <param name="database"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Representations.JSON.InstalledSoftware GetInstalledSoftwareBysourceUrl(Database database, string sourceUrl) {

            if (database == null) throw new ArgumentNullException("database");
            if (string.IsNullOrEmpty(sourceUrl)) throw new NullReferenceException("sourceUrl");

            Representations.JSON.InstalledSoftwareItems installedSoftwareItems;
            LoadSoftwareConfiguration(GetSoftwareConfigurationFile(database.ID), out installedSoftwareItems);

            foreach (var software in installedSoftwareItems.Items) {
                if (string.Equals(software.SourceUrl, sourceUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    return software;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if software exists
        /// </summary>
        /// <param name="database"></param>
        /// <param name="id">Software id</param>
        /// <returns></returns>
        public static bool SoftwareExist(Database database, string id) {

            if (database == null) throw new ArgumentNullException("database");
            if (string.IsNullOrEmpty(id)) throw new NullReferenceException("id");

            return GetInstalledSoftware(database, id) != null;
        }

        /// <summary>
        /// Load configuration
        /// </summary>
        /// <param name="path"></param>
        /// <param name="installedSoftwareItems"></param>
        private static void LoadSoftwareConfiguration(string path, out Representations.JSON.InstalledSoftwareItems installedSoftwareItems) {

            installedSoftwareItems = new Representations.JSON.InstalledSoftwareItems();

            if (File.Exists(path)) {
                installedSoftwareItems.PopulateFromJson(File.ReadAllText(path, Encoding.UTF8));
            }
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        /// <param name="file"></param>
        /// <param name="installedSoftwareItems"></param>
        private static void SaveSoftwareConfiguration(string file, Representations.JSON.InstalledSoftwareItems installedSoftwareItems) {

            if (installedSoftwareItems.Items.Count == 0) {
                if (File.Exists(file)) {
                    File.Delete(file);
                    return;
                }
            }

            File.WriteAllBytes(file, installedSoftwareItems.ToJsonUtf8());
        }

        /// <summary>
        /// Add software to configuration
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sourceUrl"></param>
        /// <param name="softwareContent"></param>
        /// <returns></returns>
        private static Representations.JSON.InstalledSoftware AddSoftwareToConfiguration(Database database, string sourceUrl, IEnumerable<DatabaseApplication> softwareContent) {

            Representations.JSON.InstalledSoftwareItems installedSoftwareItems;
            LoadSoftwareConfiguration(GetSoftwareConfigurationFile(database.ID), out installedSoftwareItems);

            string newID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(database.ID + sourceUrl);
            // Check for duplicates, if a suite content has been modified we remove the old and add the new suite
            foreach (var suite in installedSoftwareItems.Items) {
                if (suite.ID == newID) {
                    installedSoftwareItems.Items.Remove(suite);
                    break;
                }
            }

            Representations.JSON.InstalledSoftware item = installedSoftwareItems.Items.Add();

            item.ID = newID;
            item.SourceUrl = sourceUrl;

            foreach (DatabaseApplication databaseApplication in softwareContent) {
                Representations.JSON.InstalledSoftware.ContentsElementJson cItem = item.Contents.Add();
                cItem.Namespace = databaseApplication.Namespace;
                cItem.Channel = databaseApplication.Channel;
            }

            SaveSoftwareConfiguration(GetSoftwareConfigurationFile(database.ID), installedSoftwareItems);
            return item;
        }

        /// <summary>
        /// Remove software from configuration
        /// </summary>
        /// <param name="database"></param>
        /// <param name="id">Software id</param>
        private static void RemoveSoftwareConfiguration(Database database, string id) {

            Representations.JSON.InstalledSoftwareItems installedSoftwareItems;

            LoadSoftwareConfiguration(GetSoftwareConfigurationFile(database.ID), out installedSoftwareItems);

            foreach (Representations.JSON.InstalledSoftware software in installedSoftwareItems.Items) {
                if (software.ID == id) {
                    installedSoftwareItems.Items.Remove(software);
                    SaveSoftwareConfiguration(GetSoftwareConfigurationFile(database.ID), installedSoftwareItems);
                    return;
                }
            }
        }

        #endregion

        #region Reference counter file

        /// <summary>
        /// Get reference counter
        /// </summary>
        /// <param name="database"></param>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private static int GetReferenceCounter(Database database, string nameSpace, string channel) {

            string referenceFile = GetReferenceFile(database, nameSpace, channel);
            int references;
            if (ReadReferenceCounter(referenceFile, out references)) {
                return references;
            }
            // No reference file
            return -1;
        }

        /// <summary>
        /// Increase Reference Counter
        /// </summary>
        /// <param name="database"></param>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        private static void IncreaseReferenceCounter(Database database, string nameSpace, string channel) {

            int references = GetReferenceCounter(database, nameSpace, channel);
            if (references == -1) {
                // No reference file
                references = 0;
            }

            // increase
            references++;

            string referenceFile = GetReferenceFile(database, nameSpace, channel);
            WriteReferenceCounter(referenceFile, references);
        }

        /// <summary>
        /// Decreases Reference Number
        /// </summary>
        /// <remarks>
        /// If number is zero or less the reference file will be deleted.
        /// </remarks>
        /// <param name="database"></param>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        private static void DecreasesReferenceCounter(Database database, string nameSpace, string channel) {

            int references = GetReferenceCounter(database, nameSpace, channel);
            if (references == -1) {
                // No reference file
                references = 0;
            }

            // Decreases
            references--;

            string referenceFile = GetReferenceFile(database, nameSpace, channel);

            if (references <= 0) {
                // Delete file
                if (File.Exists(referenceFile)) {
                    File.Delete(referenceFile);
                }
            }
            else {
                WriteReferenceCounter(referenceFile, references);
            }
        }

        /// <summary>
        /// Write a number in a file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="value"></param>
        private static void WriteReferenceCounter(string file, int value) {

            using (StreamWriter sw = File.CreateText(file)) {
                sw.WriteLine(value);
            }
        }

        /// <summary>
        /// Read a number from a file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="value"></param>
        /// <returns>true if successfull otherwise false</returns>
        private static bool ReadReferenceCounter(string file, out int value) {

            value = -1;
            if (File.Exists(file)) {
                using (StreamReader sr = File.OpenText(file)) {
                    string firstLine = sr.ReadLine();
                    return int.TryParse(firstLine, out value);
                }
            }
            return false;
        }

        /// <summary>
        /// Get Reference filepath
        /// </summary>
        /// <example>
        /// c:\Users\john\Documents\Starcounter\Personal\Databases\default\apps\StarcounterSamples.Launcher\Stable\app.ref
        /// </example>
        /// <param name="database"></param>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private static string GetReferenceFile(Database database, string nameSpace, string channel) {

            string destinationFolder = DeployManager.GetApplicationFolder(database, nameSpace, channel);
            return Path.Combine(destinationFolder, SoftwareReferenceFileName);
        }

        #endregion

        #region Install 

        /// <summary>
        /// Install software
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sourceUrl">Application or Suite source url</param>
        /// <param name="sourceUrls">Content source urls of an suite or null/empty</param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        /// <returns>false if timeout/busy otherwice true</returns>
        public static bool InstallSoftware(Database database, string sourceUrl, IEnumerable<string> sourceUrls, Action<string> completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            if (database == null) throw new ArgumentNullException("database");
            if (string.IsNullOrEmpty(sourceUrl)) throw new ArgumentNullException("sourceUrl");

            try {

                bool timeout = Semaphore.WaitOne(100);
                if (timeout == false) {
                    return false;
                }
                ConcurrentQueue<string> softwareToInstall = new ConcurrentQueue<string>();
                ConcurrentQueue<string> revertList = new ConcurrentQueue<string>();

                if (sourceUrls != null) {
                    foreach (string sourceUrl2 in sourceUrls) {
                        softwareToInstall.Enqueue(sourceUrl2);
                    }
                }

                Representations.JSON.InstalledSoftware installedSoftware = GetInstalledSoftwareBysourceUrl(database, sourceUrl);
                if (installedSoftware != null) {
                    // Software already installed
                    Utilities.Utils.CallBack(completionCallback, installedSoftware.ID);
                    Semaphore.Release();
                    return true;
                }

                InstallSoftwareQueue(database, softwareToInstall, () => {
                    // Success
                    IList<DatabaseApplication> installedDatabaseApplications = new List<DatabaseApplication>();
                    foreach (string installedSourceUrl in sourceUrls) {
                        DatabaseApplication databaseApplication = database.GetApplicationBySourceUrl(installedSourceUrl);
                        if (databaseApplication == null) {
                            Starcounter.Administrator.Server.Handlers.StarcounterAdminAPI.AdministratorLogSource.LogError("Installed software could not be internally found by sourceUrl:" + installedSourceUrl);
                            continue;
                        }
                        installedDatabaseApplications.Add(databaseApplication);
                    }

                    Representations.JSON.InstalledSoftware installedSoftwaresItemJson = SoftwareManager.AddSoftwareToConfiguration(database, sourceUrl, installedDatabaseApplications);
                    Utilities.Utils.CallBack(completionCallback, installedSoftwaresItemJson.ID);
                    Semaphore.Release();
                }, (code, text) => {
                    // Progress
                    if (code == 1) { // Installed software successfully
                        revertList.Enqueue(text); // text = Source url
                    }

                    Utilities.Utils.CallBack(progressCallback, text);

                }, (code, text) => {
                    // Error

                    // Outside ServerManager.ServerInstance lock

                    revertList = new ConcurrentQueue<string>(revertList.Reverse());
                    // Revert
                    try {
                        UnInstallSoftwareQueue(database, revertList, null, () => {
                            // success 
                            Utilities.Utils.CallBack(errorCallback, code, text);
                            Semaphore.Release();
                        },
                        (text2) => {
                            // Progress
                            Utilities.Utils.CallBack(progressCallback, text2);
                        }, (code2, text2) => {
                            // error revert 
                            // TOOD: how to clean up?
                            Utilities.Utils.CallBack(errorCallback, code, text + ", " + text2);
                            Semaphore.Release();
                        });
                    }
                    catch {
                        Semaphore.Release();
                    }
                });
            }
            catch (Exception e) {
                Semaphore.Release();
                throw e;
            }
            return true;
        }

        /// <summary>
        /// Process Installation Software queue
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sourceUrls"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void InstallSoftwareQueue(Database database, ConcurrentQueue<string> sourceUrls, Action completionCallback = null, Action<int, string> progressCallback = null, Action<int, string> errorCallback = null) {

            if (sourceUrls.Count == 0) {
                Utilities.Utils.CallBack(completionCallback);
                return;
            }

            // Pop an app from the list
            string installSoftwareSourceUrl;
            if (sourceUrls.TryDequeue(out installSoftwareSourceUrl) == false) {
                Utilities.Utils.CallBack(errorCallback, -1, "Internal error, failed to dequeue item from ConcurrentQueue.");
                return;
            }

            // Check if Software is already installed.
            DatabaseApplication databaseApplication = database.GetApplicationBySourceUrl(installSoftwareSourceUrl);
            if (databaseApplication != null && databaseApplication.IsDeployed) {

                if (databaseApplication.IsInstalled == false) {
                    // Application is not auto-started.
                    // This can occure if the admin user changed the property in the administrator.
                    // Will let this property be untoched.
                }

                // Increase reference counter.
                IncreaseReferenceCounter(database, databaseApplication.Namespace, databaseApplication.Channel);

                // Application already installed
                // Process next item in the list
                InstallSoftwareQueue(database, sourceUrls, completionCallback, progressCallback, errorCallback);
                return;
            }

            // Install Software
            InstallSoftware(database, installSoftwareSourceUrl, true, (installedDatabaseApplication) => {
                // Success

                // Report installed software
                Utilities.Utils.CallBack(progressCallback, 1, installedDatabaseApplication.SourceUrl);  // Installed software successfully

                // Process next item in the list
                InstallSoftwareQueue(database, sourceUrls, completionCallback, progressCallback, errorCallback);

            }, (text) => {

                Utilities.Utils.CallBack(progressCallback, 0, text);

            }, (code, text) => {

                if( code == -5) {
                    // Installed correct but failed to start
                    Utilities.Utils.CallBack(progressCallback, 1, installSoftwareSourceUrl);  // Installed software successfully
                }

                Utilities.Utils.CallBack(errorCallback, code, text); 

                
            });
        }

        /// <summary>
        /// Install software
        /// Download->Install->(Start if database is running)
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sourceUrl"></param>
        /// <param name="canBeUninstalled"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void InstallSoftware(Database database, string sourceUrl, bool canBeUninstalled, Action<DatabaseApplication> completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            Utilities.Utils.CallBack(progressCallback, "Installing application");

            DeployManager.Download(sourceUrl, database, false, (deployedApplication) => {
                // Success
                #region Install Application
                deployedApplication.InstallApplication((installedApplication) => {

                    #region SetCanBeUninstalledFlag

                    installedApplication.SetCanBeUninstalledFlag(canBeUninstalled, (databaseApplication) => {

                        // Success
                        // If database is started start application
                        if (installedApplication.Database.IsRunning) {

                            Utilities.Utils.CallBack(progressCallback, "Starting " + databaseApplication.DisplayName);

                            #region Start Application
                            installedApplication.StartApplication((startedApplication) => {

                                IncreaseReferenceCounter(startedApplication.Database, startedApplication.Namespace, startedApplication.Channel);
                                Utilities.Utils.CallBack(completionCallback, startedApplication);
                            }, (startedApplication, wasCancelled, title, message, helpLink) => {
                                Utilities.Utils.CallBack(errorCallback, -5, message);
                            });
                            #endregion
                        }
                        else {

                            IncreaseReferenceCounter(databaseApplication.Database, databaseApplication.Namespace, databaseApplication.Channel);
                            Utilities.Utils.CallBack(completionCallback, databaseApplication);
                        }

                    }, (dapplication, wasCancelled, title, message, helpLink) => {
                        // Error
                        Utilities.Utils.CallBack(errorCallback, -7, message);
                    });
                    #endregion

                }, (installedApplication, wasCancelled, title, message, helpLink) => {
                    // Error
                    Utilities.Utils.CallBack(errorCallback, -4, message);
                });

                #endregion
            }, (message) => {
                Utilities.Utils.CallBack(errorCallback, -1, message);
            });
        }

        #endregion

        #region Uninstall

        /// <summary>
        /// Uninstalls a software
        /// </summary>
        /// <param name="database"></param>
        /// <param name="id">Software id</param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        /// <returns>false if timeout/busy otherwice true</returns>
        public static bool UnInstallSoftware(Database database, string id, Action completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            if (database == null) throw new ArgumentNullException("database");
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id");

            try {

                bool timeout = Semaphore.WaitOne(15000);
                if (timeout == false) {
                    return false;
                }

                UnInstallSoftwareWithoutSemaphoreLock(database, id, () => {

                    Utilities.Utils.CallBack(completionCallback);
                    Semaphore.Release();

                }, progressCallback, (code, text) => {

                    Utilities.Utils.CallBack(errorCallback, code, text);
                    Semaphore.Release();
                });
            }
            catch (Exception e) {
                Semaphore.Release();
                throw e;
            }
            return true;
        }

        /// <summary>
        /// Uninstall software
        /// Delete application(s)
        /// </summary>
        /// <param name="database"></param>
        /// <param name="id"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void UnInstallSoftwareWithoutSemaphoreLock(Database database, string id, Action completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            Representations.JSON.InstalledSoftware installedSoftwareJson = SoftwareManager.GetInstalledSoftware(database, id);

            ConcurrentQueue<string[]> softwareContent = new ConcurrentQueue<string[]>();

            foreach (var item in installedSoftwareJson.Contents) {
                softwareContent.Enqueue(new string[] { item.Namespace, item.Channel });
            }

            UnInstallSoftwareQueue(database, softwareContent, null, () => {

                SoftwareManager.RemoveSoftwareConfiguration(database, installedSoftwareJson.ID);
                Utilities.Utils.CallBack(completionCallback);

            }, progressCallback, (code, text) => {

                Utilities.Utils.CallBack(errorCallback, code, text);
            });
        }

        /// <summary>
        /// Uninstallation loop for removing application and all it's versions
        /// </summary>
        /// <param name="database"></param>
        /// <param name="nameSpaceChannelList"></param>
        /// <param name="errors"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void UnInstallSoftwareQueue(Database database, ConcurrentQueue<string[]> nameSpaceChannelList, ConcurrentQueue<string> errors, Action completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            if (nameSpaceChannelList.Count == 0) {
                HandleDone(errors, completionCallback, errorCallback);
                return;
            }

            // Pop an app from the list
            string[] nameSpaceChannel;
            if (nameSpaceChannelList.TryDequeue(out nameSpaceChannel) == false) {
                Utilities.Utils.CallBack(errorCallback, -1, "Internal error, failed to dequeue item from queue.");
                return;
            }

            string nameSpace = nameSpaceChannel[0];
            string channel = nameSpaceChannel[1];

            // UnInstall application
            UnInstallSoftware(database, nameSpace, channel, () => {
                // Success
                // Process next item in the list
                UnInstallSoftwareQueue(database, nameSpaceChannelList, errors, completionCallback, progressCallback, errorCallback);

            }, progressCallback, (code, text) => {

                if (errors == null) {
                    errors = new ConcurrentQueue<string>();       // FIFO
                }

                // Collect errors and show them when list if processed
                errors.Enqueue(text);

                // Process next item in the list
                UnInstallSoftwareQueue(database, nameSpaceChannelList, errors, completionCallback, progressCallback, errorCallback);
            });
        }

        /// <summary>
        /// Uninstallation loop for removing application
        /// </summary>
        /// <param name="database"></param>
        /// <param name="sourceUrls"></param>
        /// <param name="errors"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void UnInstallSoftwareQueue(Database database, ConcurrentQueue<string> sourceUrls, ConcurrentQueue<string> errors, Action completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            if (sourceUrls.Count == 0) {
                HandleDone(errors, completionCallback, errorCallback);
                return;
            }

            // Pop an app from the list
            string sourceUrl;
            if (sourceUrls.TryDequeue(out sourceUrl) == false) {
                Utilities.Utils.CallBack(errorCallback, -1, "Internal error, failed to dequeue item from queue.");
                return;
            }

            DatabaseApplication databaseApplication = database.GetApplicationBySourceUrl(sourceUrl);
            if (databaseApplication == null) {
                // Application not installed.
                UnInstallSoftwareQueue(database, sourceUrls, errors, completionCallback, progressCallback, errorCallback);
                return;
            }

            // UnInstall application
            UnInstallApplication(database, databaseApplication, (deletedDatabaseApplication) => {
                // Success
                // Process next item in the list
                UnInstallSoftwareQueue(database, sourceUrls, errors, completionCallback, progressCallback, errorCallback);

            }, progressCallback, (code, text) => {

                // Error
                if (errors == null) {
                    errors = new ConcurrentQueue<string>();
                }

                // Collect errors and show them when list if processed
                errors.Enqueue(text);

                // Process next item in the list
                UnInstallSoftwareQueue(database, sourceUrls, errors, completionCallback, progressCallback, errorCallback);
            });
        }

        /// <summary>
        /// Handle when list is processed
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        private static void HandleDone(ICollection errors, Action completionCallback = null, Action<int, string> errorCallback = null) {

            if (errors != null && errors.Count > 0) {
                string errorMessage = string.Empty;
                foreach (string err in errors) {
                    if (!string.IsNullOrEmpty(errorMessage)) {
                        errorMessage += " ";
                    }
                    errorMessage += err;
                }
                Utilities.Utils.CallBack(errorCallback, -2, errorMessage);
                return;
            }

            Utilities.Utils.CallBack(completionCallback);
        }

        /// <summary>
        /// Uninstalls all application all versions
        /// </summary>
        /// <param name="database"></param>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void UnInstallSoftware(Database database, string nameSpace, string channel, Action completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            int referenceCounter = SoftwareManager.GetReferenceCounter(database, nameSpace, channel);

            if (referenceCounter > 1) {
                SoftwareManager.DecreasesReferenceCounter(database, nameSpace, channel);
                Utilities.Utils.CallBack(completionCallback);
                return;
            }

            if (referenceCounter <= 0) {
                Utilities.Utils.CallBack(completionCallback);
                return;
            }

            // Uninstall all versions of the software
            ConcurrentQueue<string> apps = new ConcurrentQueue<string>();
            IList<DatabaseApplication> databaseApplications = database.GetApplications(nameSpace, channel);
            foreach (DatabaseApplication databaseApplication in databaseApplications) {
                // An empty sourceUrl can be a "local" started application, application not deployed/installed from appstore.
                if (databaseApplication.IsDeployed) {
                    apps.Enqueue(databaseApplication.SourceUrl);
                }
            }

            UnInstallSoftwareQueue(database, apps, null, () => {

                SoftwareManager.DecreasesReferenceCounter(database, nameSpace, channel);
                Utilities.Utils.CallBack(completionCallback);
            }, progressCallback, (code, text) => {
                Utilities.Utils.CallBack(errorCallback, code, text);
            });
        }

        /// <summary>
        /// Uninstall application.
        /// Delete application
        /// </summary>
        /// <param name="database"></param>
        /// <param name="databaseApplication"></param>
        /// <param name="completionCallback"></param>
        /// <param name="progressCallback"></param>
        /// <param name="errorCallback"></param>
        private static void UnInstallApplication(Database database, DatabaseApplication databaseApplication, Action<DatabaseApplication> completionCallback = null, Action<string> progressCallback = null, Action<int, string> errorCallback = null) {

            Utilities.Utils.CallBack(progressCallback, "Uninstalling application");

            databaseApplication.DeleteApplication(true, (deletedApplication) => {

                Utilities.Utils.CallBack(completionCallback, deletedApplication);
            }, (startedApplication, wasCancelled, title, message, helpLink) => {

                Utilities.Utils.CallBack(errorCallback, -3, message);
            });
        }

        #endregion
    }
}
