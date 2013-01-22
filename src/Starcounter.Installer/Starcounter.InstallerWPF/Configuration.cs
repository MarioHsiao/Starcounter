//#define SIMULATE_INSTALLATION // REMOVE!
using System;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using Starcounter.InstallerWPF.Properties;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections;
using Starcounter.InstallerWPF.Pages;
using Starcounter.InstallerWPF.Components;
using System.Threading;
using Starcounter.Controls;
using Starcounter.InstallerEngine;
using Starcounter.Internal;
using System.IO.Compression;

namespace Starcounter.InstallerWPF
{
    public class Configuration : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Identifies common path for installation: product name + version.
        /// </summary>
        public static string StarcounterCommonPath;

        private Hashtable _Components = new Hashtable();
        public Hashtable Components
        {
            get { return this._Components; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can execute.
        /// </summary>
        /// <value>
        /// <c>true</c> if there is comething to execute; otherwise, <c>false</c>.
        /// </value>
        public bool CanExecute
        {
            get
            {
                bool bSomethingToExecute = false;
                IDictionaryEnumerator _enumerator = this.Components.GetEnumerator();

                while (_enumerator.MoveNext())
                {
                    BaseComponent component = _enumerator.Value as BaseComponent;

                    if (component != null && component.ExecuteCommand && component.IsExecuteCommandEnabled)
                    {
                        bSomethingToExecute = true;
                        break;
                    }
                }

                return bSomethingToExecute;
            }

        }
        #endregion


        public Configuration()
        {
            StarcounterCommonPath = System.IO.Path.Combine(ConstantsBank.SCProductName,
                InstallerMain.GetEmbVersionInfo().Version.ToString());
        }

        /// <summary>
        /// Sets the default values.
        /// </summary>
        /// <param name="setupOptions">The setup options.</param>
        public void SetDefaultValues(SetupOptions setupOptions)
        {

            Samples samples = this.GetComponent(Samples.Identifier) as Samples;

            switch (setupOptions)
            {
                case SetupOptions.Ask:
                    break;

                case SetupOptions.None:
                    break;

                case SetupOptions.Install:

                    if (samples != null)
                    {
                        samples.SetCanBeInstalled(true);
                        samples.SetCanBeUnInstalled(true);
                    }

                    break;

                case SetupOptions.AddComponents:

                    if (samples != null)
                    {
                        samples.SetCanBeInstalled(true);
                    }

                    break;

                case SetupOptions.RemoveComponents:

                    if (samples != null)
                    {
                        samples.SetCanBeUnInstalled(false);
                    }

                    break;

                case SetupOptions.Uninstall:

                    if (samples != null)
                    {
                        samples.SetCanBeInstalled(true);
                        samples.SetCanBeUnInstalled(true);
                    }

                    break;

            }

        }

        /// <summary>
        /// Generates the INI file.
        /// </summary>
        private void GenerateIniFile()
        {
            string iniFilename = @"C:\test.ini";   // TODO: Path!

            if (File.Exists(iniFilename))
            {
                File.Delete(iniFilename);
            }

            var iniFileHandler = new IniFileHandler(iniFilename);


            // Loop through all items of a Hashtable
            IDictionaryEnumerator item = this.Components.GetEnumerator();
            while (item.MoveNext())
            {
                BaseComponent component = item.Value as BaseComponent;

                if (component.Command == ComponentCommand.None || component.Command == ComponentCommand.Update)
                {
                    // TODO: We do not support "Updates" yet.
                    continue;
                }

                if (component.ExecuteCommand == false)
                {
                    // Ignore commands that is not to be "Executed"
                    continue;
                }

                iniFileHandler.IniWriteValue(component.ComponentIdentifier, component.Command.ToString(), component.ExecuteCommand.ToString());

                // Get Properties
                IList<DictionaryEntry> properties = component.GetProperties();
                if (properties != null)
                {
                    foreach (DictionaryEntry property in properties)
                    {
                        string valueStr;
                        if (property.Value == null)
                        {
                            valueStr = string.Empty;
                        }
                        else
                        {
                            valueStr = property.Value.ToString();
                        }

                        iniFileHandler.IniWriteValue(component.ComponentIdentifier, property.Key.ToString(), valueStr);
                    }
                }
            }
        }

        /// <summary>
        /// Checking if we are either installing first time or
        /// adding new components to existing installation.
        /// </summary>
        /// <returns>True if yes :)</returns>
        Boolean InstallingOrAddingComponents()
        {
            bool installing = false;

            // Check if we are installing or uninstalling
            IDictionaryEnumerator item = this.Components.GetEnumerator();
            while (item.MoveNext())
            {
                BaseComponent component = item.Value as BaseComponent;

                if (component.Command == ComponentCommand.None || component.Command == ComponentCommand.Update)
                {
                    // TODO: We do not support "Updates" yet.
                    continue;
                }

                if (component.ExecuteCommand == false)
                {
                    // Ignore commands that is not to be "Executed"
                    continue;
                }
                if (component.Command == ComponentCommand.Install)
                {
                    installing = true;
                    break;
                }

                if (component.Command == ComponentCommand.Uninstall)
                {
                    installing = false;
                    break;
                }
            }

            return installing;
        }

        private void GenerateSpecialIniFile()
        {
            // Checking if we are installing or adding components.
            bool installingOrAdding = InstallingOrAddingComponents();

            InstallationBase installationBase = this.GetComponent(InstallationBase.Identifier) as InstallationBase;
            if (installationBase == null || string.IsNullOrEmpty(installationBase.Path))
            {
                MessageBox.Show("Corrupt Installation", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Getting installation path.
            String installationPath = installationBase.Path;
            String iniFilename = Path.Combine(installationPath, ConstantsBank.ScGUISetupIniName);

            // Checking if previous installation GUI settings file exists.
            if (File.Exists(iniFilename))
                File.Delete(iniFilename);

            IniFileHandler iniFileHandler = new IniFileHandler(iniFilename);

            if (installingOrAdding)
            {
                const string installSection = "Starcounter Installation Settings";

                // InstallationBase
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_AddStarcounterToStartMenu, installationBase.AddToStartMenu.ToString());

                // PersonalServer
                PersonalServer personalServer = this.GetComponent(PersonalServer.Identifier) as PersonalServer;
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_InstallPersonalServer, personalServer.ExecuteCommand.ToString());
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_PersonalServerPath, personalServer.Path);

                // SystemServer
                SystemServer systemServer = this.GetComponent(SystemServer.Identifier) as SystemServer;
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_InstallSystemServer, systemServer.ExecuteCommand.ToString());
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_SystemServerPath, systemServer.Path);

                // VisualStudio2010Integration
                VisualStudio2010Integration visualStudio2010Integration = this.GetComponent(VisualStudio2010Integration.Identifier) as VisualStudio2010Integration;
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_InstallVS2010Integration, visualStudio2010Integration.ExecuteCommand.ToString());

