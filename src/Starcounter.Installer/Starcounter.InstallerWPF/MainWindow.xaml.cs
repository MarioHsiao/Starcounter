using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Starcounter.InstallerWPF.Pages;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Windows.Data;
using System.Collections;
using Starcounter.InstallerWPF.Components;
using Starcounter.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using Starcounter.InstallerWPF.DemoSequence;
using Starcounter.Internal;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Resources;



namespace Starcounter.InstallerWPF {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        #region Commands

        #region NextPage

        private void CanExecute_NextPage_Command(object sender, CanExecuteRoutedEventArgs e) {

            if (this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                if (this.pages_lb.Items.CurrentPosition == this.pages_lb.Items.Count - 1) {
                    e.CanExecute = false;
                }
                else {
                    BasePage page = this.pages_lb.Items.CurrentItem as BasePage;
                    e.CanExecute = page.CanGoNext;
                }
                e.Handled = true;
            }
        }
        private void Executed_NextPage_Command(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;
            this.pages_lb.Items.MoveCurrentToNext();
        }

        #endregion

        #region PreviousPage

        private void CanExecute_PreviousPage_Command(object sender, CanExecuteRoutedEventArgs e) {
            if (this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                if (this.pages_lb.Items.CurrentPosition == 0) {
                    e.CanExecute = false;
                }
                else {

                    BasePage page = this.pages_lb.Items.CurrentItem as BasePage;
                    e.CanExecute = page.CanGoBack;
                }
                e.Handled = true;
            }
        }
        private void Executed_PreviousPage_Command(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;
            //this.pages_lb.IsSynchronizedWithCurrentItem 
            //NavigationCommands.PreviousPage
            //NavigationCommands.GoToPage
            bool result = this.pages_lb.Items.MoveCurrentToPrevious();

        }

        #endregion

        #region GoToPage

        private void CanExecute_GoToPage_Command(object sender, CanExecuteRoutedEventArgs e) {

            if (e.Parameter is Exception) {
                e.Handled = true;
                e.CanExecute = true;
                return;
            }

            if (e.OriginalSource is Hyperlink && !string.IsNullOrEmpty(e.Parameter as string)) {
                e.CanExecute = true;
                e.Handled = true;
                return;
            }

            if (this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                BasePage page = this.pages_lb.Items.CurrentItem as BasePage;
                e.CanExecute = page.CanGoNext;
                e.Handled = true;
                return;
            }

            if (e.Parameter is BasePage) {
                e.Handled = true;
                e.CanExecute = true;
                return;
            }
            if (string.IsNullOrEmpty(e.Parameter as string)) {
                e.Handled = true;
                e.CanExecute = false;
                return;
            }

        }

