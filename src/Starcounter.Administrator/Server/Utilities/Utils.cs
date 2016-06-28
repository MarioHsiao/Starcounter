using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter.Administrator.Server.Handlers;
using Starcounter.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Utilities {
    public class Utils {

        /// <summary>
        /// Create Directory structure 
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Created root base folder</returns>
        public static string CreateDirectory(string path) {
            string createdBaseFolder = null;

            DirectoryInfo di = new DirectoryInfo(path);

            while (di.Exists == false) {
                createdBaseFolder = di.FullName;
                di = di.Parent;
            }

            Directory.CreateDirectory(path);

            return createdBaseFolder;
        }

        /// <summary>
        /// Check if directory is empty
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static bool IsDirectoryEmpty(string folder) {
            return !Directory.EnumerateFileSystemEntries(folder).Any();
        }


        /// <summary>
        /// Replaces certain parameter in XML file.
        /// </summary>
        /// <param name="pathToXml"></param>
        /// <param name="paramName"></param>
        /// <param name="paramNewValue"></param>
        /// <returns></returns>
        public static bool ReplaceXMLParameterInFile(
              String pathToXml,
              String paramName,
              String paramNewValue) {
            if (!File.Exists(pathToXml))
                return false;

            String fileContents = File.ReadAllText(pathToXml);

            // Searching for the first entry.
            Int32 startIndex = fileContents.IndexOf(paramName);
            if (startIndex <= 0)
                return false;

            // Searching the end of the parameter value.
            Int32 endIndex = fileContents.IndexOf('<', startIndex);
            String strToReplace = fileContents.Substring(startIndex, endIndex - startIndex);

            // Replacing with new parameter value.
            fileContents = fileContents.Replace(strToReplace, paramName + ">" + paramNewValue);

            // Saving modified XML file contents.
            File.WriteAllText(pathToXml, fileContents);

            return true;
        }


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        //private long GetTotalFreeSpace(string driveName) {
        //    foreach (DriveInfo drive in DriveInfo.GetDrives()) {
        //        if (drive.IsReady && drive.Name == driveName) {
        //            return drive.TotalFreeSpace;
        //        }
        //    }
        //    return -1;
        //}

        public static bool GetCPUAndMemeoryUsage(out float cpu, out float ram) {

            try {
                PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpu = theCPUCounter.NextValue();
                System.Threading.Thread.Sleep(500);
                cpu = theCPUCounter.NextValue();

                PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
                ram = theMemCounter.NextValue();
                return true;
            }
            catch (Exception) {
                cpu = -1;
                ram = -1;
                return false;
            }
        }

        #region Callback
        public static void CallBack(Action callback) {

            try {
                if (callback != null) {
                    callback();
                }
            }
            catch (Exception e) {
                StarcounterAdminAPI.AdministratorLogSource.LogError(string.Format("UnhandledException, {0}", e.ToString()));
            }
        }

        public static void CallBack(Action<string> callback, string text) {

            try {
                if (callback != null) {
                    callback(text);
                }
            }
            catch (Exception e) {
                StarcounterAdminAPI.AdministratorLogSource.LogError(string.Format("UnhandledException, {0}", e.ToString()));
            }
        }

        public static void CallBack(Action<DatabaseApplication> callback, DatabaseApplication databaseApplication) {

            if (callback != null) {
                try {
                    callback(databaseApplication);
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogError(string.Format("UnhandledException, {0}", e.ToString()));
                }
            }
        }

        public static void CallBack(Action<int,string> callback, int code, string text) {

            if (callback != null) {
                try {
                    callback(code,text);
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogError(string.Format("UnhandledException, {0}", e.ToString()));
                }
            }
        }

        #endregion

    }
}