                // VisualStudio2012Integration
                VisualStudio2012Integration visualStudio2012Integration = this.GetComponent(VisualStudio2012Integration.Identifier) as VisualStudio2012Integration;
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_InstallVS2012Integration, visualStudio2012Integration.ExecuteCommand.ToString());

                // Samples
                Samples samples = this.GetComponent(Samples.Identifier) as Samples;

                // InstallActivityMonitor
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_InstallActivityMonitor, "False");

                // StarcounterAdministrator
                StarcounterAdministrator starcounterAdministrator = this.GetComponent(StarcounterAdministrator.Identifier) as StarcounterAdministrator;
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_InstallAdministrator, starcounterAdministrator.ExecuteCommand.ToString());
                iniFileHandler.IniWriteValue(installSection, ConstantsBank.Setting_CreateAdministratorShortcuts, starcounterAdministrator.CreateAdministratorShortcuts.ToString());

                // We need to update servers installation paths in SetupSettings.ini in order to be able to fetch this later.
                IniFileHandler origIniFile = new IniFileHandler(Path.Combine(installationPath, ConstantsBank.ScGlobalSettingsIniName));

                if ((personalServer.Command == ComponentCommand.Install) || personalServer.ExecuteCommand)
                {
                    origIniFile.IniWriteValue(installSection, ConstantsBank.Setting_InstallPersonalServer, personalServer.ExecuteCommand.ToString());
                    origIniFile.IniWriteValue(installSection, ConstantsBank.Setting_PersonalServerPath, personalServer.Path);
                }

                if ((systemServer.Command == ComponentCommand.Install) || systemServer.ExecuteCommand)
                {
                    origIniFile.IniWriteValue(installSection, ConstantsBank.Setting_InstallSystemServer, systemServer.ExecuteCommand.ToString());
                    origIniFile.IniWriteValue(installSection, ConstantsBank.Setting_SystemServerPath, systemServer.Path);
                }
            }
            else
            {
                const string uninstallSection = "Starcounter Uninstall Settings";

                // PersonalServer
                PersonalServer personalServer = this.GetComponent(PersonalServer.Identifier) as PersonalServer;
                iniFileHandler.IniWriteValue(uninstallSection, ConstantsBank.Setting_RemovePersonalServer, personalServer.ExecuteCommand.ToString());

                // SystemServer
                SystemServer systemServer = this.GetComponent(SystemServer.Identifier) as SystemServer;
                iniFileHandler.IniWriteValue(uninstallSection, ConstantsBank.Setting_RemoveSystemServer, systemServer.ExecuteCommand.ToString());

                // VisualStudio2010Integration
                VisualStudio2010Integration visualStudio2010Integration = this.GetComponent(VisualStudio2010Integration.Identifier) as VisualStudio2010Integration;
                iniFileHandler.IniWriteValue(uninstallSection, ConstantsBank.Setting_RemoveVS2010Integration, visualStudio2010Integration.ExecuteCommand.ToString());

                // VisualStudio2012Integration
                VisualStudio2012Integration visualStudio2012Integration = this.GetComponent(VisualStudio2012Integration.Identifier) as VisualStudio2012Integration;
                iniFileHandler.IniWriteValue(uninstallSection, ConstantsBank.Setting_RemoveVS2012Integration, visualStudio2012Integration.ExecuteCommand.ToString());

                // StarcounterAdministrator
                StarcounterAdministrator starcounterAdministrator = this.GetComponent(StarcounterAdministrator.Identifier) as StarcounterAdministrator;
                iniFileHandler.IniWriteValue(uninstallSection, ConstantsBank.Setting_RemoveAdministrator, starcounterAdministrator.ExecuteCommand.ToString());
            }
        }

        private BaseComponent GetComponent(string identifier)
        {
            IDictionaryEnumerator item = this.Components.GetEnumerator();
            while (item.MoveNext())
            {
                BaseComponent component = item.Value as BaseComponent;
                if (string.Equals(component.ComponentIdentifier, identifier))
                {
                    return component;
                }

            }
            return null;
        }

        // Data stream of the embedded ZIP archive.
        static Stream archiveZipStream = null;

        // Reads once Zip archive from embedded resources and returns its data stream.
        public static Stream ArchiveZipStream
        {
            get
            {
                // Checking if we have already loaded the archive.
                if (archiveZipStream != null)
                    return archiveZipStream;

                // Loading Zip archive from embedded resources from scratch.
                String resourceName = "Starcounter.InstallerWPF.resources.Archive.zip";
                Stream memStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (memStream == null)
                    throw new FileNotFoundException("Archive.zip package can't be found as an embedded resource.");

                return memStream;
            }
        }

        /// <summary>
        /// Calls installer engine for core installation functionality.
        /// </summary>
        public void RunInstallerEngine(EventHandler<Utilities.InstallerProgressEventArgs> progressCallback,
                                       EventHandler<Utilities.MessageBoxEventArgs> messageboxCallback)
        {
            bool installingOrAdding = InstallingOrAddingComponents();
            String[] args = null;

            // Obtaining installation path.
            InstallationBase installationBase = this.GetComponent(InstallationBase.Identifier) as InstallationBase;
            String installationPath = installationBase.Path;

            // Checking installation directory.
            if (!Directory.Exists(installationPath))
                Directory.CreateDirectory(installationPath);

            // Setting current working directory.
            Environment.CurrentDirectory = installationPath;

            // Checking if we are installing.
            if (installingOrAdding)
            {
                // Checking if its first time installation so we copy binaries.
                if (!installationBase.IsInstalled)
                {
                    // Checking if there are any files in the target installation directory.
                    if (Utilities.DirectoryIsNotEmpty(new DirectoryInfo(installationPath)))
                    {
                        // Setting normal attributes for all files and folders in installation directory.
                        Utilities.SetNormalDirectoryAttributes(new DirectoryInfo(installationPath));

                        // Asking if user wants to delete installation path before proceeding.
                        Utilities.MessageBoxEventArgs messageBoxEventArgs = new Utilities.MessageBoxEventArgs("Installation path '" + installationPath + "' is occupied. Do you want to proceed overwriting existing files?", "Installation path is occupied...",
                            WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Exclamation, WpfMessageBoxResult.No);
                        messageboxCallback(this, messageBoxEventArgs);

                        // Obtaining user's decision.
                        WpfMessageBoxResult result = messageBoxEventArgs.MessageBoxResult;

                        // User confirmed to delete previous installation directory.
                        if (result != WpfMessageBoxResult.Yes)
                            throw ErrorCode.ToException(Error.SCERRINSTALLERABORTED, "User has canceled file copy process.");
                    }

                    try
                    {
                        // Extracting all files to installation directory, and overwriting old files.
                        ZipArchive zipArchive = new ZipArchive(ArchiveZipStream, ZipArchiveMode.Read);
                        using (zipArchive)
                        {
                            zipArchive.ExtractToDirectory(installationPath);
                        }
                    }
                    catch (Exception exc)
                    {
                        // Copying files to installation directory from current directory since there is no embedded archive.
                        String currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        if (File.Exists(Path.Combine(currentDirectory, ConstantsBank.SCInstallerEngine + ".dll")))
                        {
                            // Checking if we are not copying to the current running directory.
                            if (String.Compare(Path.GetFullPath(installationPath).TrimEnd('\\'),
                                               Path.GetFullPath(currentDirectory).TrimEnd('\\'),
                                               StringComparison.InvariantCultureIgnoreCase) != 0)
                            {
                                // We checked that at least one necessary file exists in the current directory.
                                Utilities.CopyFilesRecursively(new DirectoryInfo(currentDirectory), new DirectoryInfo(installationPath));
                            }
                        }
                        else
                        {
                            // No files in current directory, re-throwing the exception.
                            throw exc;
                        }
                    }
                }
            }
            else
            {
                // Creating new uninstall file.
                args = new String[] { "--uninstall" };
            }

            // Creating new installation file.
            //GenerateIniFile();  // TODO: Remove
            GenerateSpecialIniFile();

            // Running core setup function.
            String targetSettingsINIFile = Path.Combine(installationPath, ConstantsBank.ScGUISetupIniName);
            InstallerMain.StarcounterSetup(args, installationPath, targetSettingsINIFile, progressCallback, messageboxCallback);
        }

        /// <summary>
        /// Executes the settings.
        /// Note, This is not done in the Main thread!
        /// </summary>
        public void ExecuteSettings(EventHandler<Utilities.InstallerProgressEventArgs> progressCallback,
                                    EventHandler<Utilities.MessageBoxEventArgs> messageboxCallback)
        {
            Utilities.InstallerProgressEventArgs args = new Utilities.InstallerProgressEventArgs();
            try
            {

                // Simulate installation....
#if SIMULATE_INSTALLATION
                for (int i = 0; i < 100; i++)
                {
                    // Simulate progress
                    args.Progress++;
                    args.Text = "Installing " + i + Environment.NewLine + "item";
                    if (progressCallback != null)
                    {
                        progressCallback(this, args);
                    }

                    // Simulate question
                    if (i == 50)
                    {
                        if (messageboxCallback != null)
                        {

                            string message = "Test link in message box http://www.starcounter.com " + Environment.NewLine + "Do you want to cancel installation?";

                            InstallerEngine.Utilities.MessageBoxEventArgs messageBoxEventArgs = new InstallerEngine.Utilities.MessageBoxEventArgs(message, "Installer", WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question, WpfMessageBoxResult.No);
                            //MessageBoxEventArgs messageBoxEventArgs = new MessageBoxEventArgs("Do you want to cancel?", "Installer", WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question, WpfMessageBoxResult.No);
                            messageboxCallback(this, messageBoxEventArgs);

                            WpfMessageBoxResult result = messageBoxEventArgs.MessageBoxResult;

                            if (result == WpfMessageBoxResult.Yes)
                            {
                                throw new Exception("User canceled");
                            }
                        }
                    }

                    // Simulate question
                    if (i == 80)
                    {
                        if (messageboxCallback != null)
                        {
                            InstallerEngine.Utilities.MessageBoxEventArgs messageBoxEventArgs = new InstallerEngine.Utilities.MessageBoxEventArgs("Some feedback information", "Installer", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
                            messageboxCallback(this, messageBoxEventArgs);
                        }
                    }

                    Thread.Sleep(10);   // TODO: Remove delay simulation
                }
#else
                // Simply running installation engine with provided callbacks.
                RunInstallerEngine(progressCallback, messageboxCallback);

#endif

            }
            catch (Exception e)
            {
                throw e;
            }

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }

        #endregion

    }

    /// <summary>
    /// INI files native helper.
    /// </summary>
    class IniFileHandler
    {
        // WritePrivateProfileStringW (Unicode) and WritePrivateProfileStringA (ANSI)
        private readonly string _iniFile;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public IniFileHandler(string file)
        {
            _iniFile = file;
        }

        public void IniWriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _iniFile);
        }

        public string IniReadValue(string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", temp, 255, _iniFile);
            return temp.ToString();
        }
    }

    /// <summary>
    /// Component action command
    /// </summary>
    public enum ComponentCommand
    {
        /// <summary>
        /// No action is taken
        /// </summary>
        None,
        /// <summary>
        /// Installation command
        /// </summary>
        Install,
        /// <summary>
        /// Uninstallation command
        /// </summary>
        Uninstall,
        /// <summary>
        /// Update command
        /// </summary>
        Update
    }
}
