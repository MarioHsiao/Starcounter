
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System;

namespace Starcounter.InstallerWPF.Components
{
    public class VisualStudio2015Integration : BaseComponent
    {
        public const string Identifier = "VisualStudio2015Integration";

        public override string ComponentIdentifier
        {
            get
            {
                return VisualStudio2015Integration.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "VisualStudio 2015 Integration";
            }
        }

        //public override bool IsExecuteCommandEnabled {
        //    get {
        //        return false;
        //    }
        //}

        public override bool CanBeInstalled {
            get {
                return true;
            }
        }
        //public override bool IsExecuteCommandEnabled {
        //    get {
        //        return false;
        //    }
        //}   


        private readonly string[] _Dependencys = new string[] { VisualStudio2015.Identifier, PersonalServer.Identifier };
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

            MainWindow win = System.Windows.Application.Current.MainWindow as MainWindow;

#if !SIMULATE_INSTALLATION
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.VS2015Integration];
#endif
            switch (this.Command)
            {
                case ComponentCommand.Install:
                    this.ExecuteCommand = (!this.IsInstalled) && (DependenciesCheck.VStudio2015Installed()) && win.Configuration.SetupUserSettings.Vs2015Integration;
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

        public VisualStudio2015Integration(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override bool ValidateSettings() {
            return true;
        }

    }
}
