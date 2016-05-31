using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.Collections;
using Starcounter.InstallerWPF.Rules;
using System.Globalization;
using System.Windows.Controls;
using Starcounter.Internal;
using System.IO;

namespace Starcounter.InstallerWPF.Components {
    public class InstallationBase : BaseComponent {

        public const string Identifier = "StarcounterInstallation";

        public override string ComponentIdentifier {
            get {
                return InstallationBase.Identifier;
            }
        }

        public override string Name {
            get {
                return "Starcounter Installation Path";
            }
        }

        public override bool ShowProperties {
            get {
                return true;
            }
        }

        public override bool IsExecuteCommandEnabled {
            get {
                return false;
            }
        }

        //public bool HasPath
        //{
        //    get
        //    {
        //        return !string.IsNullOrEmpty(this.Path);
        //    }
        //}

        public string Path {
            get {

                if (string.IsNullOrEmpty(this.BasePath)) {
                    return null;
                }

                if (Utilities.IsDeveloperFolder(this.BasePath)) {
                    // Developer folder, do not add product name "Starcounter"
                    return this.BasePath;
                }

                return System.IO.Path.Combine(this.BasePath, ConstantsBank.SCProductName);
            }
        }

        private string _BasePath;
        public string BasePath {

            get {
                return this._BasePath;
            }
            set {

                if (string.Compare(this._BasePath, value) == 0) return;
                this._BasePath = value;

                this.OnPropertyChanged("BasePath");
                this.OnPropertyChanged("Path");
                this.OnPropertyChanged("DirectoryContainsFiles");
            }

        }


        public bool DirectoryContainsFiles {
            get {
                if (this.BasePath != null && this.BasePath is String) {
                    // Add Starcounter
                    string folder = System.IO.Path.Combine(this.BasePath, ConstantsBank.SCProductName);
                    return MainWindow.DirectoryContainsFiles(folder, true);
                }
                return false;
            }
        }


        private bool _SendUsageAndCrashReports;
        public bool SendUsageAndCrashReports {
            get {
                return this._SendUsageAndCrashReports;
            }
            set {
                if (this._SendUsageAndCrashReports == value) return;
                this._SendUsageAndCrashReports = value;
                this.OnPropertyChanged("SendUsageAndCrashReports");
            }
        }


        private bool _AddToStartMenu;
        public bool AddToStartMenu {
            get {
                return this._AddToStartMenu;
            }
            set {
                if (this._AddToStartMenu == value) return;
                this._AddToStartMenu = value;
                this.OnPropertyChanged("AddToStartMenu");
            }
        }

        public InstallationBase(ObservableCollection<BaseComponent> components)
            : base(components) {
            //this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(StarcounterInstallation_PropertyChanged);
        }

        protected override void SetDefaultValues() {
            base.SetDefaultValues();

            // Setting the installation flag.
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.InstallationBase];

            this.SendUsageAndCrashReports = Properties.Settings.Default.SendUsageAndCrashReports;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.InstallationBasePath)) {
                this.BasePath = Properties.Settings.Default.InstallationBasePath;
            }


            // Setting installation path (new path is created if not installed).
#if !SIMULATE_CLEAN_INSTALLATION

            if (string.IsNullOrEmpty(this.BasePath)) {
                string currentInstallationPath = CInstallationBase.GetInstalledDirFromEnv();
                string installationBaseFolder = this.GetInstallationBaseFolder(currentInstallationPath);
                if (string.IsNullOrEmpty(installationBaseFolder)) {
                    // Invalid folder
                    this.BasePath = null;
                }
                else {
                    this.BasePath = installationBaseFolder;
                }
            }

#endif
            if (string.IsNullOrEmpty(this.Path)) {
                String programFilesPath = ConstantsBank.ProgramFilesPath;
                this.BasePath = programFilesPath;
            }

            switch (this.Command) {
                case ComponentCommand.Install:
                    this.ExecuteCommand = !this.IsInstalled;
                    break;
                case ComponentCommand.None:
                    this.ExecuteCommand = false;
                    break;
                case ComponentCommand.Uninstall:
                    this.ExecuteCommand = false;
                    break;
                case ComponentCommand.Update:
                    this.ExecuteCommand = false;
                    break;
            }

            // Setting Start Menu settings accordingly.
            if (!this.IsInstalled) {
                this.AddToStartMenu = true;
            }
            else {
                this.AddToStartMenu = false;
            }
        }

        public override IList<System.Collections.DictionaryEntry> GetProperties() {
            List<DictionaryEntry> properties = new List<DictionaryEntry>();

            properties.Add(new DictionaryEntry("Path", this.Path));
            properties.Add(new DictionaryEntry("AddToStartMenu", this.AddToStartMenu));

            return properties;
        }

        public override bool ValidateSettings() {

            InstallationFolderRule installationFolderRule = new InstallationFolderRule();
            installationFolderRule.CheckEmptyString = true;
            ValidationResult installationFolderRuleResult = installationFolderRule.Validate(this.BasePath, CultureInfo.CurrentCulture);
            if (!installationFolderRuleResult.IsValid) return false;

            //DirectoryContainsFilesRule directoryContainsFilesRule = new DirectoryContainsFilesRule();
            //directoryContainsFilesRule.UseWarning = true;
            //directoryContainsFilesRule.CheckEmptyString = true;
            //directoryContainsFilesRule.AddStarcounter = true;
            //ValidationResult directoryContainsFilesRuleResult = directoryContainsFilesRule.Validate(this.BasePath, CultureInfo.CurrentCulture);
            //if (!directoryContainsFilesRuleResult.IsValid) return false;

            DuplicatPathCheckRule duplicatPathCheckRule = new DuplicatPathCheckRule();
            duplicatPathCheckRule.Type = DuplicatPathCheckRule.SelfType.InstallationPath;
            ValidationResult duplicatPathCheckRuleResult = duplicatPathCheckRule.Validate(this.BasePath, CultureInfo.CurrentCulture);
            if (!duplicatPathCheckRuleResult.IsValid) return false;

            return true;
        }

        public String GetInstallationBaseFolder(string folder) {

            try {
                DirectoryInfo di = new DirectoryInfo(folder);

                if (string.Equals(di.Name, ConstantsBank.SCProductName, StringComparison.InvariantCultureIgnoreCase)) {
                    return di.Parent.FullName;
                }
                return folder;
            }
            catch (Exception) {
                return null;
            }
        }
    }
}