        private void Executed_GoToPage_Command(object sender, ExecutedRoutedEventArgs e) {

            if (e.Handled == false && e.OriginalSource is Hyperlink && !string.IsNullOrEmpty(e.Parameter as string)) {
                // Used to go to web page

                this.linksUserClickedOn += e.Parameter + Environment.NewLine;

                try {
                    this.OpenBrowser(e.Parameter as string);
                    e.Handled = true;
                }
                catch (Win32Exception ee) {
                    string message = "Can not open external browser." + Environment.NewLine + ee.Message + Environment.NewLine + e.Parameter;
                    this.OnError(new Exception(message));
                }

                //try {
                //    Process.Start(new ProcessStartInfo(e.Parameter as string));
                //    e.Handled = true;
                //}
                //catch (Win32Exception) {

                //    try {
                //        Process.Start(new ProcessStartInfo("explorer.exe", e.Parameter as string));
                //        e.Handled = true;
                //    }
                //    catch (Win32Exception ee) {
                //        string message = "Can not open external browser." + Environment.NewLine + ee.Message + Environment.NewLine + e.Parameter;
                //        this.OnError(new Exception(message));
                //    }

                //}
                return;
            }


            if (e.Parameter is Exception) {
                // Remove finish page
                foreach (BasePage page in this.Pages) {
                    if (page is IFinishedPage) {
                        this.Pages.Remove(page);
                        break;
                    }
                }

                // Add error Page;
                ErrorPage errorPage = new ErrorPage();
                errorPage.Exception = e.Parameter as Exception;
                this.RegisterPage(errorPage);
                this.pages_lb.Items.MoveCurrentTo(errorPage);
                e.Handled = true;
                return;
            }

            foreach (BasePage page in this.Pages) {
                if (page.GetType().Name.Equals(e.Parameter)) {
                    this.pages_lb.Items.MoveCurrentTo(page);
                    e.Handled = true;
                    break;
                }
            }

            if (e.Parameter is BasePage) {
                BasePage page = this.RegisterPage(e.Parameter as BasePage);
                this.pages_lb.Items.MoveCurrentTo(page);
                e.Handled = true;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private void OpenBrowser(string link) {

            try {
                Process.Start(new ProcessStartInfo(link));
            }
            catch (Win32Exception) {
                Process.Start(new ProcessStartInfo("explorer.exe", link));
            }
        }


        #endregion

        #region Open License Agreement

        public static RoutedUICommand OpenLicenseAgreementRoutedCommand = new RoutedUICommand();

        private void CanExecute_OpenLicenseAgreement_Command(object sender, CanExecuteRoutedEventArgs e) {
            e.Handled = true;
            e.CanExecute = true;
        }
        private void Execute_OpenLicenseAgreement_Command(object sender, ExecutedRoutedEventArgs e) {

            e.Handled = true;
            LicenseAgreementWindow win = new LicenseAgreementWindow() { Owner = this };
            win.ShowDialog();

        }
        #endregion

        #region Commands
        private void CanExecute_ChooseFolder_Command(object sender, CanExecuteRoutedEventArgs e) {
            e.Handled = true;
            e.CanExecute = true;
        }
        private void Executed_ChooseFolder_Command(object sender, ExecutedRoutedEventArgs e) {


            if (e.OriginalSource is TextBox) {
                e.Handled = true;
                this.ChooseFolderDialog(e.OriginalSource as TextBox, e.Parameter as string);
            }
        }
        #endregion

        #region Close

        private void CanExecute_Close_Command(object sender, CanExecuteRoutedEventArgs e) {
            if (this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                BasePage page = this.pages_lb.Items.CurrentItem as BasePage;
                e.CanExecute = page.CanClose;
                e.Handled = true;
            }
        }
        private void Executed_Close_Command(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;
            this.Close();
        }

        #endregion

        #region Start
        public static RoutedUICommand StartRoutedCommand = new RoutedUICommand();

        private void CanExecute_Start_Command(object sender, CanExecuteRoutedEventArgs e) {
            if (this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                BasePage page = this.pages_lb.Items.CurrentItem as BasePage;
                e.CanExecute = page.CanClose;
                e.Handled = true;
            }
        }

        private void Executed_Start_Command(object sender, ExecutedRoutedEventArgs e) {
            e.Handled = true;

            bool bStartDemoComponent = false;
            Demo demoComponent = this.Configuration.Components[Demo.Identifier] as Demo;
            if (demoComponent != null && demoComponent.StartWhenInstalled && demoComponent.ExecuteCommand == true) {
                bStartDemoComponent = true;
            }

            // Generate ArgumentFile For Demo-Sequence
            this.GenerateArgumentFileForDemoSequence(bStartDemoComponent);

            // Filtering post-start applications.
            InstallerMain.FilterStartupFile(true, bStartDemoComponent);

            IFinishedPage finishPage = this.pages_lb.Items.CurrentItem as IFinishedPage;

            if (finishPage != null && finishPage.GoToWiki && this.Configuration.Unattended == false) {

                try {
                    string link = @"https://starcounter.io/docs/";
                    this.OpenBrowser(link);
                }
                catch (Win32Exception ee) {
                    string message = "Failed to open web browser." + Environment.NewLine + ee.Message + Environment.NewLine + e.Parameter;
                    this.OnError(new Exception(message));
                    return;
                }
            }

            if (finishPage != null && this.Configuration.Unattended && !string.IsNullOrEmpty(this.FinishedMessageInUnattendedMode)) {
                this.Hide();
                //                this.Visibility = Visibility.Hidden;
                WpfMessageBox.Show(this.FinishedMessageInUnattendedMode, "Starcounter Installation");
            }

            this.Close();   // Close Installer program and lets the waiting parent process continue
        }


        // Generate a file with arguments for the demo (executable)
        // The when the demo is started (started from another process) this file will be read.
        // A simple way of passing arguments between processes
        private void GenerateArgumentFileForDemoSequence(bool bStartDemoComponent) {
            string startDemoArgumentsFile = ConstantsBank.ScStartDemosTemp;

            // Cleanup old file if it exists (just to be safe).
            if (File.Exists(startDemoArgumentsFile)) {
                // Check readonly flag
                FileAttributes attr = File.GetAttributes(startDemoArgumentsFile);
                bool isReadOnly = ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
                if (isReadOnly) {
                    // Remove readonly flag
                    attr ^= FileAttributes.ReadOnly;
                    File.SetAttributes(startDemoArgumentsFile, attr);
                }
                File.Delete(startDemoArgumentsFile);
            }

            if (bStartDemoComponent) {

                string demoPath = this.GetDemoPath();

                if (!string.IsNullOrEmpty(demoPath)) {
                    // Getting personal server installation path.

                    VisualStudio2012Integration visualStudio2012IntegrationComponent = this.Configuration.Components[VisualStudio2012Integration.Identifier] as VisualStudio2012Integration;
                    PersonalServer personalServerComponent = this.Configuration.Components[PersonalServer.Identifier] as PersonalServer;
                    SystemServer systemServerComponent = this.Configuration.Components[SystemServer.Identifier] as SystemServer;

                    String postDemoType = String.Empty;

                    if ((visualStudio2012IntegrationComponent != null && visualStudio2012IntegrationComponent.IsAvailable) &&
                        (personalServerComponent != null && personalServerComponent.IsAvailable)) {
                        postDemoType = PostDemoTypeEnum.VS2012.ToString();
                    }

                    // Checking if no VS solution should be started.
                    if ((String.IsNullOrEmpty(postDemoType)) &&
                            ((personalServerComponent != null && personalServerComponent.IsAvailable) ||
                             (systemServerComponent != null && systemServerComponent.IsAvailable))) {
                        postDemoType = PostDemoTypeEnum.PREBUILT.ToString();
                    }

                    File.AppendAllText(startDemoArgumentsFile, demoPath + Environment.NewLine);
                    File.AppendAllText(startDemoArgumentsFile, postDemoType.ToString() + Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Gets the demo path.
        /// </summary>
        /// <returns>'Root' path to demos or null</returns>
        private string GetDemoPath() {
            PersonalServer personalServer = this.GetComponent(PersonalServer.Identifier) as PersonalServer;
            if (personalServer != null && !string.IsNullOrEmpty(personalServer.Path)) {
                return personalServer.Path;
            }
            return null;
        }


        #endregion

        #endregion

        #region Properties

        private string linksUserClickedOn = string.Empty;

        // Setup Options
        private SetupOptions _SetupOptions = SetupOptions.None;
        public SetupOptions SetupOptions {
            get { return this._SetupOptions; }
            set {
                if (this._SetupOptions == value) return;
                this._SetupOptions = value;
                this.OnPropertyChanged("SetupOptions");
            }
        }

        private ObservableCollection<object> _Pages = new ObservableCollection<object>();
        public ObservableCollection<object> Pages {
            get { return this._Pages; }

        }

        private Configuration _Configuration = new Configuration();
        public Configuration Configuration {
            get {
                return this._Configuration;
            }
            set {
                this._Configuration = value;
            }
        }

        private bool _ChangeAdditionalSettings = false;
        public bool ChangeAdditionalSettings {
            get {
                return this._ChangeAdditionalSettings;
            }
            set {
                this._ChangeAdditionalSettings = value;

                // Clear previous pages setup
                //while (this.Pages.Count > 1) {
                //    this.Pages.RemoveAt(1);
                //}
                this.OnPropertyChanged("ChangeAdditionalSettings");
                this.OnPropertyChanged("SetupOptions");
            }
        }



        public string NextButtonTitle {
            get {

                if (this.pages_lb == null || this.pages_lb.Items.CurrentItem == null || this.pages_lb.Items.CurrentPosition == this.pages_lb.Items.Count - 1) {
                    return "_Unknown";
                }

                BasePage nextPage = this.pages_lb.Items[this.pages_lb.Items.CurrentPosition + 1] as BasePage;
                if (nextPage is IProgressPage) {

                    if (this.SetupOptions == InstallerWPF.Pages.SetupOptions.RemoveComponents ||
                        this.SetupOptions == InstallerWPF.Pages.SetupOptions.Uninstall) {
                        return "_Uninstall";
                    }

                    return "_Install";
                }

                return "_Next";

            }
        }

        //private Boolean[] _InstalledComponents;
        //public Boolean[] InstalledComponents
        //{
        //    get
        //    {
        //        return this._InstalledComponents;
        //    }
        //    set
        //    {
        //        this._InstalledComponents = value;
        //    }

        //}

        private string _Version;
        public string Version {
            get {
                return this._Version;
            }
            protected set {
                if (string.Compare(this._Version, value, true) == 0) return;
                this._Version = value;
                this.OnPropertyChanged("Version");
            }
        }


        private IList<String> _PoweredByResources = new ObservableCollection<string>();
        public IList<String> PoweredByResources {
            get {
                return this._PoweredByResources;
            }
        }

        public static Boolean[] InstalledComponents;

        public SetupOptions DefaultSetupOptions = SetupOptions.None;
        public string FinishedMessageInUnattendedMode = null;

        #endregion

        public MainWindow() {

            //if (Properties.Settings.Default.UpgradeRequired) {
            //    Properties.Settings.Default.Upgrade();
            //    Properties.Settings.Default.UpgradeRequired = false;
            //    Properties.Settings.Default.Save();
            //}

            this.Closing += new CancelEventHandler(MainWindow_Closing);
            this.PropertyChanged += new PropertyChangedEventHandler(MainWindow_PropertyChanged);
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Closed += new EventHandler(CleanUpAfterUninstall);

            this.InitPoweredByResources();

            InitializeComponent();
        }


        /// <summary>
        /// Populate "poweredby" resource image list
        /// </summary>
        private void InitPoweredByResources() {

            string resource1 = "resources/poweredby1.png";
            if (this.IsResourceIsAvailable(resource1)) {
                this.PoweredByResources.Add(resource1);
            }

            string resource2 = "resources/poweredby2.png";
            if (this.IsResourceIsAvailable(resource2)) {
                this.PoweredByResources.Add(resource2);
            }

        }

        /// <summary>
        /// Check if resource is available
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsResourceIsAvailable(string name) {

            try {
                StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/../" + name));
                if (sri != null && sri.Stream.Length > 0) {
                    return true;
                }
            }
            catch { }

            return false;
        }

        void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if ("SetupOptions".Equals(e.PropertyName)) {

                // Clear previous pages setup
                while (this.Pages.Count > 1) {
                    this.Pages.RemoveAt(1);
                }

                this.Configuration.SetDefaultValues(this.SetupOptions);

                switch (this.SetupOptions) {
                    case SetupOptions.Install:
                        this.UpdateComponentsCommand(ComponentCommand.Install);
                        this.RegisterFirstInstallationPages();
                        break;

                    case SetupOptions.Ask:
                        this.UpdateComponentsCommand(ComponentCommand.None);
                        this.RegisterAddRemoveUninstallQuestionPages();
                        break;

                    case SetupOptions.None:
                        this.UpdateComponentsCommand(ComponentCommand.None);
                        this.RegisterAddRemoveUninstallQuestionPages();
                        break;

                    case SetupOptions.AddComponents:
                        this.UpdateComponentsCommand(ComponentCommand.Install);
                        this.RegisterAddComponentsPages();
                        break;

                    case SetupOptions.RemoveComponents:
                        this.UpdateComponentsCommand(ComponentCommand.Uninstall);
                        this.RegisterRemoveComponentsPages();
                        break;

                    case SetupOptions.Uninstall:
                        this.UpdateComponentsCommand(ComponentCommand.Uninstall);
                        this.RegisterUninstallPages();
                        break;
                }


                if (this.SetupOptions != InstallerWPF.Pages.SetupOptions.Ask &&
                    this.SetupOptions != SetupOptions.None) {
                    // Validate Environment
                    if (!this.ValidateEnvironment()) {
                        // Exiting the application.
                        Environment.Exit(1);
                    }
                }
            }
        }

        /// <summary>
        /// Calls InstallerEngine for proper version info implementation.
        /// </summary>
        /// <returns></returns>
        private string GetVersionString() {
            return "Version " + CurrentVersion.Version;
        }

        /// <summary>
        /// Checks recommended hardware settings for Starcounter.
        /// </summary>
        void CheckHardwareStatus() {

            Boolean notFullfilled = false;
            String faults = "";

            if (Utilities.LessThan4GbMemory()) {
                notFullfilled = true;
                faults += "Your machine has less than 4Gb of RAM." + Environment.NewLine;
            }

            if (Environment.ProcessorCount <= 1) {
                notFullfilled = true;
                faults += "Your machine has less than 2 CPU logical processors." + Environment.NewLine;
            }

            // Checking requirements are not met.
            if (notFullfilled) {

                WpfMessageBox.Show(
                    "To be productive Starcounter recommends that your machine has at least 4Gb of RAM and 2 CPU logical processors." +
                    Environment.NewLine + faults +
                    Environment.NewLine + "You can now proceed with installation.",
                    "Recommended hardware requirements are not met.",
                    WpfMessageBoxButton.OK, WpfMessageBoxImage.Exclamation);
            }
        }


        void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            this._InternalComponents.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_InternalComponents_CollectionChanged);

            this.Pages.CollectionChanged += Pages_CollectionChanged;
            this.pages_lb.SelectionChanged += pages_lb_SelectionChanged;

            // Retrieve Version of setup package
            this.Version = this.GetVersionString();

            // Retrieve current installed components
            MainWindow.InstalledComponents = ComponentsCheck.GetListOfInstalledComponents();

            this.DataContext = this.Configuration;

            // Setup available components
            this.SetupComponents();

#if SIMULATE_INSTALLATION
            WpfMessageBoxResult result = WpfMessageBox.Show("Simulate First time installation?", "DEBUG", WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question);

            if (result == WpfMessageBoxResult.Yes) {
                this.RegisterPage(new WelcomeAndLicenseAgreementPage());
                this.SetupOptions = SetupOptions.Install;
            }
            else {

                if (this.DefaultSetupOptions == SetupOptions.None) {
                    this.SetupOptions = SetupOptions.Ask;
                }
                else {
                    this.SetupOptions = this.DefaultSetupOptions;
                }
            }
#else
            if (!this.HasCurrentInstalledComponents()) {
                // Checking system recommendations.
                CheckHardwareStatus();

                if (!this.Configuration.Unattended) {
                    this.RegisterPage(new WelcomeAndLicenseAgreementPage());
                }
                this.SetupOptions = SetupOptions.Install;
            }
            else {

                if (this.DefaultSetupOptions == SetupOptions.None) {
                    this.SetupOptions = SetupOptions.Ask;
                }
                else {
                    this.SetupOptions = this.DefaultSetupOptions;
                }
            }
#endif
            this.Activate();

        }

        void Pages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            this.OnPropertyChanged("NextButtonTitle");


        }

        /// <summary>
        /// Determines whether there is some components already installed.
        /// </summary>
        /// <returns>
        /// <c>true</c> if there is installed components; otherwise, <c>false</c>.
        /// </returns>
        private bool HasCurrentInstalledComponents() {
            if (MainWindow.InstalledComponents == null) {
                // Retrieve current installed components
                MainWindow.InstalledComponents = ComponentsCheck.GetListOfInstalledComponents();
            }

            // Checking if something is installed.
            foreach (Boolean component in MainWindow.InstalledComponents) {
                if (component) {
                    return true;
                }
            }
            return false;
        }

        private bool bDelayedClose = false;

        /// <summary>
        /// Handles the Closing event of the MainWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        void MainWindow_Closing(object sender, CancelEventArgs e) {

            if (bDelayedClose == true) {
                e.Cancel = false;
                return;
            }

            if (this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                BasePage page = this.pages_lb.Items.CurrentItem as BasePage;
                e.Cancel = !page.CanClose;
            }

            if (e.Cancel == false && this.pages_lb != null && this.pages_lb.Items.CurrentItem != null) {
                if (!(this.pages_lb.Items.CurrentItem is IFinishedPage || this.pages_lb.Items.CurrentItem is ErrorPage)) {

                    WpfMessageBoxResult result = WpfMessageBox.Show("Do you want to exit the setup program?", "Starcounter - Setup", WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question);
                    if (result == WpfMessageBoxResult.Yes) {
                        e.Cancel = false;
                    }
                    else {
                        e.Cancel = true;

                    }
                }
            }


            // Cancel the close down and use a delayed closedown
            if (e.Cancel == false && this.bDelayedClose == false) {
                e.Cancel = true;

                this.bDelayedClose = true;

                this.Hide();

                // Send the tracking data before we close down.
                Dispatcher disp = Dispatcher.FromThread(Thread.CurrentThread);

                Starcounter.Tracking.Client.Instance.SendInstallerEnd(this.linksUserClickedOn,
                      delegate (object sender2, Starcounter.Tracking.CompletedEventArgs args) {
                          // Send compleated (success or error)
                          disp.BeginInvoke(DispatcherPriority.Normal, new Action(delegate {
                              // Close down on our main thread
                              this.Close();
                          }));
                      }
                );
            }
        }

        /// <summary>
        /// Handles the Closed event of the MainWindow control when uninstalling.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        void CleanUpAfterUninstall(object sender, EventArgs e) {
            if (SetupOptions.Uninstall == this.SetupOptions)
                UninstallEngine.DeleteInstallationDir(false);
        }

