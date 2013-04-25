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
using Starcounter.InstallerEngine;
using Starcounter.InstallerWPF.Converters;

namespace Starcounter.InstallerWPF.Pages
{
    /// <summary>
    /// Interaction logic for UninstallQuestionPage.xaml
    /// </summary>
    public partial class AddRemoveUninstallQuestionPage : BasePage
    {
        #region Properties

        public override bool HasErrors
        {
            get
            {
                return base.HasErrors || ((MainWindow)Application.Current.MainWindow).SetupOptions == SetupOptions.None;
            }
        }

        public override bool CanGoBack
        {
            get
            {
                return false;
            }
        }

 
        #endregion

        public AddRemoveUninstallQuestionPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(UninstallQuestionPage_Loaded);
        }

        void UninstallQuestionPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitDefaultValues();
        }

        /// <summary>
        /// Inits the default values.
        /// </summary>
        private void InitDefaultValues()
        {

            // Setup colors
            CommandToBackgroundConverter commandToBackgroundConverter = new CommandToBackgroundConverter();

            this.setupOptions_RadioButton_Option1.Background = commandToBackgroundConverter.Convert(ComponentCommand.Install, typeof(Brush), null, null) as Brush;
            this.setupOptions_RadioButton_Option2.Background = commandToBackgroundConverter.Convert(ComponentCommand.Uninstall, typeof(Brush), null, null) as Brush;
            this.setupOptions_RadioButton_Option3.Background = commandToBackgroundConverter.Convert(ComponentCommand.Uninstall, typeof(Brush), null, null) as Brush;

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            switch (mainWindow.SetupOptions)
            {
                case SetupOptions.Ask:
                    this.setupOptions_RadioButton_Option3.IsChecked = true; // Uninstall
                    break;
                case SetupOptions.AddComponents:
                    this.setupOptions_RadioButton_Option1.IsChecked = true; // Add
                    break;
                case SetupOptions.Install:
                    this.setupOptions_RadioButton_Option1.IsChecked = true; // Add
                    break;
                case SetupOptions.None:
                    this.setupOptions_RadioButton_Option1.IsChecked = true; // Add
                    break;
                case SetupOptions.RemoveComponents:
                    this.setupOptions_RadioButton_Option2.IsChecked = true; // Remove
                    break;
                case SetupOptions.Uninstall:
                    this.setupOptions_RadioButton_Option3.IsChecked = true; // Uninstall
                    break;

            }



        }


        /// <summary>
        /// Handles the Checked event of the RadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (setupOptions_RadioButton_Option1.IsChecked == true)
            {
                mainWindow.SetupOptions = SetupOptions.AddComponents;
            }
            else if (setupOptions_RadioButton_Option2.IsChecked == true)
            {
                mainWindow.SetupOptions = SetupOptions.RemoveComponents;
            }
            else if (setupOptions_RadioButton_Option3.IsChecked == true)
            {
                mainWindow.SetupOptions = SetupOptions.Uninstall;
            }

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum SetupOptions
    {
        /// <summary>
        /// 
        /// </summary>
        None,
        /// <summary>
        /// 
        /// </summary>
        Ask,
        /// <summary>
        /// 
        /// </summary>
        Install,
        /// <summary>
        /// 
        /// </summary>
        Uninstall,
        /// <summary>
        /// 
        /// </summary>
        AddComponents,
        /// <summary>
        /// 
        /// </summary>
        RemoveComponents

    }

}
