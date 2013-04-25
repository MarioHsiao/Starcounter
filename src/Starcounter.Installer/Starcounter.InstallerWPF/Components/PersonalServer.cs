using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.Collections;
using Starcounter.Internal;

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

            this.Path = basePath;

            this.DefaultUserHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort;
            this.DefaultSystemHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
            this.DefaultPrologSqlProcessPort = StarcounterConstants.NetworkPorts.DefaultPersonalPrologSqlProcessPort;

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

        private UInt16 _DefaultUserHttpPort;
        public UInt16 DefaultUserHttpPort
        {
            get
            {
                return _DefaultUserHttpPort;
            }

            set
            {
                if (_DefaultUserHttpPort == value)
                    return;

                this._DefaultUserHttpPort = value;
                this.OnPropertyChanged("DefaultUserHttpPort");
            }
        }

        private UInt16 _DefaultSystemHttpPort;
        public UInt16 DefaultSystemHttpPort
        {
            get
            {
                return _DefaultSystemHttpPort;
            }

            set
            {
                if (_DefaultSystemHttpPort == value)
                    return;

                this._DefaultSystemHttpPort = value;
                this.OnPropertyChanged("DefaultSystemHttpPort");
            }
        }

        private UInt16 _DefaultPrologSqlProcessPort;
        public UInt16 DefaultPrologSqlProcessPort
        {
            get
            {
                return _DefaultPrologSqlProcessPort;
            }

            set
            {
                if (_DefaultPrologSqlProcessPort == value)
                    return;

                this._DefaultPrologSqlProcessPort = value;
                this.OnPropertyChanged("DefaultPrologSqlProcessPort");
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
