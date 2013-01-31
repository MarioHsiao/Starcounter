﻿using System;
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

namespace Starcounter.InstallerWPF
{
    /// <summary>
    /// Interaction logic for InitializationWindow.xaml
    /// </summary>
    public partial class InitializationWindow : Window
    {
        Dispatcher _dispatcher;
        bool canClose = false;
        /// <summary>
        /// Entry point for installer.
        /// </summary>
        public InitializationWindow()
        {
            // Setting library-resolving hooks.
            SetLibraryHooks();

            _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            this.Loaded += new RoutedEventHandler(InitializationWindow_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(InitializationWindow_Closing);
            InitializeComponent();
        }

        void InitializationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (canClose == false)
            {
                e.Cancel = true;
            }
        }

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        /// <summary>
        /// Runs the internal setup.
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void RunInternalSetup(String[] args)
        {
            InstallerMain.StarcounterSetup(args, null, null, null, null);
        }

        private void StartAnimation()
        {
            Storyboard Element_Storyboard = this.PART_Canvas.FindResource("canvasAnimation") as Storyboard;
            Element_Storyboard.Begin(this.PART_Canvas, true);
        }

        private void StopAnimation()
        {
            Storyboard Element_Storyboard = this.PART_Canvas.FindResource("canvasAnimation") as Storyboard;
            Element_Storyboard.Stop(this.PART_Canvas);
        }


        void InitializationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            ThreadPool.QueueUserWorkItem(this.InitInstallerWrapper);
        }

