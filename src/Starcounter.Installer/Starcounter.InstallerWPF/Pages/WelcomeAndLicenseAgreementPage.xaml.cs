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
using System.Windows.Resources;
using System.IO;

namespace Starcounter.InstallerWPF.Pages
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomeAndLicenseAgreementPage : BasePage
    {
        #region Properties

        public override bool CanGoBack
        {
            get
            {
                return !HasErrors;
            }
        }

        public override bool CanGoNext
        {
            get
            {
                return !HasErrors;
            }

        }

        #endregion

        public WelcomeAndLicenseAgreementPage()
        {
            InitializeComponent();
        }
    }
}
