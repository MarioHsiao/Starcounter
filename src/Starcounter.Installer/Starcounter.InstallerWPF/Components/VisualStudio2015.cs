
using Starcounter.InstallerEngine;
using System;
using System.Collections.ObjectModel;

namespace Starcounter.InstallerWPF.Components
{
    public class VisualStudio2015 : BaseComponent
    {
        public const string Identifier = "VisualStudio 2015";

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
                return VisualStudio2015.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "VisualStudio 2015";
            }
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.IsInstalled = false;//DependenciesCheck.VStudio2015Installed();

        }

        public VisualStudio2015(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }

    }
}
