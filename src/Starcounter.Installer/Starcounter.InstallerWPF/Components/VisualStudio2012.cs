
using Starcounter.InstallerEngine;
using System;
using System.Collections.ObjectModel;

namespace Starcounter.InstallerWPF.Components
{
    public class VisualStudio2012 : BaseComponent
    {
        public const string Identifier = "VisualStudio 2012";

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
                return VisualStudio2012.Identifier;
            }
        }

        public override string Name
        {
            get
            {
                return "VisualStudio 2012";
            }
        }

        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.IsInstalled = DependenciesCheck.VStudio2012Installed();

        }

        public VisualStudio2012(ObservableCollection<BaseComponent> components)
            : base(components)
        {
        }

        public override bool ValidateSettings() {
            throw new NotImplementedException();
        }

    }
}
