using System;
using System.IO;
using Microsoft.Win32;
using System.Configuration.Install;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Management;
using System.Security.Principal;
using System.Security.AccessControl;
using Starcounter.Controls;
using Starcounter;
using Microsoft.VisualBasic.Devices;
using System.Collections.Generic;
using Starcounter.Internal;
using System.Xml;
using System.Runtime.ExceptionServices;

namespace Starcounter.InstallerEngine {
    public class Utilities {
        /// <summary>
        /// Returns true if running on build server.
        /// </summary>
        static Nullable<Boolean> _runningOnBuildServer = null;
        public static Boolean RunningOnBuildServer() {
            if (_runningOnBuildServer != null)
                return _runningOnBuildServer.Value;

            // Checking if running on build server.
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True") {
                _runningOnBuildServer = true;
                return true;
            }

            _runningOnBuildServer = false;
            return false;
        }

        /// <summary>
        /// Checks if child directory is contained within its parent.
        /// </summary>
        public static bool ParentChildDirectory(String parentDir, String childDir) {
            if (Path.GetDirectoryName(childDir).StartsWith(Path.GetDirectoryName(parentDir),
                StringComparison.InvariantCultureIgnoreCase)) {
                return true;
            }

            return false;
        }


        // Reads server installation path from configuration file.
        public static String ReadServerInstallationPath(String serverConfigPath) {
            if (!File.Exists(serverConfigPath))
                return null;

            XmlDocument serverXML = new XmlDocument();
            serverXML.Load(serverConfigPath);
            return (serverXML.GetElementsByTagName(MixedCodeConstants.ServerConfigDirName))[0].InnerText;
        }