        #region Setup Components

        private ObservableCollection<BaseComponent> _InternalComponents = new ObservableCollection<BaseComponent>();

        /// <summary>
        /// Setups the components.
        /// </summary>
        private void SetupComponents() {
            // Pre

            VisualStudio2012 visualStudio2012 = new VisualStudio2012(this._InternalComponents);
            this._InternalComponents.Add(visualStudio2012);

            VisualStudio2013 visualStudio2013 = new VisualStudio2013(this._InternalComponents);
            this._InternalComponents.Add(visualStudio2013);

            VisualStudio2015 visualStudio2015 = new VisualStudio2015(this._InternalComponents);
            this._InternalComponents.Add(visualStudio2015);

            // Starcounter installation

            InstallationBase starcounterInstallation = new InstallationBase(this._InternalComponents);
            this._InternalComponents.Add(starcounterInstallation);

            // System Server

            SystemServer systemServer = new SystemServer(this._InternalComponents);
            this._InternalComponents.Add(systemServer);

            // Personal Server

            PersonalServer personalServer = new PersonalServer(this._InternalComponents);
            this._InternalComponents.Add(personalServer);

            // Commandline Tools

            //CommandlineTools commandlineTools = new CommandlineTools(this._InternalComponents);
            //this._InternalComponents.Add(commandlineTools);


            // Connectivity

            //ODBCDriver oDBCDriver = new ODBCDriver(this._InternalComponents);
            //this._InternalComponents.Add(oDBCDriver);

            //ADONETDriver aDONETDriver = new ADONETDriver(this._InternalComponents);
            //this._InternalComponents.Add(aDONETDriver);

            //LiveObjects liveObjects = new LiveObjects(this._InternalComponents);
            //this._InternalComponents.Add(liveObjects);

            // Developer tools

            VisualStudio2012Integration visualStudio2012Integration = new VisualStudio2012Integration(this._InternalComponents);
            this._InternalComponents.Add(visualStudio2012Integration);

            VisualStudio2013Integration visualStudio2013Integration = new VisualStudio2013Integration(this._InternalComponents);
            this._InternalComponents.Add(visualStudio2013Integration);

            VisualStudio2015Integration visualStudio2015Integration = new VisualStudio2015Integration(this._InternalComponents);
            this._InternalComponents.Add(visualStudio2015Integration);

            // Samples

            //Samples samples = new Samples(this._InternalComponents);
            //this._InternalComponents.Add(samples);

            // Demo

            Demo demo = new Demo(this._InternalComponents);
            this._InternalComponents.Add(demo);
        }

