using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using Starcounter.Controls;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using Starcounter.InstallerWPF.DemoSequence;
using Starcounter.InstallerEngine;
using System.IO.Compression;
using Starcounter.Internal;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Linq;
using Starcounter.Advanced.Configuration;
using Starcounter.Server;
using Starcounter.InstallerWPF.Pages;
using System.Globalization;

namespace Starcounter.InstallerWPF {

    /// <summary>
    /// Interaction logic for InitializationWindow.xaml
    /// </summary>
    public partial class InitializationWindow : Window {

        ///////////////////////////////////////////////////
        // BEGIN WARNING!!!
        // Do not modify, even whitespace here!!!
        // Used for direct replacement by installer.
        ///////////////////////////////////////////////////
        const String ScVersion = "2.0.0.0";
        private readonly DateTime ScVersionDate = DateTime.Parse("1900-01-01 01:01:01Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        ///////////////////////////////////////////////////
        // END WARNING!!!
        ///////////////////////////////////////////////////

        const String StarcounterBin = "StarcounterBin";
        const String ScInstallerGUI = "Starcounter-Setup";

        /// <summary>
        /// Returns the directory path where Starcounter is installed,
        /// obtained from environment variables.
        /// </summary>
        String GetInstalledDirFromEnv() {
            // First checking the user-wide installation directory.
            String scInstDir = Environment.GetEnvironmentVariable(StarcounterBin, EnvironmentVariableTarget.User);

            if (scInstDir != null)
                return scInstDir;

            // Then checking the system-wide installation directory.
            scInstDir = Environment.GetEnvironmentVariable(StarcounterBin, EnvironmentVariableTarget.Machine);

            return scInstDir;
        }

        /// <summary>
        /// Get installed version information
        /// </summary>
        /// <param name="version"></param>
        /// <param name="versionDate"></param>
        /// <returns>True if successfull otherwice false</returns>
        bool GetInstalledVersionInfo(out string version, out DateTime versionDate) {

            version = null;
            versionDate = DateTime.MinValue;

            // Reading INSTALLED Starcounter version XML file.
            String installDir = GetInstalledDirFromEnv();

            if (installDir != null) {

                XmlDocument versionXML = new XmlDocument();
                String versionInfoFilePath = System.IO.Path.Combine(installDir, "VersionInfo.xml");
                // Checking that version file exists and loading it.
                try {
                    if (File.Exists(versionInfoFilePath)) {
                        versionXML.Load(versionInfoFilePath);
                        // NOTE: We are getting only first element.
                        version = (versionXML.GetElementsByTagName("Version"))[0].InnerText;
                        versionDate = DateTime.Parse((versionXML.GetElementsByTagName("VersionDate"))[0].InnerText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                        return true;
                    }
                }
                catch {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Stops current execution to ask a user to make a decision about a question.
        /// </summary>
        /// <param name="question">Question string.</param>
        /// <param name="title">Message box title.</param>
        /// <returns>'True' if user agreed, 'False' otherwise.</returns>
        static Boolean AskUserForDecision(String question, String title) {

            WpfMessageBoxResult userChoice = WpfMessageBox.Show(
                question,
                title,
                WpfMessageBoxButton.YesNo,
                WpfMessageBoxImage.Exclamation,
                WpfMessageBoxResult.No);

            // Checking user's choice.
            if (userChoice != WpfMessageBoxResult.Yes)
                return false;

            return true;
        }

        /// <summary>
        /// Kills all disturbing processes and waits for them to shutdown.
        /// </summary>
        /// <param name="procNames">Names of processes.</param>
        /// Returns true if processes were killed.
        static Boolean KillDisturbingProcesses(String[] procNames) {

            Boolean promptedKillMessage = false;

            foreach (String procName in procNames) {

                Process[] procs = Process.GetProcessesByName(procName);

                foreach (Process proc in procs) {

                    // Asking for user decision about killing processes.
                    if (!promptedKillMessage) {

                        promptedKillMessage = true;

                        if (!AskUserForDecision(
                            "All running Starcounter processes will be stopped to continue the setup." + Environment.NewLine +
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

                        WpfMessageBox.Show(processCantBeKilled, "Process can not be killed...");

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
        /// Checks for compatible database image files version for existing installation.
        /// </summary>
        /// <returns></returns>
        static void CheckExistingDatabasesForCompatibility(out List<String> dbListToUnload) {

            // Killing all disturbing processes.
            //if (!KillDisturbingProcesses(InstallerMain.ScProcessesList)) {
            //    Environment.Exit(1);
            //}

            dbListToUnload = new List<String>();

            var configDir = System.IO.Path.Combine(StarcounterEnvironment.InstallationDirectory, StarcounterEnvironment.Directories.InstallationConfiguration);

            if (!Directory.Exists(configDir)) {
                configDir = System.IO.Path.Combine(StarcounterEnvironment.InstallationDirectory);

                if (!Directory.Exists(configDir))
                    throw new Exception("Starcounter installation directory does not exist: " + configDir);
            }

            var configFile = System.IO.Path.Combine(configDir, StarcounterEnvironment.FileNames.InstallationServerConfigReferenceFile);

            if (!File.Exists(configFile))
                throw new Exception("Starcounter server installation configuration file does not exist: " + configFile);

            var xml = XDocument.Load(configFile);
            var query = from c in xml.Root.Descendants(MixedCodeConstants.ServerConfigDirName)
                        select c.Value;

            var serverDir = query.First();
            var serverConfigPath = System.IO.Path.Combine(serverDir, StarcounterEnvironment.ServerNames.PersonalServer + ServerConfiguration.FileExtension);

            if (!File.Exists(serverConfigPath))
                throw new Exception("Starcounter server configuration file does not exist: " + serverConfigPath);

            var serverConfig = ServerConfiguration.Load(serverConfigPath);

            foreach (var databaseConfig in DatabaseConfiguration.LoadAll(serverConfig)) {

                var image = ImageFile.Read(databaseConfig.Runtime.ImageDirectory, databaseConfig.Name);

                // Checking if image files not found.
                if (null == image)
                    continue;

                if (image.Version != ImageFile.GetRuntimeImageVersion()) {
                    dbListToUnload.Add(databaseConfig.Name);
                }
            }
        }

        /// <summary>
        /// Checks if another version of Starcounter is installed.
        /// </summary>
        /// <returns></returns>
        Boolean IsAnotherVersionInstalled() {

            String installedVersion;
            DateTime installedVersionDate;

            bool success = GetInstalledVersionInfo(out installedVersion, out installedVersionDate);

            // If there is an installed version and it's not the same as the current installer
            if (success && installedVersion != ScVersion) {

                WpfMessageBoxResult userChoice = WpfMessageBoxResult.None;

                String upgradeQuestion = string.Format("Do you want to {0} from version {1} to {2} ?", (ScVersionDate > installedVersionDate) ? "upgrade" : "downgrade", installedVersion, ScVersion),
                    headingMessage = "Starcounter Installation";

                // Checking for the existing databases compatibility.
                List<String> dbListToUnload = new List<String>();
                String errorString = null;

                try {
                    CheckExistingDatabasesForCompatibility(out dbListToUnload);
                }
                catch (Exception exc) {
                    errorString = exc.ToString();
                }

                if (null == errorString) {

                    if (dbListToUnload.Count > 0) {

                        String dbListToUnloadText = String.Join(Environment.NewLine, dbListToUnload);

                        upgradeQuestion += Environment.NewLine + Environment.NewLine +
                            "Existing database image files are incompatible with this installation (database(s): " + dbListToUnloadText + "). " +
                            "Please follow the instructions at: " + Environment.NewLine +
                            "https://github.com/Starcounter/Starcounter/wiki/Reloading-database-between-Starcounter-versions " + Environment.NewLine +
                            "to unload/reload databases.";

                    }
                }
                else {

                    // Some error occurred during the check.
                    upgradeQuestion +=
                        "Error occurred during verification of existing database image files versions." + Environment.NewLine +
                        "Please follow the instructions at: " + Environment.NewLine +
                        "https://github.com/Starcounter/Starcounter/wiki/Reloading-database-between-Starcounter-versions " + Environment.NewLine +
                        "to unload/reload databases." + Environment.NewLine +
                        "Error message: " + errorString;
                }

                // IMPORTANT: Since StarcounterBin can potentially be used
                // in this installer we have to delete it for this process.
                Environment.SetEnvironmentVariable(StarcounterBin, null);

                // Asking for user choice about uninstalling.
                userChoice = WpfMessageBox.Show(
                    upgradeQuestion,
                    headingMessage,
                    WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question);

                if (userChoice == WpfMessageBoxResult.Yes) {

                    this.isUpgrade = true;
                    this.unattended = true;
                    this.setupOptions = SetupOptions.Install;

                    // Asking to launch current installed version uninstaller.
                    String installDir = GetInstalledDirFromEnv();

                    String prevSetupExeFile;
                    FindSetupExe(installDir, out prevSetupExeFile);
                    if (prevSetupExeFile == null) {
                        System.Windows.MessageBox.Show(
                            "Failed to find previous setup exe for Starcounter " + installedVersion +
                            " in '" + installDir + "'. Please uninstall previous version of Starcounter manually.");
                        return true;
                    }

                    Process prevSetupProcess = new Process();
                    prevSetupProcess.StartInfo.FileName = prevSetupExeFile;

                    DateTime fixedDate = new DateTime(2016, 6, 3, 0, 0, 0, DateTimeKind.Utc);

                    if (installedVersionDate >= fixedDate) {
                        prevSetupProcess.StartInfo.Arguments = "DontCheckOtherInstances uninstall unattended upgrade";
                    }
                    else {
                        prevSetupProcess.StartInfo.Arguments = "DontCheckOtherInstances";
                    }

                    prevSetupProcess.Start();

                    // Waiting until previous installer finishes its work.
                    prevSetupProcess.WaitForExit();

                    // Checking version once again.
                    success = GetInstalledVersionInfo(out installedVersion, out installedVersionDate);

                    // IMPORTANT: Since PATH env var still contains path to old installation directory
                    // we have to reset it for this process as well, once uninstallation is complete.
                    String pathUserEnvVar = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("PATH", pathUserEnvVar);

                    // If No more old installation - just continue the new one.
                    return success;
                }

                //WpfMessageBox.Show(
                //    "Please uninstall previous(" + installedVersion + ") version of Starcounter before installing this one.",
                //    "Starcounter is already installed...");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Find Starcounter setup exe in a folder
        /// The file must start with "starcounter-" and end with "-setup.exe"
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="file"></param>
        void FindSetupExe(string folder, out string file) {
            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(s => System.IO.Path.GetFileName(s).StartsWith("starcounter-", StringComparison.InvariantCultureIgnoreCase) && s.EndsWith("-setup.exe", StringComparison.InvariantCultureIgnoreCase));
            file = files.FirstOrDefault();
        }

        Dispatcher _dispatcher;
        bool canClose = false;

        /// <summary>
        /// Entry point for installer.
        /// </summary>
        public InitializationWindow() {

            // Setting library-resolving hooks.
            SetLibraryHooks();

            _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            this.Loaded += new RoutedEventHandler(InitializationWindow_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(InitializationWindow_Closing);

            InitializeComponent();
        }

        void InitializationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (canClose == false) {
                e.Cancel = true;
            }
        }

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        /// <summary>
        /// Runs the internal setup.
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void RunInternalSetup(String[] args) {
            InstallerMain.StarcounterSetup(args, null, null, null, null);
        }

        private void StartAnimation() {
            Storyboard Element_Storyboard = this.PART_Canvas.FindResource("canvasAnimation") as Storyboard;
            Element_Storyboard.Begin(this.PART_Canvas, true);
        }

        private void StopAnimation() {
            Storyboard Element_Storyboard = this.PART_Canvas.FindResource("canvasAnimation") as Storyboard;
            Element_Storyboard.Stop(this.PART_Canvas);
        }

        void InitializationWindow_Loaded(object sender, RoutedEventArgs e) {

            // TODO: Fix automatic loading of styles!

            var colors = new Uri("pack://application:,,,/colors.xaml", UriKind.RelativeOrAbsolute);
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = colors });

            var styles = new Uri("pack://application:,,,/styles.xaml", UriKind.RelativeOrAbsolute);
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = styles });

            // Try extracting static installer dependencies (only parent process does this).
            ExtractInstallerDependencies();

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                           new Action(delegate {
                               // Checking if another Starcounter version is installed.
                               // NOTE: Environment.Exit is used on purpose here, not just "return";
                               if (IsAnotherVersionInstalled())
                                   Environment.Exit(0);

                               this.Visibility = Visibility.Hidden;
                               ThreadPool.QueueUserWorkItem(this.InitInstallerWrapper);
                           }
            ));

        }

        /// <summary>
        /// Wraps the installer initialization function.
        /// </summary>
        private void InitInstallerWrapper(object state) {

            // NOTE: DONT MODIFY IF U DONT KNOW WHAT ARE U DOING!
            // (because of GAC and DLL loading, etc).

            try {

                // Starting animation.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {
                        // Show window
                        this.StartAnimation();
                        this.Visibility = System.Windows.Visibility.Visible;

                        //this.Focus();
                        this.Activate();
                    }));

                // Initializing installer.
                InitInstaller();

                // Checking system requirements (calling using function
                // wrapper so that library is resolved without errors.).
                CheckInstallationRequirements();

                // Stopping animation.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            new Action(delegate {
                                // Show window
                                this.StopAnimation();
                            }));

                // Bringing window on top.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {
                        // Show window
                        this.Visibility = System.Windows.Visibility.Visible;

                        //this.Focus();
                        this.Activate();
                    }));

                // Success.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {
                        this.OnSuccess();
                    }
                ));
            }
            catch (Exception e) {
                // Error / Message
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {
                        this.OnError(e);
                    }
                ));
            }
        }

        private void OnSuccess() {
            bool bWaitWindowGotFocus = false;
            if (this.IsFocused || this.IsKeyboardFocused) {
                bWaitWindowGotFocus = true;
            }

            System.Windows.Forms.Screen screen = this.GetCurrentScreen();

            MainWindow mainWindow = new MainWindow();
            mainWindow.Closed += MainWindow_Closed;
            mainWindow.DefaultSetupOptions = this.setupOptions;
            mainWindow.Configuration.Unattended = this.unattended;
            mainWindow.Configuration.IsUpgrade = this.isUpgrade;
            App.Current.MainWindow = mainWindow;
            this.CloseWindow();

            using (HwndSource source = new HwndSource(new HwndSourceParameters())) {

                Matrix m = source.CompositionTarget.TransformFromDevice;
                Point position = m.Transform(new Point(screen.Bounds.Location.X + (screen.Bounds.Width / 2), screen.Bounds.Location.Y + (screen.Bounds.Height / 2)));
                mainWindow.Left = position.X - (mainWindow.Width / 2);
                mainWindow.Top = position.Y - (mainWindow.Height / 2);
            }
            mainWindow.Topmost = true;  // Hack to get window on top.
            mainWindow.Show();
            mainWindow.Topmost = false; // Hack to get window on top.

            // Move focus from WaitWindow to MainWindow
            if (bWaitWindowGotFocus) {
                //mainWindow.Focus();
                mainWindow.Activate();
            }

        }

        private void MainWindow_Closed(object sender, EventArgs e) {

            MessageBox.Show("MainWindow_Closed");

            MainWindow mainWindow = sender as MainWindow;
            if (mainWindow != null) {
                IFinishedPage finishPage = mainWindow.pages_lb.Items.CurrentItem as IFinishedPage;
                if (finishPage != null && this.isUpgrade && this.unattended) {
                    MessageBox.Show("Starcounter was successfully installed");
                }
            }
        }

        private System.Windows.Forms.Screen GetCurrentScreen() {
            double waitWindowCenter_X = this.ActualWidth / 2;
            double waitWindowCenter_Y = this.ActualHeight / 2;

            Point centerPoint = this.PointToScreen(new Point(waitWindowCenter_X, waitWindowCenter_Y));

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens) {
                if (centerPoint.X >= screen.Bounds.Location.X && centerPoint.X <= (screen.Bounds.Location.X + screen.Bounds.Width)) {
                    if (centerPoint.Y >= screen.Bounds.Location.Y && centerPoint.Y <= (screen.Bounds.Location.Y + screen.Bounds.Height)) {
                        return screen;
                    }
                }
            }
            return System.Windows.Forms.Screen.PrimaryScreen;
        }

        private void CloseWindow() {
            canClose = true;
            Close();
        }

        private void OnError(Exception e) {

            // Send the tracking error before we close down.
            Dispatcher disp = Dispatcher.FromThread(Thread.CurrentThread);

            Starcounter.Tracking.Client.Instance.SendInstallerException(e,
                              delegate (object sender2, Starcounter.Tracking.CompletedEventArgs args) {
                                  // Send compleated (success or error)
                                  disp.BeginInvoke(DispatcherPriority.Normal, new Action(delegate {

                                      // Checking if we are in silent mode.
                                      if (silentMode) {
                                          // Checking if its a quiet exit message.
                                          if ((e.InnerException != null) && (e.InnerException is InstallerException)) {
                                              InstallerException installerException = e.InnerException as InstallerException;
                                              if (installerException.ErrorCode == InstallerErrorCode.QuietExit) {
                                                  this.CloseWindow();
                                                  return;
                                              }
                                          }

                                          // Printing exception message.
                                          Console.Error.WriteLine(e.Message);

                                          // Exiting the process with non-zero exit code.
                                          Environment.Exit(1);
                                      }

                                      // Checking different types of exception.
                                      if ((e.InnerException != null) && (e.InnerException is InstallerException)) {
                                          InstallerException installerException = e.InnerException as InstallerException;

                                          switch (installerException.ErrorCode) {
                                              case InstallerErrorCode.CanNotElevate: {
                                                      WpfMessageBox.Show(e.Message, "Starcounter Installer.");
                                                      this.CloseWindow();
                                                      return;
                                                  }

                                              case InstallerErrorCode.ExistingInstance: {
                                                      WpfMessageBox.Show(e.Message, "Starcounter Installer.");
                                                      this.CloseWindow();
                                                      return;
                                                  }

                                              case InstallerErrorCode.QuietExit: {
                                                      this.CloseWindow();
                                                      return;
                                                  }

                                              case InstallerErrorCode.Unknown: {
                                                      break;
                                                  }
                                          }
                                      }

                                      // TODO: Show exception error window
                                      WpfMessageBox.Show(e.Message, "Starcounter Installer");

                                      // This will abort the installer
                                      this.CloseWindow();



                                  }));
                              }
                        );


        }

        #region Installer Engine

        // Array of files that needs to be statically linked to the
        // installer and not be dependent on dynamic assembly resolving
        // hooks.
        //
        // Remarks: the entire solution with static installer files
        // is due to a Microsoft bug that shows when serializing using
        // the XMLSerializer and assemblies part of the contract is
        // loaded dynamically. MS claimed this is fixed in .NET 4.0 so
        // when we do the switch, we can remove this extra functionality
        // to have less code to maintain.
        //
        // A forum thread exists here:
        // http://www.starcounter.com/forum/showthread.php?1216-Installing-Sc-Failed
        // This thread in turn links to the MS bug thread.
        static String[] StaticInstallerDependencies =
        {
            "Starcounter.InstallerNativeHelper.dll",
            "Starcounter.REST.dll",
            "scerrres.dll",
            "schttpparser.dll",
            "sccoredbh.dll",
            "behemoth.dll",
            "bmx.dll",
            "coalmine.dll",
            "sccoreapp.dll",
            "sccoredb.dll",
            "sccoredbg.dll",
            "sccoredbm.dll",
            "sccorelib.dll",
            "sccorelog.dll",
            "server.dll",
            "sunflower.dll"
        };

        // PID of the parent process.
        Int32 parentPID = -1;

        // Indicates if setup started in silent mode.
        Boolean silentMode = false;
        // unattended setup is not the same as silent, silent should not show any qui.
        Boolean unattended = false;
        SetupOptions setupOptions = SetupOptions.None;

        // Keep settings when uninstalling (set to true when updating starcounter)
        Boolean isUpgrade = false;

        internal static String ScEnvVarName = "StarcounterBin";

        // First installer function that needs to be called.
        void InitInstaller() {
            //System.Diagnostics.Debugger.Launch();

            // Attaching the console.
            AttachConsole(-1);

            // Flag stating if direct internal setup should be launched.
            Boolean internalMode = false;

            // Don't check for other setups running.
            Boolean dontCheckOtherInstances = false;
            // Checking command line parameters.
            String[] args = Environment.GetCommandLineArgs();


            // Checking if special parameters are supplied.
            List<String> userArgs = new List<String>();
            for (Int32 i = 1; i < args.Length; i++) {
                String param = args[i];

                if (param.StartsWith(ConstantsBank.SilentArg, StringComparison.InvariantCultureIgnoreCase)) {
                    silentMode = true;
                    userArgs.Add(param);
                }
                else if (param.StartsWith(ConstantsBank.DontCheckOtherInstancesArg, StringComparison.InvariantCultureIgnoreCase)) {
                    dontCheckOtherInstances = true;
                }
                else if (param.Equals("unattended", StringComparison.InvariantCultureIgnoreCase)) {
                    args = args.Where(w => w != args[i]).ToArray(); // This argument can not be passed along to RunInternalSetup(...)
                    i--;
                    this.unattended = true;
                }
                else if (param.Equals("uninstall", StringComparison.InvariantCultureIgnoreCase)) {
                    args = args.Where(w => w != args[i]).ToArray(); // This argument can not be passed along to RunInternalSetup(...)
                    i--;
                    this.setupOptions = SetupOptions.Uninstall;
                }
                else if (param.Equals("install", StringComparison.InvariantCultureIgnoreCase)) {
                    args = args.Where(w => w != args[i]).ToArray(); // This argument can not be passed along to RunInternalSetup(...)
                    i--;
                    this.setupOptions = SetupOptions.Install;
                }
                else if (param.Equals("upgrade", StringComparison.InvariantCultureIgnoreCase)) {
                    args = args.Where(w => w != args[i]).ToArray(); // This argument can not be passed along to RunInternalSetup(...)
                    i--;
                    this.isUpgrade = true;
                }
                else {
                    internalMode = true;
                    userArgs.Add(param);
                }
            }

            // Checking if any setup instances are running.
            if ((!dontCheckOtherInstances) && AnotherSetupRunning()) {
                String errMsg = "Please finish working with the previous instance before running this setup.";

                // Have to throw general exception because of problems resolving Starcounter.Framework library.
                throw new Exception(errMsg, new InstallerException(errMsg, InstallerErrorCode.ExistingInstance));
            }

            // Registering exceptions handling and application exit event.
            System.Windows.Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            String silentMsg = "Silently ending installer process.";

            // Checking if we need to run the internal setup directly.
            if (internalMode) {
                String[] userArgsArray = null;
                if (userArgs.Count > 0)
                    userArgsArray = userArgs.ToArray();

                // Running internal setup.
                RunInternalSetup(userArgsArray);

                // Have to throw general exception because of problems resolving Starcounter.Framework library.
                throw new Exception(silentMsg, new InstallerException(silentMsg, InstallerErrorCode.QuietExit));
            }
        }

        // Tries to remove temporary extracted files.
        static void RemoveTempExtractedFiles() {
            // Checking that we don't remove files if setup is running from installation directory.
            if (!Utilities.IsDeveloperInstallation()) {
                String curDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

                foreach (String tempFileName in StaticInstallerDependencies) {
                    String tempFilePath = System.IO.Path.Combine(curDir, tempFileName);
                    if (File.Exists(tempFilePath)) {
                        try { File.Delete(tempFilePath); }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Sets necessary library hooks.
        /// </summary>
        public void SetLibraryHooks() {
            // Install hook to load dynamic bound dependencies from the embedded archive
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(PackagedLibrariesLoadHook);
        }

        // Wrapper for extracting library static dependencies.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void ExtractInstallerDependencies() {
            ExtractInstallerDependenciesFromZip();
        }

        /// <summary>
        /// Check if another instance of setup is running.
        /// </summary>
        Boolean AnotherSetupRunning() {
            // Trying to find through all processes.
            Process[] processeslist = Process.GetProcesses();
            foreach (Process process in processeslist) {
                if (process.ProcessName.StartsWith("Starcounter", StringComparison.CurrentCultureIgnoreCase) &&
                    process.ProcessName.EndsWith("Setup", StringComparison.CurrentCultureIgnoreCase)) {
                    // Checking process IDs.
                    if (Process.GetCurrentProcess().Id != process.Id) {
                        // Checking if its also not a parent process.
                        if (process.Id != parentPID)
                            return true;
                    }
                }
            }
            return false;
        }

        // Extracts library static dependencies.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void ExtractInstallerDependenciesFromZip() {
            // Checking if archive is fake (developer-compiled installer).
            if (Configuration.ArchiveZipStream.Length < 128)
                return;

            ZipArchive zipArchive = null;
            string targetDirectory;

            targetDirectory = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            try {
                zipArchive = new ZipArchive(Configuration.ArchiveZipStream, ZipArchiveMode.Read);
            }
            catch {
                // We try to open a reader on the archive data, but if we fail,
                // we silently return, assuming we install in a development-ish
                // environment, where the embedded archive is just a placeholder.

                return;
            }

            // Extracting first-level dependencies from archive.
            using (zipArchive) {
                foreach (var dependentBinary in StaticInstallerDependencies) {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries) {
                        // Checking if file name is the same.
                        if (0 == String.Compare(entry.Name, dependentBinary, true)) {
                            String pathToExtractedFile = System.IO.Path.Combine(targetDirectory, entry.FullName);

                            try {
                                // Deleting old file if any.
                                if (File.Exists(pathToExtractedFile))
                                    File.Delete(pathToExtractedFile);

                                // Extracting the file.
                                entry.ExtractToFile(pathToExtractedFile, true);

                                // Hiding the extracted file.
                                File.SetAttributes(pathToExtractedFile, FileAttributes.Hidden);

                                break;
                            }
                            catch {
                                // Just ignoring failures.
                            }
                        }
                    }
                }
            }
        }

        // Wrapper for checking installation requirements.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void CheckInstallationRequirements() {
            // Disabling assemblies pre-loading since installer has custom resolver.
            HelperFunctions.DisableAssembliesPreLoading();

            // Sending installer start statistics.
            Starcounter.Tracking.Client.Instance.SendInstallerStart();

            // Checking if system requirements are fine.
            Utilities.CheckInstallationRequirements();
        }

        // Callback that is used to help resolving archived libraries.
        public Assembly PackagedLibrariesLoadHook(Object sender, ResolveEventArgs args) {
            // Name of the library that should be resolved.
            AssemblyName asmName = new AssemblyName(args.Name);
            //MessageBox.Show(asmName.Name);

            if (asmName.Name.EndsWith(".resources")) {
                // For some reason .Net 4.0 calls AssemblyResolve for resources as well.
                // As a workaround we ignore these calls.
                // https://connect.microsoft.com/VisualStudio/feedback/details/526836/wpf-appdomain-assemblyresolve-being-called-when-it-shouldnt
                return null;
            }

            // Byte array containing assembly.
            Byte[] assemblyData = null;

            // Creating complete assembly name.
            asmName.Name += ".dll";

            // When the name has been resolved, we check if the file is one that
            // we expect to be statically linked; if it is, we don't try to resolve
            // it because it will break something else

            bool shouldBeStaticallyResolved = StaticInstallerDependencies.Any<string>(delegate (string candidate) {
                return candidate.Equals(asmName.Name, StringComparison.InvariantCultureIgnoreCase);
            });
            if (shouldBeStaticallyResolved)
                return null;

            try {
                // Extracting one needed DLL.
                ZipArchive zipArchive = new ZipArchive(Configuration.ArchiveZipStream, ZipArchiveMode.Read);
                using (zipArchive) {
                    // Searching for the needed entry.
                    foreach (ZipArchiveEntry entry in zipArchive.Entries) {
                        // Checking if file name is the same.
                        if (0 == String.Compare(entry.Name, asmName.Name, true)) {
                            using (Stream memStream = entry.Open()) {
                                // Reading from stream into byte array.
                                assemblyData = new Byte[entry.Length];
                                memStream.Read(assemblyData, 0, assemblyData.Length);
                            }
                            break;
                        }
                    }
                }
            }
            catch {
                // Obtaining current installation path with all needed binaries.
                String binariesFolder = Environment.GetEnvironmentVariable(InitializationWindow.ScEnvVarName, EnvironmentVariableTarget.User);
                if (binariesFolder == null) {
                    binariesFolder = Environment.GetEnvironmentVariable(InitializationWindow.ScEnvVarName, EnvironmentVariableTarget.Machine);

                    if (binariesFolder == null)
                        throw new FileNotFoundException("Can't find Starcounter installation path containing binaries. Please re-install Starcounter using the standalone installer.");
                }

                // DLL can't be extracted from the Zip package, eventually trying to load from file in installation directory.
                assemblyData = File.ReadAllBytes(System.IO.Path.Combine(binariesFolder, asmName.Name));
            }

            // Finally loading assembly from byte array.
            if (assemblyData != null)
                return Assembly.Load(assemblyData);

            return null;
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Send,
                                          new Action(delegate {
                                              this.ShowError((Exception)e.ExceptionObject);
                                          }));
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            e.Handled = true;
            this.ShowError(e.Exception);
        }

        private void ShowError(Exception e) {
            if (System.Windows.Application.Current.MainWindow is MainWindow) {
                ((MainWindow)System.Windows.Application.Current.MainWindow).OnError(e);

                // The layout will not be updated after an exception, i don't know why!. this is a workaround for that
                ((MainWindow)System.Windows.Application.Current.MainWindow).UpdateLayout();
            }
            else {
                WpfMessageBox.Show(e.ToString(), "Starcounter Installer", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        // Wrapper for starting post-setup processes.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void StartPostSetupProcesses() {
            // Setting Starcounter variables for current process (so that subsequently
            // started processes can find the installation path).
            Environment.SetEnvironmentVariable(
                ConstantsBank.SCEnvVariableName,
                CInstallationBase.GetEnvVarMachineUser(ConstantsBank.SCEnvVariableName),
                EnvironmentVariableTarget.Process);

            Environment.SetEnvironmentVariable(
                ConstantsBank.SCEnvVariableDefaultServer,
                CInstallationBase.GetEnvVarMachineUser(ConstantsBank.SCEnvVariableDefaultServer),
                EnvironmentVariableTarget.Process);

            Environment.SetEnvironmentVariable(
                ConstantsBank.SCEnvVariableDefaultPersonalPort,
                CInstallationBase.GetEnvVarMachineUser(ConstantsBank.SCEnvVariableDefaultPersonalPort),
                EnvironmentVariableTarget.Process);

            Environment.SetEnvironmentVariable(
                ConstantsBank.SCEnvVariableDefaultSystemPort,
                CInstallationBase.GetEnvVarMachineUser(ConstantsBank.SCEnvVariableDefaultSystemPort),
                EnvironmentVariableTarget.Process);

            // Calling post-setup processes function.
            InstallerMain.StartPostSetupProcesses(true);

            // Get the Mainthread
            // TODO: Re-enable the demo sequence when it exists.
            /*
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    this.StartDemoSequence();
                }));
            }
            */
        }

        private void StartDemoSequence() {
            string startDemoArgumentsFile = ConstantsBank.ScStartDemosTemp;

            if (File.Exists(startDemoArgumentsFile)) {
                string[] lines = File.ReadAllLines(startDemoArgumentsFile);

                // Consume the file.
                FileAttributes attr = File.GetAttributes(startDemoArgumentsFile);
                bool isReadOnly = ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
                if (isReadOnly) {
                    // Remove readonly flag
                    attr ^= FileAttributes.ReadOnly;
                    File.SetAttributes(startDemoArgumentsFile, attr);
                }
                File.Delete(startDemoArgumentsFile);

                if (lines != null && lines.Length > 1) {
                    // Checking if there is no demo starting.
                    if (String.IsNullOrEmpty(lines[1]))
                        return;

                    DemoSequenceWindow demoSequenceWindow = new DemoSequenceWindow();
                    demoSequenceWindow.Start(lines[0], lines[1]);

                }

                //// Start Demo-Sequence program
                //if (lines != null && lines.Length > 0)
                //{
                //    // TODO: Hardcoded path
                //    Process p = new Process();
                //    p.StartInfo = new ProcessStartInfo(@"c:\Users\andwah\Documents\Visual Studio 2010\Projects\Startup-Sequence\Startup-Sequence\bin\Debug\Startup-Sequence.exe");
                //    p.StartInfo.Arguments = lines[0];
                //    p.Start();
                //}
            }

        }

        #endregion
    }

    public enum InstallerErrorCode {
        Unknown = 0,
        ExistingInstance = 1,
        CanNotElevate = 2,
        QuietExit = 3
    }

    public class InstallerException : Exception {
        public InstallerErrorCode ErrorCode { get; protected set; }

        public InstallerException(string message, Exception innerException, InstallerErrorCode errorCode)
            : base(message, innerException) {
            this.ErrorCode = errorCode;
        }

        public InstallerException(string message, InstallerErrorCode errorCode)
            : base(message) {
            this.ErrorCode = errorCode;
        }
    }
}
