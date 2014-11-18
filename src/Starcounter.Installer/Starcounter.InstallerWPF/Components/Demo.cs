using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using Starcounter.InstallerEngine;

namespace Starcounter.InstallerWPF.Components
{
    public class Demo : BaseComponent
    {

        #region Properties

        public const string Identifier = "Demo";

        public override string ComponentIdentifier
        {
            get
            {
                return Demo.Identifier;
            }
        }

        public override bool CanBeUnInstalled
        {
            get
            {
                return false;
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

        #endregion


        private readonly string[] _Dependencys = new string[] {  InstallationBase.Identifier, SystemServer.Identifier+"|"+PersonalServer.Identifier  };
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


            // TODO: Add ComponentsCheck.Components.Demo
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.InstallationBase];

            // Setting shortcuts installation accordingly.
            this.StartWhenInstalled = !this.IsInstalled;

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

        public Demo(ObservableCollection<BaseComponent> components)
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

            return properties;

        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }


    }
}
