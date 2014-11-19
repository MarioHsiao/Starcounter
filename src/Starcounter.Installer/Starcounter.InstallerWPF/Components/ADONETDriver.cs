using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Starcounter.InstallerWPF.Components
{


    public class ADONETDriver : BaseComponent
    {
        public const string Identifier = "ADONETDriver";

        public override string ComponentIdentifier
        {
            get
            {
                return ADONETDriver.Identifier;
            }
        }

        public override bool IsExecuteCommandEnabled
        {
            get
            {
                return false;
            }
        }

        public override bool ShowProperties
        {
            get
            {
                return false;
            }
        }

        public override string Comment
        {
            get
            {
                return "(Coming soon)";
            }
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

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

        public ADONETDriver(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }

    }
}
