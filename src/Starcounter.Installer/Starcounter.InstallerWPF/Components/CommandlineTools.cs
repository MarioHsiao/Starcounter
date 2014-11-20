using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;

namespace Starcounter.InstallerWPF.Components
{
    public class CommandlineTools :  BaseComponent
    {
        public const string Identifier = "CommandlineTools";

        public override string ComponentIdentifier
        {
            get
            {
                return CommandlineTools.Identifier;
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

        public override bool CanBeInstalled
        {
            get
            {
                return false;
            }
        }


        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

//            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.CommandlineTools];
            this.IsInstalled = false;

            switch (this.Command)
            {
                case ComponentCommand.Install:
                    this.ExecuteCommand = false;
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

        public CommandlineTools(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }


    }
}
