using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.Collections;

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

            this.Path = System.IO.Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\" +
                                               Configuration.StarcounterCommonPath , ConstantsBank.SCSystemDatabasesName);

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
