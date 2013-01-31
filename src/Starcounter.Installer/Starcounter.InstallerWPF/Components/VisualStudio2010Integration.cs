using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starcounter.InstallerEngine;
using System.Collections.ObjectModel;

namespace Starcounter.InstallerWPF.Components
{
    public class VisualStudio2010Integration : BaseComponent
    {
        public const string Identifier = "VisualStudio2010Integration";

        public override string ComponentIdentifier
        {
            get
            {
                return VisualStudio2010Integration.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "VisualStudio 2010 Integration";
            }
        }

        private readonly string[] _Dependencys = new string[] {  VisualStudio2010.Identifier, PersonalServer.Identifier };
        public override string[] Dependencys
        {
            get
            {
                return this._Dependencys;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can be installed by starcounter installer.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can be installed; otherwise, <c>false</c>.
        /// </value>
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

            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.VS2010Integration];

            switch (this.Command)
            {
                case ComponentCommand.Install:
                    this.ExecuteCommand = false; //(!this.IsInstalled) && (DependenciesCheck.VStudio2010Installed());
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
        public VisualStudio2010Integration(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }    

    }
}
