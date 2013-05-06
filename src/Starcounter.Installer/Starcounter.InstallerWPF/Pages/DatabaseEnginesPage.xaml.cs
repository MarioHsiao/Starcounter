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
using System.ComponentModel;
using Starcounter.InstallerWPF.Rules;
using Starcounter.InstallerWPF.Components;
using Starcounter.InstallerEngine;

namespace Starcounter.InstallerWPF.Pages {
    /// <summary>
    /// Interaction logic for DatabaseEnginesPage.xaml
    /// </summary>
    public partial class DatabaseEnginesPage : BasePage {



        public DatabaseEnginesPage() {
            InitializeComponent();
            this.Loaded += DatabaseEnginesPage_Loaded;
        }

        void DatabaseEnginesPage_Loaded(object sender, RoutedEventArgs e) {

            MainWindow win = Application.Current.MainWindow as MainWindow;
            InstallationBase installationBaseComponent = win.Configuration.Components[InstallationBase.Identifier] as InstallationBase;

            // Personal Server DUP path check
            Binding personalPathBinding = BindingOperations.GetBinding(this.tb_PersonalServerPath, TextBox.TextProperty);
            foreach (ValidationRule rule in personalPathBinding.ValidationRules) {
                if (rule is DuplicatPathCheckRule) {
                    ((DuplicatPathCheckRule)rule).InstallationPath = installationBaseComponent.Path;
                    ((DuplicatPathCheckRule)rule).SystemServerPath = this.tb_SystemServerPath.Text;
                }
            }

            // System Server DUP path check
            Binding systemPathBinding = BindingOperations.GetBinding(this.tb_SystemServerPath, TextBox.TextProperty);
            foreach (ValidationRule rule in systemPathBinding.ValidationRules) {
                if (rule is DuplicatPathCheckRule) {
                    ((DuplicatPathCheckRule)rule).InstallationPath = installationBaseComponent.Path;
                    ((DuplicatPathCheckRule)rule).PersonalServerPath = this.tb_PersonalServerPath.Text;
                }
            }

        }

    }
}
