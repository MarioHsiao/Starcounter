using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.Collections;

namespace Starcounter.InstallerWPF.Components
{
    public class StarcounterAdministrator : BaseComponent
    {
        #region Properties

        public const string Identifier = "StarcounterAdministrator";

        public override string ComponentIdentifier
        {
            get
            {
                return StarcounterAdministrator.Identifier;
            }
        }


        private bool _StartWhenInstalled;
        public bool StartWhenInstalled
        {
            get
            {
                return this._StartWhenInstalled;
            }
            set
            {
                if (this._StartWhenInstalled == value) return;
                this._StartWhenInstalled = value;
                this.OnPropertyChanged("StartWhenInstalled");
            }
        }

        private bool _CreateAdministratorShortcuts;
        public bool CreateAdministratorShortcuts
        {
            get
            {
                return this._CreateAdministratorShortcuts;
            }
            set
            {
                if (this._CreateAdministratorShortcuts == value) return;
                this._CreateAdministratorShortcuts = value;
                this.OnPropertyChanged("CreateAdministratorShortcuts");
            }
        }

        #endregion


        private readonly string[] _Dependencys = new string[] {  InstallationBase.Identifier };
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

            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.Administrator];

            // Setting shortcuts installation accordingly.
            this.CreateAdministratorShortcuts = !this.IsInstalled;
            this.StartWhenInstalled = false;

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

        public StarcounterAdministrator(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <returns></returns>
        public override IList<System.Collections.DictionaryEntry> GetProperties()
        {

            List<DictionaryEntry> properties = new List<DictionaryEntry>();

            properties.Add(new DictionaryEntry("StartWhenInstalled", this.StartWhenInstalled));
            properties.Add(new DictionaryEntry("CreateDesktopShortcuts", this.CreateAdministratorShortcuts));

            return properties;

        }

    }
}