        /// <summary>
        /// Compares if two directory paths are equal.
        /// </summary>
        /// <param name="dirPath1">First directory path.</param>
        /// <param name="dirPath2">Second directory path.</param>
        /// <returns>TRUE if paths are equal.</returns>
        public static bool EqualDirectories(string dirPath1, string dirPath2) {
            if (string.IsNullOrEmpty(dirPath1))
                throw new ArgumentNullException("dirPath1");

            if (string.IsNullOrEmpty(dirPath2))
                throw new ArgumentNullException("dirPath2");

            return string.Compare(
                Path.GetFullPath(dirPath1).TrimEnd('\\'),
                Path.GetFullPath(dirPath2).TrimEnd('\\'),
                StringComparison.CurrentCultureIgnoreCase) == 0;
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

        public static bool ReadValueFromXMLInFile(String pathToXml, String paramName, out String paramValue) {

            paramValue = null;

            if (!File.Exists(pathToXml))
                return false;

            String fileContents = File.ReadAllText(pathToXml);

            // Searching for the first entry.
            Int32 startIndex = fileContents.IndexOf("<" + paramName + ">");
            if (startIndex <= 0)
                return false;

            startIndex += paramName.Length + 2;

            // Searching the end of the parameter value.
            Int32 endIndex = fileContents.IndexOf('<', startIndex);
            paramValue = fileContents.Substring(startIndex, endIndex - startIndex);


            // Replacing with new parameter value.
            //            fileContents = fileContents.Replace(strToReplace, paramName + ">" + paramValue);


            return true;
        }

        /// <summary>
        /// Helping function to copy folders recursively.
        /// </summary>
        /// <param name="source">Source folder.</param>
        /// <param name="target">Destination folder.</param>
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) {
            // Traverse through all directories.
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

            // Traverse through all files.
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        /// <summary>
        /// Sets normal security attributes for files and folders (recursively).
        /// </summary>
        public static void SetNormalDirectoryAttributes(FileSystemInfo fsi) {
            if (fsi == null) return;

            // Trying to set file attributes.
            try { fsi.Attributes = FileAttributes.Normal; }
            catch { }

            // Checking if its a directory and go into recursion if yes.
            var di = fsi as DirectoryInfo;
            if (di != null) {
                // Setting the folder attribute.
                try { fsi.Attributes = FileAttributes.Directory; }
                catch { }

                // Trying to obtain sub-information for the folder.
                FileSystemInfo[] fsis = null;
                try {
                    // Checking if directory exists.
                    if (!Directory.Exists(fsi.FullName))
                        return;

                    fsis = di.GetFileSystemInfos();
                }
                catch { }

                // Iterating through each sub-folder.
                if (fsis != null) {
                    foreach (var dirInfo in fsis) {
                        // Go into recursion for each sub-directory/file.
                        SetNormalDirectoryAttributes(dirInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if directory is not empty (recursively).
        /// </summary>
        public static Boolean DirectoryIsNotEmpty(FileSystemInfo fsi) {
            // No file or directory exist.
            if (fsi == null) return false;

            // Checking if its a directory and go into recursion if yes.
            var dirInfo = fsi as DirectoryInfo;
            if (dirInfo != null) {
                // Trying to obtain sub-information for the folder.
                FileSystemInfo[] subFsis = null;
                try { subFsis = dirInfo.GetFileSystemInfos(); }
                catch { }

                // Iterating through each sub-folder element if any.
                if (subFsis != null) {
                    foreach (var subFsi in subFsis) {
                        // Go into recursion for each sub-directory/file.
                        if (DirectoryIsNotEmpty(subFsi)) return true;
                    }
                }

                // No subfolders/files inside.
                return false;
            }

            // Its a file.
            return true;
        }

        /// <summary>
        /// Creates a complete registry path if it does not exist.
        /// </summary>
        /// <param name="fullRegistryPath">Full path to a registry node.</param>
        /// <param name="systemWide">Indicates the personal or system-wide usage.</param>
        public static RegistryKey CreateRegistryPathIfNeeded(String fullRegistryPath, Boolean systemWide) {
            RegistryKey rk = null;

            String[] pathSplitUp = fullRegistryPath.Split('\\');
            if (!pathSplitUp[0].Equals("SOFTWARE", StringComparison.CurrentCultureIgnoreCase)) {
                throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM, "Full registry path should contain SOFTWARE element.");
            }

            // Checking for full path existence.
            if (!systemWide) {
                // Checking if registry key path already exists.
                rk = Registry.CurrentUser.OpenSubKey(fullRegistryPath, true);
                if (rk != null) return rk;
                rk = Registry.CurrentUser.OpenSubKey(pathSplitUp[0], true);
            }
            else {
                // Checking if registry key path already exists.
                rk = Registry.LocalMachine.OpenSubKey(fullRegistryPath, true);
                if (rk != null) return rk;
                rk = Registry.LocalMachine.OpenSubKey(pathSplitUp[0], true);
            }

            // Trying to open other keys in the path.
            for (int i = 1; i < pathSplitUp.Length; i++) {
                RegistryKey tempRk = rk.OpenSubKey(pathSplitUp[i], true);
                if (tempRk == null) {
                    rk = rk.CreateSubKey(pathSplitUp[i]);
                }
                else rk = tempRk;
            }

            return rk;
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        /// <summary>
        /// Removes Zone Identifier from all listed files.
        /// </summary>
        public static void RemoveZoneIdentifier(String dirPath, String[] filePatterns) {
            // Getting list of files with specified pattern.
            LinkedList<String> fileList = GetDirectoryFilesRegex(dirPath, filePatterns);

            // Walking through each file.
            foreach (String filePath in fileList) {
                // Removing zone identifier.
                DeleteFile(filePath + ":Zone.Identifier");
            }
        }

        /// <summary>
        /// Recursively searches files satisfying regex pattern and puts them into linked list.
        /// Linked list consists of complete file paths.
        /// </summary>
        public static LinkedList<String> GetDirectoryFilesRegex(String dirPath, String[] filePatterns) {
            // Creating empty file list.
            LinkedList<String> fileList = new LinkedList<String>();

            // First checking if directory exists.
            if (!Directory.Exists(dirPath))
                return fileList;

            // Getting all files recursively.
            String[] allFiles = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

            // Looking if any file matches the pattern.
            foreach (String pattern in filePatterns) {
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (String filePath in allFiles) {
                    // Getting file name from path.
                    String fileName = Path.GetFileName(filePath);

                    // Comparing with Regex.
                    if (rgx.IsMatch(fileName)) {
                        // Adding file if its not in the list already.
                        if (fileList.Find(filePath) == null)
                            fileList.AddLast(filePath);
                    }
                }
            }

            // Returning the linked list with complete file paths.
            return fileList;
        }

        /// <summary>
        /// Checks if the given path is on local drive not network.
        /// </summary>
        public static Boolean IsLocalPath(String fullDirPath) {
            DirectoryInfo dirInfo = new DirectoryInfo(fullDirPath);
            String rootFullName = dirInfo.Root.FullName;
            if (rootFullName.StartsWith("\\"))
                return false;

            // Checking each local drive (including mapped network drives).
            foreach (DriveInfo d in DriveInfo.GetDrives()) {
                if (String.Compare(rootFullName, d.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    return (d.DriveType != DriveType.Network);
            }

            return false;
        }

        /// <summary>
        /// Check if the path is a "developer" path i.e \bin\debug where developers build to.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Boolean IsDeveloperFolder(string path) {

            if (string.IsNullOrEmpty(path)) return false;

            if (Directory.Exists(System.IO.Path.Combine(path, "S"))) {

                if (File.Exists(System.IO.Path.Combine(path, "coalmine.dll"))) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Recursively checks if directory contains certain files (any of them).
        /// </summary>
        /// <param name="dirPath">Path to a directory to check.</param>
        /// <param name="filePatterns">List of file REGEX patterns to search for.</param>
        /// <returns>True if any matching file found.</returns>
        public static Boolean DirectoryContainsFilesRegex(String dirPath, String[] filePatterns) {
            // First checking if directory exists.
            if (!Directory.Exists(dirPath))
                return false;

            // Getting all files recursively.
            String[] allFiles = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);

            // Looking if any file matches the pattern.
            foreach (String pattern in filePatterns) {
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (String filePath in allFiles) {
                    // Getting file name from path.
                    String fileName = Path.GetFileName(filePath);

                    // Comparing with Regex.
                    if (rgx.IsMatch(fileName))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if we are installing on 64-bit machine.
        /// We can't simply check only the IntPtr.Size,
        /// since if running in 32-bit .NET Framework
        /// on 64-bit Windows, we will get 32-bit answer.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);

        /// <summary>
        /// Checks if current platform is 64 bit.
        /// </summary>
        public static Boolean Platform64Bit() {
            if (IntPtr.Size == 8) // Pure 64-bit platform.
            {
                return true;
            }

            // Checking current Windows version and then if current process is 32 under 64-bit.
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6) {
                using (Process p = Process.GetCurrentProcess()) {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal)) {
                        return false;
                    }
                    return retVal;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if machine memory is less than 4Gb.
        /// </summary>
        /// <returns>True if less than approximately 4Gb memory.</returns>
        public static Boolean LessThan4GbMemory() {
            ComputerInfo compInfo = new ComputerInfo();
            UInt64 totalMemoryBytes = compInfo.TotalPhysicalMemory;

            // Comparing just the approximate amount of megabytes.
            if ((totalMemoryBytes / 1048576) < 3500)
                return true;

            return false;
        }

        [DllImport("Starcounter.InstallerNativeHelper.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe extern static void sc_check_cpu_features(
            ref Boolean popcnt_instr
        );

        /// <summary>
        /// Checks if current machine is one core.
        /// </summary>
        /// <returns>True if one core.</returns>

        [HandleProcessCorruptedStateExceptions]
        public static void CheckProcessorRequirements() {
            // TODO: Check later if this CPU cores requirement is needed at all.
            /*if (Environment.ProcessorCount <= 1)
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED,
                    "To run Starcounter you must have more than one logical core on the machine.");
            }*/

            // Checking processor features.
            Boolean popcntInstr = false;

            try {
                sc_check_cpu_features(ref popcntInstr);
            }
            catch {
                popcntInstr = false;
            }

            if (!popcntInstr) {
                //Utilities.MessageBoxWarning(
                throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED,
                    "Your processor micro-architecture is not supported by Starcounter." + Environment.NewLine +
                    "For Intel processors, Starcounter requires a Nehalem micro-architecture or later." + Environment.NewLine +
                    "For AMD processors, Starcounter requires a Barcelona micro-architecture or later." + Environment.NewLine +
                    "Please refer to the system requirements for more information.");
            }
        }

        /// <summary>
        /// Gets a value indicating if the current client process is run with
        /// administrative rights.
        /// </summary>
        public static bool RunsAsAdministrator {
            get {
                AssurePrivilegeRightsLookup();
                return runsAsAdministrator;
            }
        }
        private static bool runsAsAdministrator = false;

        /// <summary>
        /// Gets a value that reveals if the user running the client has
        /// administrative rights on the local computer.
        /// </summary>
        /// <remarks>
        /// Note that this does not mean that the client/process runs with
        /// administrative rights. Under Windows UAC (when enabled) the normal
        /// mode even for users having administrative rights is to run
        /// processes without them. To see if the loaded client/process is
        /// really running with administrative rights, see 
        /// <c>RunsAsAdministrator</c>.
        /// </remarks>
        public static bool? HasAdministrativeRights {
            get {
                AssurePrivilegeRightsLookup();
                return hasAdministrativeRights;
            }
        }
        private static bool? hasAdministrativeRights = null;

        private static void AssurePrivilegeRightsLookup() {
            if (hasAdministrativeRights.HasValue == false) {
                // Try first determining if the currently running user has
                // administrative rights on the local computer. If any check
                // fails, report false.

                try {
                    WindowsIdentity cur = WindowsIdentity.GetCurrent();

                    foreach (IdentityReference role in cur.Groups) {
                        if (role.IsValidTargetType(typeof(SecurityIdentifier))) {
                            SecurityIdentifier sid = role as SecurityIdentifier;

                            // Checking if user is in either of Administrator groups.
                            if (sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                                sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                hasAdministrativeRights = true;
                                break;
                            }
                        }
                    }
                }
                catch {
                    hasAdministrativeRights = false;
                }

                // Based on the fact if the user has administrative rights or
                // not, try determining if the current client/process actually
                // runs with such rights.

                try {
                    runsAsAdministrator =
                        hasAdministrativeRights.Value
                        ? (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator)
                        : false;
                }
                catch {
                    runsAsAdministrator = false;
                }
            }
        }

        /// <summary>
        /// Creates shortcut using external utility.
        /// </summary>
        public static void CreateShortcut(
            String pathToOrigin,
            String pathToLnk,
            String commandArgs,
            String workingDir,
            String description,
            String iconPath) {
            /*
            // Creating shortcut using our utility.
            ProcessStartInfo shortcutInfo = new ProcessStartInfo();
            shortcutInfo.FileName = "\"" + InstallerMain.InstallationDir + "\\CreateShortcut\"";
            shortcutInfo.Arguments = "\"" + pathToOrigin + "\" \"" + pathToLnk + "\" \"" + args + "\" \"" + workingDir + "\" \"" + description + "\" \"" + iconPath + "\"";
            shortcutInfo.UseShellExecute = false;
            shortcutInfo.CreateNoWindow = true;

            // Start the build process.
            Process shortcutProcess = Process.Start(shortcutInfo);

            // Waiting some seconds for build process to finish.
            shortcutProcess.WaitForExit(60000);

            // Checking if build process has finished.
            if ((!shortcutProcess.HasExited) || (shortcutProcess.ExitCode != 0))
            {
                Utilities.MessageBoxInfo("Can't create shortcut to: " + pathToOrigin + ". Error code: " + shortcutProcess.ExitCode + ".",
                    "Problems with shortcut creation...");
            }

            // Closing process instance.
            shortcutProcess.Close();
            */

            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(pathToLnk);
            shortcut.Description = description;
            shortcut.TargetPath = pathToOrigin;
            shortcut.WorkingDirectory = workingDir;
            shortcut.IconLocation = iconPath;
            shortcut.Arguments = commandArgs;
            shortcut.Save();
        }

        /// <summary>
        /// Checks if its developer installation.
        /// </summary>
        /// <returns></returns>
        public static Boolean IsDeveloperInstallation() {
            String curDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            // Checking that we don't remove files if setup is running from installation directory.
            if (File.Exists(System.IO.Path.Combine(curDir, StarcounterConstants.ProgramNames.ScCode + ".exe")))
                return true;

            return false;
        }

        /// <summary>
        /// Checks for basic Starcounter setup requirements.
        /// </summary>
        public static void CheckInstallationRequirements() {
            // Checking if we are in developer installation.
            if (IsDeveloperInstallation())
                return;

            // Checking if platform is 64-bit.
            if (!Utilities.Platform64Bit()) {
                throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED,
                    "Starcounter requires 64-bit operating system to be installed and run on.");
            }

            // Checking for processor features.
            Utilities.CheckProcessorRequirements();

            // Checking who is running this setup.
            if (!Utilities.RunsAsAdministrator) {
                throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED,
                    "During installation current user must have administrative rights on the local computer and Starcounter installer must be run with administrative rights.");
            }
        }

        // Currently logged in user name.
        public static String loggedInUserName = null;
        public static String LoggedInUserName {
            get {
                if (loggedInUserName == null) {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT UserName FROM Win32_ComputerSystem");
                    String user = String.Empty;
                    foreach (ManagementObject queryObj in searcher.Get()) {
                        user = Convert.ToString(queryObj["UserName"]);
                    }

                    // Removing the 
                    String[] splitName = user.Split(new Char[] { '\\' });
                    if (splitName.Length == 1) {
                        loggedInUserName = splitName[0];
                        return loggedInUserName;
                    }
                    else if (splitName.Length == 2) {
                        loggedInUserName = splitName[1];
                        return loggedInUserName;
                    }
                    else return null;
                }

                return loggedInUserName;
            }
        }

        // Currently logged in user security identifier.
        public static String loggedInUserSid = null;
        public static String LoggedInUserSid {
            get {
                if (loggedInUserSid == null) {
                    NTAccount ntAcc = new NTAccount(LoggedInUserName);
                    SecurityIdentifier si = (SecurityIdentifier)ntAcc.Translate(typeof(SecurityIdentifier));
                    loggedInUserSid = si.ToString();
                    return loggedInUserSid;
                }

                return loggedInUserSid;
            }
        }

        /// <summary>
        /// Indicates if user was already prompted about killing Starcounter processes.
        /// </summary>
        public static Boolean promptedKillMessage = false;

        /// <summary>
        /// Kills all disturbing processes and waits for them to shutdown.
        /// </summary>
        /// <param name="procNames">Names of processes.</param>
        /// Returns true if processes were killed.
        /// <param name="silentKill"></param>
        public static Boolean KillDisturbingProcesses(String[] procNames, Boolean silentKill) {
            foreach (String procName in procNames) {
                Process[] procs = Process.GetProcessesByName(procName);
                foreach (Process proc in procs) {
                    // Asking for user decision about killing processes.
                    if (!InstallerMain.SilentFlag && !promptedKillMessage && !silentKill) {
                        promptedKillMessage = true;
                        if (!AskUserForDecision("All Starcounter processes will be stopped to continue the setup." + Environment.NewLine +
                            "Are you sure you want to proceed?",
                            "Stopping Starcounter processes...")) {
                            return false;
                        }
                    }

                    try {
                        proc.Kill();
                        proc.WaitForExit();

                    }
                    catch (Exception exc) {

                        String processCantBeKilled = "Process " + proc.ProcessName + " can not be killed:" + Environment.NewLine +
                            exc.ToString() + Environment.NewLine +
                            "Please shutdown the corresponding application explicitly.";

                        if (InstallerMain.SilentFlag) {
                            // Printing a console message.
                            Utilities.ConsoleMessage(processCantBeKilled);
                        }
                        else {
                            MessageBoxInfo(processCantBeKilled, "Process can not be killed...");
                        }

                        return false;

                    }
                    finally {
                        proc.Close();
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Sets normal security attributes for the folder (recursively) and deletes it.
        /// </summary>
        public static void ForceDeleteDirectory(FileSystemInfo fsi) {
            if (fsi == null) return;

            // Trying to set file attributes.
            try { fsi.Attributes = FileAttributes.Normal; }
            catch { }

            // Checking if its a directory and go into recursion if yes.
            var di = fsi as DirectoryInfo;
            if (di != null) {
                // Setting the folder attribute.
                try { fsi.Attributes = FileAttributes.Directory; }
                catch { }

                // Trying to obtain sub-information for the folder.
                FileSystemInfo[] fsis = null;
                try { fsis = di.GetFileSystemInfos(); }
                catch { }

                // Iterating through each sub-folder.
                if (fsis != null) {
                    foreach (var dirInfo in fsis) {
                        // Go into recursion for each sub-directory/file.
                        ForceDeleteDirectory(dirInfo);
                    }
                }
            }

            // Trying to delete the file/directory.
            try { fsi.Delete(); }
            catch { }
        }

        /// <summary>
        /// Sets normal security attributes for the folder (recursively) and deletes
        /// (either matched or NOT matched) entries specified with REGEX strings.
        /// </summary>
        public static void ForceDeleteDirectoryEntryPatterns(FileSystemInfo fsi,
                                                             String[] filePatterns,
                                                             Boolean deleteMatchedOnly) {
            if (fsi == null)
                return;

            // Checking if its a directory and go into recursion if yes.
            var di = fsi as DirectoryInfo;
            if (di != null) {
                // First looking if directory name matches the pattern.
                Boolean matched = false;
                foreach (String pattern in filePatterns) {
                    Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

                    // Getting directory name from path.
                    String fileName = Path.GetFileName(di.FullName);

                    // Comparing with Regex patterns.
                    if (rgx.IsMatch(fileName)) {
                        if (deleteMatchedOnly) {
                            ForceDeleteDirectory(di); // File name matched so deleting it.
                            return;
                        }

                        matched = true;
                        break;
                    }
                }

                // Checking if file didn't match all patterns.
                if (!matched) {
                    if (!deleteMatchedOnly) {
                        ForceDeleteDirectory(di); // File name NOT matched so deleting it.
                        return;
                    }
                }

                // Trying to obtain sub-information for the folder.
                FileSystemInfo[] fsis = null;
                try { fsis = di.GetFileSystemInfos(); }
                catch { }

                // Iterating through each sub-folder.
                if (fsis != null) {
                    foreach (var dirInfo in fsis) {
                        // Go into recursion for each sub-directory/file.
                        ForceDeleteDirectoryEntryPatterns(dirInfo, filePatterns, deleteMatchedOnly);
                    }
                }

                // If the directory name didn't match the pattern we don't delete it.
                return;
            }

            // If we are here that its a file.
            try {
                // Looking if a file matches the pattern.
                Boolean matched = false;
                foreach (String pattern in filePatterns) {
                    Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

                    // Getting file name from path.
                    String fileName = Path.GetFileName(fsi.FullName);

                    // Comparing with Regex patterns.
                    if (rgx.IsMatch(fileName)) {
                        if (deleteMatchedOnly) {
                            fsi.Delete(); // File name matched so deleting it.
                            return;
                        }

                        matched = true;
                        break;
                    }
                }

                // Checking if file didn't match all patterns.
                if (!matched) {
                    if (!deleteMatchedOnly) {
                        fsi.Delete(); // File name NOT matched so deleting it.
                        return;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Adds permissions for the current user on directory.
        /// </summary>
        /// <param name="dirPath"></param>
        public static void AddDirFullPermissionsForCurrentUser(String dirPath) {
            FileSystemRights accessRights;

            // Getting current user account name.
            String accountName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            // Setting full control.
            accessRights = FileSystemRights.FullControl;
            bool modified;
            InheritanceFlags none = new InheritanceFlags();
            none = InheritanceFlags.None;

            // Set on directory itself.
            FileSystemAccessRule accessRule = new FileSystemAccessRule(accountName, accessRights, none, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
            DirectoryInfo dInfo = new DirectoryInfo(dirPath);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.ModifyAccessRule(AccessControlModification.Set, accessRule, out modified);

            // Always allow objects to inherit on a directory.
            InheritanceFlags iFlags = new InheritanceFlags();
            iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

            // Add access rule for the inheritance.
            FileSystemAccessRule accessRule2 = new FileSystemAccessRule(accountName, accessRights, iFlags, PropagationFlags.InheritOnly, AccessControlType.Allow);
            dSecurity.ModifyAccessRule(AccessControlModification.Add, accessRule2, out modified);

            dInfo.SetAccessControl(dSecurity);
        }

        /// <summary>
        /// Recursively adds specified access rights to a directory.
        /// </summary>
        /// <param name="fsi">Current file system information (directory or file)</param>
        /// <param name="accessRule">Security access rule.</param>
        public static void RemoveDirectoryAccessRights(FileSystemInfo fsi,
                                                       FileSystemAccessRule accessRule) {
            // Checking for dead end.
            if (fsi == null) return;

            // Checking if its a directory and go into recursion if yes.
            var dirInfo = fsi as DirectoryInfo;
            if (dirInfo != null) // Its a directory.
            {
                try {
                    // Checking if directory exists.
                    if (!Directory.Exists(fsi.FullName))
                        return;

                    // Getting existing access control rules for this directory.
                    DirectorySecurity security = Directory.GetAccessControl(fsi.FullName);

                    // Adding needed security access rule.
                    security.RemoveAccessRuleAll(accessRule);

                    // Applying security changes.
                    Directory.SetAccessControl(fsi.FullName, security);
                }
                catch { }

                // Trying to obtain sub-information for the directory.
                FileSystemInfo[] fsis = dirInfo.GetFileSystemInfos();

                // Iterating through each sub-folder.
                if (fsis != null) {
                    foreach (var subDirInfo in fsis) {
                        // Go into recursion for each sub-directory/file.
                        RemoveDirectoryAccessRights(subDirInfo, accessRule);
                    }
                }
            }
            else // Its a file.
            {
                try {
                    // Checking if file exists.
                    if (!File.Exists(fsi.FullName)) return;

                    // Getting existing access control rules for this file.
                    FileSecurity security = File.GetAccessControl(fsi.FullName);

                    // Adding needed security access rule.
                    security.RemoveAccessRuleAll(accessRule);

                    // Applying security changes.
                    File.SetAccessControl(fsi.FullName, security);
                }
                catch { }
            }
        }

        /// <summary>
        /// Reports an installation event in terms
        /// of logging and user notification.
        /// </summary>
        /// <param name="msg">Message to report.</param>
        public static void ReportSetupEvent(String msg) {
            LogMessage(msg);
            ShowFeedback(msg);
        }

        /// <summary>
        /// Path to the directory where setup log file is stored.
        /// </summary>
        static String SetupLogDirectory = StarcounterEnvironment.Directories.SystemAppDataDirectory;

        /// <summary>
        /// Logs specified message to the log file.
        /// </summary>
        /// <param name="msg">Message to log.</param>
        public static void LogMessage(String msg) {
            if (InstallerMain.SilentFlag) {
                // Printing message to console.
                Console.WriteLine(msg);
            }

            // Checking if directory exist.
            if (!Directory.Exists(SetupLogDirectory))
                Directory.CreateDirectory(SetupLogDirectory);

            // Appending text to log file.
            File.AppendAllText(Path.Combine(SetupLogDirectory, ConstantsBank.ScLogFileName),
                DateTime.Now.ToString() + ", " + InstallerMain.ProgressPercent + "%: " + msg + Environment.NewLine + Environment.NewLine);
        }

        /// <summary>
        /// Prints specified message to console.
        /// </summary>
        /// <param name="msg">Message to print.</param>
        public static void ConsoleMessage(String msg) {
            Console.Error.WriteLine(msg);
        }

        /// <summary>
        /// Calls installer GUI feedback function.
        /// </summary>
        /// <param name="msg">Message to show in feedback.</param>
        public static void ShowFeedback(String msg) {
            if (InstallerMain.GuiProgressCallback != null) {
                InstallerMain.GuiProgressCallback(null,
                    new InstallerProgressEventArgs(msg, InstallerMain.ProgressPercent));
            }
        }

        /// <summary>
        /// Stops current execution to ask a user to make a decision about a question.
        /// </summary>
        /// <param name="question">Question string.</param>
        /// <param name="title">Message box title.</param>
        /// <returns>'True' if user agreed, 'False' otherwise.</returns>
        public static Boolean AskUserForDecision(String question, String title) {
            if (InstallerMain.GuiMessageboxCallback != null) {
                // Calling installer GUI message box.
                MessageBoxEventArgs messageBoxEventArgs = new MessageBoxEventArgs(
                    question,
                    title,
                    WpfMessageBoxButton.YesNo,
                    WpfMessageBoxImage.Exclamation,
                    WpfMessageBoxResult.No);

                InstallerMain.GuiMessageboxCallback(null, messageBoxEventArgs);

                // Checking user's choice.
                WpfMessageBoxResult userChoice = messageBoxEventArgs.MessageBoxResult;
                if (userChoice != WpfMessageBoxResult.Yes)
                    return false;

                return true;
            }
            else {
                // Calling standard message box.
                DialogResult userChoice = MessageBox.Show(
                    question,
                    title,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (userChoice != DialogResult.Yes)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Message box warning alternative.
        /// </summary>
        public static void MessageBoxWarning(String message, String title) {
            if (InstallerMain.GuiMessageboxCallback != null) {
                // Calling installer GUI message box.
                InstallerMain.GuiMessageboxCallback(null,
                    new MessageBoxEventArgs(message, title, WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning));
            }
            else {
                // Calling standard message box.
                MessageBox.Show(message,
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            LogMessage(message);
        }

        /// <summary>
        /// Message box information alternative.
        /// </summary>
        public static void MessageBoxInfo(String message, String title, Boolean forceDefaultMsgBox = false) {
            if ((!forceDefaultMsgBox) && (InstallerMain.GuiMessageboxCallback != null)) {
                // Calling installer GUI message box.
                InstallerMain.GuiMessageboxCallback(null,
                    new MessageBoxEventArgs(message, title, WpfMessageBoxButton.OK, WpfMessageBoxImage.Information));
            }
            else {
                // Calling standard message box.
                MessageBox.Show(message,
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            LogMessage(message);
        }

        /// <summary>
        /// Message box error alternative.
        /// </summary>
        public static void MessageBoxError(String message, String title) {
            if (InstallerMain.GuiMessageboxCallback != null) {
                // Calling installer GUI message box.
                InstallerMain.GuiMessageboxCallback(null,
                    new MessageBoxEventArgs(message, title, WpfMessageBoxButton.OK, WpfMessageBoxImage.Error));
            }
            else {
                // Calling standard message box.
                MessageBox.Show(message,
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            LogMessage(message);
        }

        /// <summary>
        /// Used to show execution progress
        /// </summary>
        public class InstallerProgressEventArgs : EventArgs {
            private int _Progress;
            /// <summary>
            /// Gets or sets the progress in percent.
            /// Minimum of 0 and max of 100
            /// </summary>
            /// <value>
            /// The progress.
            /// </value>
            public int Progress {
                get {
                    return this._Progress;
                }
                set {
                    _Progress = value;
                }
            }

            private string _Text;
            /// <summary>
            /// Gets or sets the text.
            /// Note: Please use as short text as possible, less then 20 chars depending of font size etc..
            /// </summary>
            /// <value>
            /// The text.
            /// </value>
            public string Text {
                get {
                    return this._Text;
                }
                set {
                    _Text = value;
                }
            }

            public bool HasError {
                get {
                    return this.Error != null;
                }
            }

            public Exception Error { get; protected set; }

            public InstallerProgressEventArgs() {
                _Progress = 0;
                _Text = "";
            }

            public InstallerProgressEventArgs(string text, int progress) {
                _Text = text;
                _Progress = progress;
            }

            public InstallerProgressEventArgs(Exception e) {
                this.Error = e;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class MessageBoxEventArgs : EventArgs {
            /// <summary>
            /// Gets or sets the text.
            /// Note: Please use as short text as possible, less then 20 chars depending of font size etc..
            /// </summary>
            /// <value>
            /// The text.
            /// </value>
            public string MessageBoxText { get; set; }
            public string Caption { get; set; }
            public WpfMessageBoxButton Button { get; set; }
            public WpfMessageBoxImage Icon { get; set; }
            public WpfMessageBoxResult DefaultResult { get; set; }
            public WpfMessageBoxResult MessageBoxResult { get; set; }

            public MessageBoxEventArgs(string messageBoxText) {
                //this.MessageBoxText = messageBoxText;
                this.Init(messageBoxText, null, null, null, null);
            }

            public MessageBoxEventArgs(string messageBoxText, string caption) {
                //this.MessageBoxText = messageBoxText;
                //this.Caption = caption;
                this.Init(messageBoxText, caption, null, null, null);
            }

            public MessageBoxEventArgs(string messageBoxText, string caption, WpfMessageBoxButton button) {
                //this.MessageBoxText = messageBoxText;
                //this.Caption = caption;
                this.Init(messageBoxText, caption, button, null, null);
            }


            public MessageBoxEventArgs(string messageBoxText, string caption, WpfMessageBoxButton button, WpfMessageBoxImage icon) {
                //this.MessageBoxText = messageBoxText;
                //this.Caption = caption;
                //this.Button = button;
                //this.Icon = icon;
                this.Init(messageBoxText, caption, button, icon, null);

            }

            public MessageBoxEventArgs(string messageBoxText, string caption, WpfMessageBoxButton button, WpfMessageBoxImage icon, WpfMessageBoxResult defaultResult) {
                //this.MessageBoxText = messageBoxText;
                //this.Caption = caption;
                //this.Button = button;
                //this.Icon = icon;
                //this.DefaultResult = defaultResult;
                this.Init(messageBoxText, caption, button, icon, defaultResult);
            }

            private void Init(string messageBoxText, string caption, WpfMessageBoxButton? button, WpfMessageBoxImage? icon, WpfMessageBoxResult? defaultResult) {
                this.MessageBoxText = messageBoxText;

                if (caption == null) {
                    this.Caption = string.Empty;
                }
                else {
                    this.Caption = caption;
                }


                if (button == null) {
                    this.Button = WpfMessageBoxButton.OK;
                }
                else {
                    this.Button = (WpfMessageBoxButton)button;
                }

                if (icon == null) {
                    this.Icon = WpfMessageBoxImage.None;
                }
                else {
                    this.Icon = (WpfMessageBoxImage)icon;
                }

                if (defaultResult == null) {
                    if (this.Button == WpfMessageBoxButton.OK || this.Button == WpfMessageBoxButton.OKCancel) {
                        this.DefaultResult = WpfMessageBoxResult.OK;
                    }
                    else if (this.Button == WpfMessageBoxButton.YesNo || this.Button == WpfMessageBoxButton.YesNoCancel) {
                        this.DefaultResult = WpfMessageBoxResult.Yes;
                    }

                }
                else {
                    this.DefaultResult = (WpfMessageBoxResult)defaultResult;
                }
            }
        }
    }
}