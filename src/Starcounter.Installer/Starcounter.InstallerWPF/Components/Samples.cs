using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;

namespace Starcounter.InstallerWPF.Components
{

    public class Samples : BaseComponent
    {
        public const string Identifier = "Samples";

        bool _CanBeUnInstalled = false;
        public override bool CanBeUnInstalled
        {
            get
            {
                return _CanBeUnInstalled;
            }
        }

        bool _CanBeInstalled = false;
        public override bool CanBeInstalled
        {
            get
            {
                return _CanBeInstalled;
            }
        }

        public override string ComponentIdentifier
        {
            get
            {
                return Samples.Identifier;
            }
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            //this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.Administrator];
            this.IsInstalled = false; // ComponentsCheck.AnyComponentsExist();

            switch (this.Command)
            {
                case ComponentCommand.Install:
                    this.ExecuteCommand = false; //!this.IsInstalled;
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

        public Samples(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }


        public void SetCanBeInstalled(bool value)
        {
            this._CanBeInstalled = value;
        }

        public void SetCanBeUnInstalled(bool value)
        {
            this._CanBeUnInstalled = value;
        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }

    }
}
