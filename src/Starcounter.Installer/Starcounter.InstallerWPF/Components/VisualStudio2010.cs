using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;

namespace Starcounter.InstallerWPF.Components
{

    public class VisualStudio2010 : BaseComponent
    {
        public const string Identifier = "VisualStudio 2010";

        public override bool CanBeInstalled
        {
            get
            {
                return false;
            }
        }
        public override string ComponentIdentifier
        {
            get
            {
                return VisualStudio2010.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "VisualStudio 2010";
            }
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.IsInstalled = DependenciesCheck.VStudio2010Installed();

        }

        public VisualStudio2010(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

    }
}
