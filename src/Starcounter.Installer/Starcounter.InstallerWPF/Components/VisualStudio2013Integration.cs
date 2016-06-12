
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System;

namespace Starcounter.InstallerWPF.Components {
    public class VisualStudio2013Integration : BaseComponent {
        public const string Identifier = "VisualStudio2013Integration";

        public override string ComponentIdentifier {
            get {
                return VisualStudio2013Integration.Identifier;
            }
        }

        public override string Name {
            get {
                return "VisualStudio 2013 Integration";
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


        private readonly string[] _Dependencys = new string[] { VisualStudio2013.Identifier, PersonalServer.Identifier };
        public override string[] Dependencys {
            get {
                return this._Dependencys;
            }
        }


        protected override void SetDefaultValues() {
            base.SetDefaultValues();

            MainWindow win = System.Windows.Application.Current.MainWindow as MainWindow;


#if !SIMULATE_INSTALLATION
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.VS2013Integration];
#endif
            switch (this.Command) {
                case ComponentCommand.Install:

                    if (win.Configuration.CurrentInstallationSettings != null) {
                        this.ExecuteCommand = (!this.IsInstalled) && (DependenciesCheck.VStudio2013Installed()) && win.Configuration.CurrentInstallationSettings.Vs2013Integration;
                    }
                    else {
                        this.ExecuteCommand = (!this.IsInstalled) && (DependenciesCheck.VStudio2013Installed());
                    }

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
        public VisualStudio2013Integration(ObservableCollection<BaseComponent> components)
            : base(components) {
        }

        public override bool ValidateSettings() {
            return true;
        }

    }
}
