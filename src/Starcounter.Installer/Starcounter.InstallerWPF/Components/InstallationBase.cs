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

namespace Starcounter.InstallerWPF.Components
{
    public class InstallationBase : BaseComponent
    {

        public const string Identifier = "StarcounterInstallation";

        public override string ComponentIdentifier
        {
            get
            {
                return InstallationBase.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "Starcounter Installation Path";
            }
        }

        public override bool ShowProperties
        {
            get
            {
                return true;
            }
        }

        public override bool IsExecuteCommandEnabled
        {
            get
            {
                return false;
            }
        }

        public bool HasPath
        {
            get
            {
                return !string.IsNullOrEmpty(this.Path);
            }
        }

        private string _Path;
        public string Path
        {
            get
            {
                return this._Path;
            }
            set
            {
                if (string.Compare(this._Path, value) == 0) return;
                this._Path = value;
                this.OnPropertyChanged("Path");
            }
        }

        private bool _AddToStartMenu;
        public bool AddToStartMenu
        {
            get
            {
                return this._AddToStartMenu;
            }
            set
            {
                if (this._AddToStartMenu == value) return;
                this._AddToStartMenu = value;
                this.OnPropertyChanged("AddToStartMenu");
            }
        }

        public InstallationBase(ObservableCollection<BaseComponent> components)
            : base(components)
        {
            //this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(StarcounterInstallation_PropertyChanged);
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            // Setting the installation flag.
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.InstallationBase];

            // Setting installation path (new path is created if not installed).
            this.Path = CInstallationBase.GetInstalledDirFromEnv();
            if (string.IsNullOrEmpty(this.Path))
            {
                String programFilesPath = ConstantsBank.ProgramFilesPath;

                this.Path = System.IO.Path.Combine(programFilesPath, Configuration.StarcounterCommonPath);
            }

            switch (this.Command)
            {
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
            if (!this.IsInstalled)
            {
                this.AddToStartMenu = true;
            }
            else
            {
                this.AddToStartMenu = false;
            }
        }

        public override IList<System.Collections.DictionaryEntry> GetProperties()
        {
            List<DictionaryEntry> properties = new List<DictionaryEntry>();

            properties.Add(new DictionaryEntry("Path", this.Path));
            properties.Add(new DictionaryEntry("AddToStartMenu", this.AddToStartMenu));

            return properties;
        }


        public override bool ValidateSettings() {

            DirectoryContainsFilesRule r = new DirectoryContainsFilesRule();
            r.UseWarning = true;
            r.CheckEmptyString = true;
            ValidationResult result = r.Validate(this.Path, CultureInfo.CurrentCulture);

            return result.IsValid;
        }
    }
}
