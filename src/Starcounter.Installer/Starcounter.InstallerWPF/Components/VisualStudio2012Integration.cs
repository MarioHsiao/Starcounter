﻿
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System;

namespace Starcounter.InstallerWPF.Components {
    public class VisualStudio2012Integration : BaseComponent {
        public const string Identifier = "VisualStudio2012Integration";

        public override string ComponentIdentifier {
            get {
                return VisualStudio2012Integration.Identifier;
            }
        }

        public override string Name {
            get {
                return "VisualStudio 2012 Integration";
            }
        }

        private readonly string[] _Dependencys = new string[] { VisualStudio2012.Identifier, PersonalServer.Identifier };
        public override string[] Dependencys {
            get {
                return this._Dependencys;
            }
        }


        protected override void SetDefaultValues() {
            base.SetDefaultValues();

            MainWindow win = System.Windows.Application.Current.MainWindow as MainWindow;

#if !SIMULATE_INSTALLATION
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.VS2012Integration];
#endif
            switch (this.Command) {
                case ComponentCommand.Install:

                    if (win.Configuration.CurrentInstallationSettings != null ) {
                        this.ExecuteCommand = (!this.IsInstalled) && (DependenciesCheck.VStudio2012Installed()) && win.Configuration.CurrentInstallationSettings.Vs2012Integration;
                    }
                    else {
                        this.ExecuteCommand = (!this.IsInstalled) && (DependenciesCheck.VStudio2012Installed());
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
        public VisualStudio2012Integration(ObservableCollection<BaseComponent> components)
            : base(components) {
        }

        public override bool ValidateSettings() {
            return true;
        }

    }
}
