using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Starcounter.InstallerWPF.Components
{
    public class ODBCDriver : BaseComponent
    {
        public const string Identifier = "ODBCDriver";

        public override string ComponentIdentifier
        {
            get
            {
                return ODBCDriver.Identifier;
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
        public ODBCDriver(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }

    }
}
