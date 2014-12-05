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

namespace Starcounter.InstallerWPF.Pages
{

    /// <summary>
    /// Interaction logic for InstallationPathPage.xaml
    /// </summary>
    public partial class InstallationPathPage : BasePage
    {
        public InstallationPathPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(InstallationPathPage_Loaded);
        }

        void InstallationPathPage_Loaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(System.Windows.Application.Current.MainWindow), this.tb_MainInstallationPath);
        }
    }
}
