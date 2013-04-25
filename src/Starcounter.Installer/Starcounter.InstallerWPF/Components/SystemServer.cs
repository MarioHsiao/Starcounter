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
    /// <summary>
    /// 
    /// </summary>
    public class SystemServer : BaseComponent
    {

        public const string Identifier = "SystemServer";

        public override string ComponentIdentifier
        {
            get
            {
                return SystemServer.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "System Server";
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

        /// <summary>
        /// Gets a value indicating whether this instance can be installed by starcounter installer.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can be installed; otherwise, <c>false</c>.
        /// </value>
        public override bool CanBeInstalled {
            get {
                return false;
            }
        }


        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.SystemServer];

            this.Path = System.IO.Path.Combine(Environment.GetEnvironmentVariable("SystemDrive"), Configuration.StarcounterCommonPath);

            this.DefaultUserHttpPort = StarcounterConstants.NetworkPorts.DefaultSystemServerUserHttpPort;
            this.DefaultSystemHttpPort = StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort;
            this.DefaultPrologSqlProcessPort = StarcounterConstants.NetworkPorts.DefaultSystemPrologSqlProcessPort;

            switch (this.Command)
            {
                case ComponentCommand.Install:
                    this.ExecuteCommand = false; // !this.IsInstalled;
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

        public SystemServer(ObservableCollection<BaseComponent> components)
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
