using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Starcounter.InstallerEngine;
using Starcounter.Internal;

namespace Starcounter.InstallerWPF.Pages
{
    public partial class DeveloperToolsPage : BasePage
    {

        public override bool CanGoNext
        {
            get
            {
                // NOTE: Installation can proceed even when zero components are selected.
                // In this case InstallationBase will be installed.
                return true;

                /*Configuration config = this.DataContext as Configuration;

                if (!config.CanExecute)
                {
                    return false;
                }

                return base.CanGoNext;*/
            }
        }

        public DeveloperToolsPage()
        {
            InitializeComponent();
        }

    }
}
