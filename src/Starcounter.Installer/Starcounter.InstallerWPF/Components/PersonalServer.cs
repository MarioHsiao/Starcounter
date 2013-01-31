using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.Collections;

namespace Starcounter.InstallerWPF.Components
{
    public class PersonalServer : BaseComponent
    {

        public const string Identifier = "PersonalServer";

        public override string ComponentIdentifier
        {
            get
            {
                return PersonalServer.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "Personal Server";
            }
        }

        private readonly string[] _Dependencys = new string[] { InstallationBase.Identifier };
        public override string[] Dependencys
        {
            get
            {
                return this._Dependencys;
            }
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.PersonalServer];

            string basePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                                     Configuration.StarcounterCommonPath );

            this.Path = System.IO.Path.Combine(basePath, ConstantsBank.SCPersonalDatabasesName);

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


        public PersonalServer(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override IList<System.Collections.DictionaryEntry> GetProperties()
        {

            List<DictionaryEntry> properties = new List<DictionaryEntry>();

            properties.Add(new DictionaryEntry("Path", this.Path));

            return properties;

        }

    }
}