        /// <summary>
        /// Handles the CollectionChanged event of the _InternalComponents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        void _InternalComponents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {

            switch (e.Action) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:

                    foreach (BaseComponent component in e.NewItems) {
                        this.Configuration.Components.Add(component.ComponentIdentifier, component);
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
            }

        }

        /// <summary>
        /// Gets the component.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns></returns>
        private BaseComponent GetComponent(string identifier) {
            foreach (BaseComponent component in this._InternalComponents) {
                if (string.Equals(component.ComponentIdentifier, identifier)) {
                    return component;
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the components command.
        /// </summary>
        /// <param name="command">The command.</param>
        private void UpdateComponentsCommand(ComponentCommand command) {

            IDictionaryEnumerator _enumerator = this.Configuration.Components.GetEnumerator();

            while (_enumerator.MoveNext()) {
                //_string += _enumerator.Key + " ";
                BaseComponent component = _enumerator.Value as BaseComponent;
                if (component != null) {
                    component.Command = command;
                }

            }

        }

        #endregion

        /// <summary>
        /// Validates the environment.
        /// </summary>
        /// <returns></returns>
        private bool ValidateEnvironment() {
            // Retrieve installation path
            String installedPath = string.Empty;
            InstallationBase starcounterInstallation = this.GetComponent(InstallationBase.Identifier) as InstallationBase;
            if (starcounterInstallation != null) {
                installedPath = starcounterInstallation.Path;
            }

            // Checking if something is installed.
            Boolean somethingIsInstalled = this.HasCurrentInstalledComponents();

            if (somethingIsInstalled) {
                if (String.IsNullOrEmpty(installedPath)) {
                    // Corrupted Starcounter installation.
                    String foundComponents = Environment.NewLine;

                    if (InstalledComponents[(int)ComponentsCheck.Components.InstallationBase])
                        foundComponents += " - Starcounter Installation Base.";

                    if (InstalledComponents[(int)ComponentsCheck.Components.PersonalServer])
                        foundComponents += " - Personal Server Installation." + Environment.NewLine;

                    if (InstalledComponents[(int)ComponentsCheck.Components.SystemServer])
                        foundComponents += " - System Server Installation." + Environment.NewLine;

                    if (InstalledComponents[(int)ComponentsCheck.Components.VS2012Integration])
                        foundComponents += " - Visual Studio 2012 Integration.";

                    if (InstalledComponents[(int)ComponentsCheck.Components.VS2013Integration])
                        foundComponents += " - Visual Studio 2013 Integration.";

                    if (InstalledComponents[(int)ComponentsCheck.Components.VS2015Integration])
                        foundComponents += " - Visual Studio 2015 Integration.";

                    WpfMessageBoxResult result = WpfMessageBox.Show("Starcounter installation seems corrupted." + Environment.NewLine + "Footprints of the following components are found:" + Environment.NewLine +
                        foundComponents +
                        Environment.NewLine + "Would you like to run Starcounter cleanup process?" +
                        Environment.NewLine + "(All Starcounter footprints will be removed from your system).", "Starcounter installation is corrupted.", WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question, WpfMessageBoxResult.No);

                    if (result == WpfMessageBoxResult.Yes) {
                        // Showing uninstall progress page.
                        InstallerMain.StarcounterSetup(new String[] { "--cleanup" }, null, null, null, null);

                        // Calling installation folder removal function.
                        UninstallEngine.DeleteInstallationDir(false);
                    }

                    // Environment validation failed.
                    return false;
                }

                //this.Configuration.InstallationPath = installedPath;
                //this.RegisterAddRemoveUninstallQuestionPages();
            }

            return true;
        }

        #region Page Handling

        /// <summary>
        /// Registers the page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        private BasePage RegisterPage(BasePage page) {
            page.Resources.MergedDictionaries.Add(this.Resources);
            page.DataContext = this.Configuration;
            this.Pages.Add(page);
            return page;
        }

        /// <summary>
        /// Called when [error].
        /// </summary>
        /// <param name="exc">The exc.</param>
        public void OnError(Exception exc) {
            NavigationCommands.GoToPage.Execute(exc, this);
            CommandManager.InvalidateRequerySuggested();
        }



        /// <summary>
        /// Add, Remove or Uninstall Question question.
        /// </summary>
        private void RegisterAddRemoveUninstallQuestionPages() {
            this.RegisterPage(new AddRemoveUninstallQuestionPage());
        }

        /// <summary>
        /// Registers the first installation pages.
        /// </summary>
        private void RegisterFirstInstallationPages() {


            // Build Pages
            BaseComponent starcounterInstallation = this.Configuration.Components[InstallationBase.Identifier] as BaseComponent;
            if (this.ChangeAdditionalSettings || (starcounterInstallation != null && starcounterInstallation.ValidateSettings() == false)) {
                this.RegisterPage(new InstallationPathPage());
            }

            BaseComponent personalServer = this.Configuration.Components[PersonalServer.Identifier] as BaseComponent;
            if (this.ChangeAdditionalSettings || (personalServer != null && personalServer.ValidateSettings() == false)) {
                this.RegisterPage(new DatabaseEnginesPage());
            }

            BaseComponent vs2012 = this.Configuration.Components[VisualStudio2012Integration.Identifier] as BaseComponent;
            BaseComponent vs2013 = this.Configuration.Components[VisualStudio2013Integration.Identifier] as BaseComponent;
            BaseComponent vs2015 = this.Configuration.Components[VisualStudio2015Integration.Identifier] as BaseComponent;

            if (this.ChangeAdditionalSettings ||
                ((vs2012 != null && vs2012.ValidateSettings() == false) ||
                (vs2013 != null && vs2013.ValidateSettings() == false) ||
                (vs2015 != null && vs2015.ValidateSettings() == false))) {
                this.RegisterPage(new DeveloperToolsPage());
            }

            this.RegisterPage(new InstallProgressPage());
            this.RegisterPage(new InstallFinishedPage());
        }

        /// <summary>
        /// Registers the uninstall pages.
        /// </summary>
        private void RegisterUninstallPages() {

            if (!this.Configuration.Unattended) {
                this.RegisterPage(new UninstallPage());
            }

            this.RegisterPage(new UninstallProgressPage());
            this.RegisterPage(new UninstallFinishedPage());
        }

        /// <summary>
        /// Registers the remove components pages.
        /// </summary>
        private void RegisterRemoveComponentsPages() {
            this.RegisterPage(new DatabaseEnginesPage());
            //this.RegisterPage(new AdministrationToolsPage());
            //this.RegisterPage(new ConnectivityPage());
            this.RegisterPage(new DeveloperToolsPage());

            this.RegisterPage(new RemoveComponentsProgressPage());
            this.RegisterPage(new RemoveComponentsFinishedPage());
        }

        /// <summary>
        /// Registers the add components pages.
        /// </summary>
        private void RegisterAddComponentsPages() {
            //this.RegisterPage(new LicenseAgreementPage());
            this.RegisterPage(new DatabaseEnginesPage());
            //this.RegisterPage(new AdministrationToolsPage());
            //this.RegisterPage(new ConnectivityPage());
            this.RegisterPage(new DeveloperToolsPage());

            this.RegisterPage(new AddComponentsProgressPage());

            this.RegisterPage(new AddComponentsFinishedPage());

        }

        /// <summary>
        /// Handles the SelectionChanged event of the pages_lb control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void pages_lb_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            e.Handled = true;
            Selector selector = sender as Selector;

            // OnDeselected
            if (e.RemovedItems != null) {
                foreach (BasePage page in e.RemovedItems) {
                    page.OnDeselected();
                }
            }

            // OnSelected
            if (e.AddedItems != null) {
                foreach (BasePage page in e.AddedItems) {


                    if (page is IFinishedPage && this.Configuration.Unattended) {
                        MainWindow.StartRoutedCommand.Execute(null, this);
                        CommandManager.InvalidateRequerySuggested();
                    }
                    else {
                        page.OnSelected();
                    }
                }
            }

            Dispatcher _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

            _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(delegate { this.DoMarker(); }));

            this.OnPropertyChanged("NextButtonTitle");
        }


        private void DoMarker() {

            this.marker.Points.Clear();
            //this.marker2.Points.Clear();

            if (this.pages_lb.SelectedItem == null) {
                return;
            }


            DependencyObject dObj = this.pages_lb.ItemContainerGenerator.ContainerFromItem(this.pages_lb.SelectedItem);

            ((FrameworkElement)dObj).SizeChanged -= MainWindow_SizeChanged;
            ((FrameworkElement)dObj).SizeChanged += MainWindow_SizeChanged;

            Point refPoint = new Point(0, 0);


            Point point = ((UIElement)dObj).TranslatePoint(refPoint, this.grid_leftpanel);
            //this.pages_lb.SelectedItem

            double yPos = point.Y + 13;// (((UIElement)dObj).RenderSize.Height / 2);
            double width = 205; // this.grid_leftpanel.ActualWidth - 0;

            this.marker.Points.Add(new Point(0, 0));    // 1
            this.marker.Points.Add(new Point(width, 0));   // 2

            this.marker.Points.Add(new Point(width, yPos - 13));    // 3
            this.marker.Points.Add(new Point(width + 10, yPos));    // 4
            this.marker.Points.Add(new Point(width, yPos + 13));    // 5


            this.marker.Points.Add(new Point(width, grid_leftpanel.ActualHeight)); // 6
            this.marker.Points.Add(new Point(0, grid_leftpanel.ActualHeight));  // 7


            //this.marker2.Points.Add(new Point(width, 0));    // 1
            //this.marker2.Points.Add(new Point(width+10, 0));   // 2

            //this.marker2.Points.Add(new Point(width+10, grid_leftpanel.ActualHeight));    // 3
            //this.marker2.Points.Add(new Point(width, grid_leftpanel.ActualHeight));    // 4

            //this.marker2.Points.Add(new Point(width, yPos + 13));    // 5
            //this.marker2.Points.Add(new Point(width + 10, yPos));    // 6
            //this.marker2.Points.Add(new Point(width, yPos - 13));    // 7


        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
            this.DoMarker();
        }

        #endregion

        private void ChooseFolderDialog(TextBox textBox, string title) {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (!string.IsNullOrEmpty(title)) {
                folderBrowserDialog.Description = title;
            }
            else {
                folderBrowserDialog.Description = "Select path";
            }


            if (Directory.Exists(textBox.Text)) {
                folderBrowserDialog.SelectedPath = textBox.Text;
            }
            else {
                folderBrowserDialog.SelectedPath = Environment.CurrentDirectory;
            }

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                textBox.Text = folderBrowserDialog.SelectedPath;
                textBox.Focus();
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        /// <summary>
        /// Check if a directory contains files (recursive)
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns></returns>
        public static bool DirectoryContainsFiles(string targetDirectory, bool recursive) {

            try {
                if (!Directory.Exists(targetDirectory)) {
                    return false;
                }
            }
            catch {
                return false;
            }

            string[] fileEntries = Directory.GetFiles(targetDirectory);

            if (fileEntries.Length > 0) {
                return true;
            }

            if (!recursive) {
                return false;
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries) {
                if (DirectoryContainsFiles(subdirectory, true)) {
                    return true;
                }
            }

            return false;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }
        #endregion

    }


    public class ValueToIsIndeterminate : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (((int)value) == 0) {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }

}