        /// <summary>
        /// Wraps the installer initialization function.
        /// </summary>
        private void InitInstallerWrapper(object state)
        {
            try
            {
#if SIMULATE_INSTALLATION
#else
                this.InitInstaller();
#endif

                // Success.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate
                    {
                        this.OnSuccess();
                    }
                ));
            }
            catch (Exception e)
            {
                // Error / Message
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate
                    {
                        this.OnError(e);
                    }
                ));
            }
        }

        private void OnSuccess()
        {
            bool bWaitWindowGotFocus = false;
            if (this.IsFocused || this.IsKeyboardFocused)
            {
                bWaitWindowGotFocus = true;
            }

            System.Windows.Forms.Screen screen = this.GetCurrentScreen();

            MainWindow mainWindow = new MainWindow();
            App.Current.MainWindow = mainWindow;
            this.CloseWindow();

            mainWindow.Left = (screen.Bounds.Location.X + (screen.Bounds.Width / 2)) - (mainWindow.Width / 2);
            mainWindow.Top = (screen.Bounds.Location.Y + (screen.Bounds.Height / 2)) - (mainWindow.Height / 2);
            mainWindow.Show();

            // Move focus from WaitWindow to MainWindow
            if (bWaitWindowGotFocus)
            {
                //mainWindow.Focus();
                mainWindow.Activate();
            }
        }

        private System.Windows.Forms.Screen GetCurrentScreen()
        {
            double waitWindowCenter_X = this.ActualWidth / 2;
            double waitWindowCenter_Y = this.ActualHeight / 2;

            Point centerPoint = this.PointToScreen(new Point(waitWindowCenter_X, waitWindowCenter_Y));

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (centerPoint.X >= screen.Bounds.Location.X && centerPoint.X <= (screen.Bounds.Location.X + screen.Bounds.Width))
                {
                    if (centerPoint.Y >= screen.Bounds.Location.Y && centerPoint.Y <= (screen.Bounds.Location.Y + screen.Bounds.Height))
                    {
                        return screen;
                    }
                }
            }
            return System.Windows.Forms.Screen.PrimaryScreen;
        }

        private void CloseWindow()
        {
            canClose = true;
            Close();
        }

        private void OnError(Exception e)
        {
            // Checking if we are in silent mode.
            if (silentMode)
            {
                // Checking if its a quiet exit message.
                if ((e.InnerException != null) && (e.InnerException is InstallerException))
                {
                    InstallerException installerException = e.InnerException as InstallerException;
                    if (installerException.ErrorCode == InstallerErrorCode.QuietExit)
                    {
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
            if ((e.InnerException != null) && (e.InnerException is InstallerException))
            {
                InstallerException installerException = e.InnerException as InstallerException;

                switch (installerException.ErrorCode)
                {
                    case InstallerErrorCode.CanNotElevate:
                    {
                        WpfMessageBox.Show(e.Message, "Starcounter Installer.");
                        this.CloseWindow();
                        return;
                    }

                    case InstallerErrorCode.ExistingInstance:
                    {
                        WpfMessageBox.Show(e.Message, "Starcounter Installer.");
                        this.CloseWindow();
                        return;
                    }

                    case InstallerErrorCode.QuietExit:
                    {
                        this.CloseWindow();
                        return;
                    }

                    case InstallerErrorCode.Unknown:
                    {
                        break;
                    }
                }
            }

            // TODO: Show exception error window
            WpfMessageBox.Show(e.Message, "Starcounter Installer");

            // This will abort the installer
            this.CloseWindow();
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
        static String[] staticInstallerDependencies =
        { 
            //"Starcounter.Internal.dll"
        };

        static String[] TempExtractedFiles =
        {
            //"Starcounter.Internal.dll"
        };

        // Tries to remove temporary extracted files.
        static void RemoveTempExtractedFiles()
        {
            String current_directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Checking that we don't remove files if setup is running from installation directory.
            if (!File.Exists(System.IO.Path.Combine(current_directory, StarcounterConstants.ProgramNames.ScCode + ".exe")))
            {
                foreach (String temp_file_name in TempExtractedFiles)
                {
                    String temp_file_path = System.IO.Path.Combine(current_directory, temp_file_name);
                    if (File.Exists(temp_file_path))
                    {
                        try { File.Delete(temp_file_path); }
                        catch { }
                    }
                }
            }
        }

        // Indicates if this installer instance is started by parent instance.
        Boolean startedByParent = false;

        // PID of the parent process.
        Int32 parentPID = -1;

        // Indicates if setup started in silent mode.
        Boolean silentMode = false;

        internal static String ScEnvVarName = "StarcounterBin";
        // First installer function that needs to be called.
        void InitInstaller()
        {
            // Attaching the console.
            AttachConsole(-1);

            const String ParentArg = "YouHaveParent";
            const String SilentArg = "Silent";

            // Flag stating if direct internal setup should be launched.
            Boolean internalMode = false;

            // Checking command line parameters.
            String[] args = Environment.GetCommandLineArgs();

            // Checking if special parameters are supplied.
            foreach (String param in args)
            {
                if (param.EndsWith(SilentArg, StringComparison.InvariantCultureIgnoreCase))
                    silentMode = true;
            }

            // Checking if we are started from parent process.
            if ((args.Length > 2) && (args[2] == ParentArg))
            {
                //System.Diagnostics.Debugger.Break();

                parentPID = Int32.Parse(args[1]);
                startedByParent = true;

                // Checking if internal setup should be launched.
                if (args.Length > 3)
                    internalMode = true;
            }

            // Checking if any setup instances are running.
            if (AnotherSetupRunning())
            {
                String errMsg = "Please finish working with the previous instance before running this setup.";

                // Have to throw general exception because of problems resolving Starcounter.Framework library.
                throw new Exception(errMsg, new InstallerException(errMsg, InstallerErrorCode.ExistingInstance));
            }

            // Try extracting static installer dependencies (only parent process does this).
            // TODO: Check if needed at all.
            if (!startedByParent)
                ExtractInstallerDependencies();

            // Registering exceptions handling and application exit event.
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            String silentMsg = "Silently ending installer process.";

            // Checking if parent process started us.
            if (startedByParent)
            {
                // Checking if we need to run the internal setup directly.
                if (internalMode)
                {
                    // Chopping arguments.
                    String[] subArgs = null;
                    Int32 subArgsNum = args.Length - 3;
                    if (subArgsNum > 0)
                    {
                        subArgs = new String[subArgsNum];
                        for (Int32 i = 3; i < args.Length; i++)
                            subArgs[i - 3] = args[i];
                    }

                    // Running internal setup.
                    RunInternalSetup(subArgs);

                    // Have to throw general exception because of problems resolving Starcounter.Framework library.
                    throw new Exception(silentMsg, new InstallerException(silentMsg, InstallerErrorCode.QuietExit));
                }

                // Starting animation.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate
                    {
                        // Show window
                        this.StartAnimation();
                        this.Visibility = System.Windows.Visibility.Visible;

                        //this.Focus();
                        this.Activate();

                    }
                ));

                // Checking system requirements (calling using function
                // wrapper so that library is resolved without errors.).
                CheckInstallationRequirements();

                // Stopping animation.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                          new Action(delegate
                          {
                              // Show window
                              this.StopAnimation();
                          }
                      ));
            }
            else
            {
                //System.Diagnostics.Debugger.Break();

                // Checking if we need to pass some arguments to child process.
                String argsString = ParentArg;
                if (args.Length > 1)
                {
                    for (Int32 i = 1; i < args.Length; i++)
                        argsString += " " + args[i];
                }

                // Adding temp files cleanup event on parent installer process exit.
                AppDomain.CurrentDomain.ProcessExit += (s, e) => RemoveTempExtractedFiles();

                // Starting child setup instance.
                this.StartingTheElevatedInstaller(argsString);

                // Have to throw general exception because of problems resolving Starcounter.Framework library.
                throw new Exception(silentMsg, new InstallerException(silentMsg, InstallerErrorCode.QuietExit));
            }
        }

        /// <summary>
        /// Sets necessary library hooks.
        /// </summary>
        public void SetLibraryHooks()
        {
            // Install hook to load dynamic bound dependencies from the embedded archive
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(PackagedLibrariesLoadHook);
        }

        /// <summary>
        /// Starts child elevated installer instance and waits for its finish.
        /// </summary>
        private void StartingTheElevatedInstaller(string argsString)
        {
            // Starting the elevated installer.
            Process scSetup = new Process();
            scSetup.StartInfo.FileName = Assembly.GetExecutingAssembly().Location;
            scSetup.StartInfo.UseShellExecute = true;
            scSetup.StartInfo.Verb = "runas";

            // Specifying what is the parent setup process ID.
            scSetup.StartInfo.Arguments = Process.GetCurrentProcess().Id.ToString() + " " + argsString;

            // Exit code of the child instance.
            Int32 exitCode = 1;
            try
            {
                // Starting elevated installer.
                scSetup.Start();

                // Waiting until installer is finished.
                scSetup.WaitForExit();

                // Starting post-setup processes (e.g. Administrator).
                StartPostSetupProcesses();

                // Getting exit code of child setup instance.
                exitCode = scSetup.ExitCode;
                scSetup.Close();
            }
            catch
            {
                // This can occur when user answers 'No' on elevating setup process.
                // In this case we silently exiting the instance.
                String errMsg = "Problems running child setup instance (e.g. elevation canceled).";

                throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED,
                    new InstallerException(errMsg, InstallerErrorCode.CanNotElevate), errMsg);
            }

            // Checking for the child error code explicitly.
            if (exitCode != 0)
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED,
                    "Setup instance terminated with error.");
            }
        }

        // Wrapper for extracting library static dependencies.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void ExtractInstallerDependencies()
        {
            ExtractInstallerDependenciesFromZip();
        }

        /// <summary>
        /// Check if another instance of setup is running.
        /// </summary>
        Boolean AnotherSetupRunning()
        {
            // Trying to find through all processes.
            Process[] processeslist = Process.GetProcesses();
            foreach (Process process in processeslist)
            {
                if (process.ProcessName.StartsWith("Starcounter", StringComparison.CurrentCultureIgnoreCase) &&
                    process.ProcessName.EndsWith("Setup", StringComparison.CurrentCultureIgnoreCase))
                {
                    // Checking process IDs.
                    if (Process.GetCurrentProcess().Id != process.Id)
                    {
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
        void ExtractInstallerDependenciesFromZip()
        {
            // Checking if archive is fake (developer-compiled installer).
            if (Configuration.ArchiveZipStream.Length < 128)
                return;

            ZipArchive zipArchive = null;
            string targetDirectory;

            targetDirectory = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            try
            {
                zipArchive = new ZipArchive(Configuration.ArchiveZipStream, ZipArchiveMode.Read);
            }
            catch
            {
                // We try to open a reader on the archive data, but if we fail,
                // we silently return, assuming we install in a development-ish
                // environment, where the embedded archive is just a placeholder.

                return;
            }

            // Extracting first-level dependencies from archive.
            using (zipArchive)
            {
                foreach (var dependentBinary in staticInstallerDependencies)
                {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        // Checking if file name is the same.
                        if (0 == String.Compare(entry.Name, dependentBinary, true))
                        {
                            entry.ExtractToFile(System.IO.Path.Combine(targetDirectory, entry.FullName), true);
                            break;
                        }
                    }
                }
            }
        }

        // Wrapper for checking installation requirements.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void CheckInstallationRequirements()
        {
            Utilities.CheckInstallationRequirements();
        }

        // Callback that is used to help resolving archived libraries.
        public Assembly PackagedLibrariesLoadHook(Object sender, ResolveEventArgs args)
        {
            // Name of the library that should be resolved.
            AssemblyName asmName = new AssemblyName(args.Name);
            //MessageBox.Show(asmName.Name);

            if (asmName.Name.EndsWith(".resources"))
            {
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

            bool shouldBeStaticallyResolved = staticInstallerDependencies.Any<string>(delegate(string candidate)
            {
                return candidate.Equals(asmName.Name, StringComparison.InvariantCultureIgnoreCase);
            });
            if (shouldBeStaticallyResolved)
                return null;

            try
            {
                // Extracting one needed DLL.
                ZipArchive zipArchive = new ZipArchive(Configuration.ArchiveZipStream, ZipArchiveMode.Read);
                using (zipArchive)
                {
                    // Searching for the needed entry.
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        // Checking if file name is the same.
                        if (0 == String.Compare(entry.Name, asmName.Name, true))
                        {
                            using (Stream memStream = entry.Open())
                            {
                                // Reading from stream into byte array.
                                assemblyData = new Byte[entry.Length];
                                memStream.Read(assemblyData, 0, assemblyData.Length);
                            }
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Obtaining current installation path with all needed binaries.
                String binariesFolder = Environment.GetEnvironmentVariable(InitializationWindow.ScEnvVarName, EnvironmentVariableTarget.User);
                if (binariesFolder == null)
                {
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

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Send,
                                          new Action(delegate
                                          {
                                              this.ShowError((Exception)e.ExceptionObject);
                                          }));
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            this.ShowError(e.Exception);
        }

        private void ShowError(Exception e)
        {
            if (Application.Current.MainWindow is MainWindow)
            {
                ((MainWindow)Application.Current.MainWindow).OnError(e);

                // The layout will not be updated after an exception, i don't know why!. this is a workaround for that
                ((MainWindow)Application.Current.MainWindow).UpdateLayout();
            }
            else
            {
                MessageBox.Show(e.ToString(), "Starcounter Installer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Wrapper for starting post-setup processes.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void StartPostSetupProcesses()
        {
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

        private void StartDemoSequence()
        {
            string startDemoArgumentsFile = ConstantsBank.ScStartDemosTemp;

            if (File.Exists(startDemoArgumentsFile))
            {
                string[] lines = File.ReadAllLines(startDemoArgumentsFile);

                // Consume the file.
                FileAttributes attr = File.GetAttributes(startDemoArgumentsFile);
                bool isReadOnly = ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
                if (isReadOnly)
                {
                    // Remove readonly flag
                    attr ^= FileAttributes.ReadOnly;
                    File.SetAttributes(startDemoArgumentsFile, attr);
                }
                File.Delete(startDemoArgumentsFile);

                if (lines != null && lines.Length > 1)
                {
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

    public enum InstallerErrorCode
    {
        Unknown = 0,
        ExistingInstance = 1,
        CanNotElevate = 2,
        QuietExit = 3
    }

    public class InstallerException : Exception
    {
        public InstallerErrorCode ErrorCode { get; protected set; }

        public InstallerException(string message, Exception innerException, InstallerErrorCode errorCode)
            : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }

        public InstallerException(string message, InstallerErrorCode errorCode)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }
    }
}
