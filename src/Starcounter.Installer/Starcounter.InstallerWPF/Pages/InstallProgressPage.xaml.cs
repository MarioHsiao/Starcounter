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
using System.Windows.Threading;
using System.Threading;
using Starcounter.InstallerEngine;
using System.IO;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Collections;
using Starcounter.InstallerWPF.Components;
using Starcounter.Controls;

namespace Starcounter.InstallerWPF.Pages {
    /// <summary>
    /// Interaction logic for ProgressPage.xaml
    /// </summary>
    public partial class InstallProgressPage : BasePage, IProgressPage {

        #region Win32 import

        private const uint SC_CLOSE = 0xF060;
        private const int MF_DISABLED = 0x00000002;
        private const int MF_ENABLED = 0x00000000;

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("User32.Dll")]
        public static extern IntPtr DrawMenuBar(IntPtr hwnd);

        #endregion

        #region Properties

        private bool _CanGoNext = false;
        public override bool CanGoNext {
            get {
                return base.CanGoNext && _CanGoNext;
            }
        }

        private bool _CanGoBack = false;
        public override bool CanGoBack {
            get {
                return base.CanGoBack && _CanGoBack;
            }
        }

        public override bool CanClose {
            get {
                return !this._IsInstalling;
            }

        }

        private bool _IsInstalling = false;
        public bool IsInstalling {
            get {
                return this._IsInstalling;
            }
            set {
                if (this._IsInstalling == value) return;
                this._IsInstalling = value;

                this.OnPropertyChanged("IsInstalling");
                this.OnPropertyChanged("CanClose");

                this.HasProgress = value;

                this.CloseSystemMenuButtonIsEnabled = !this.IsInstalling;

            }
        }
        private Dispatcher _dispatcher;

        private bool _CloseSystemMenuButtonIsEnabled = true;
        private bool CloseSystemMenuButtonIsEnabled {
            get {
                return this._CloseSystemMenuButtonIsEnabled;
            }
            set {
                if (this._CloseSystemMenuButtonIsEnabled == value) return;
                this._CloseSystemMenuButtonIsEnabled = value;

                var hwnd = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                IntPtr menu = GetSystemMenu(hwnd, false);

                if (value) {
                    EnableMenuItem(menu, SC_CLOSE, MF_ENABLED);
                }
                else {
                    EnableMenuItem(menu, SC_CLOSE, MF_DISABLED);
                }

                // Redraw
                DrawMenuBar(hwnd);


                this.OnPropertyChanged("CloseSystemMenuButtonIsEnabled");
            }
        }

        private bool _GoToWiki = true;
        public bool GoToWiki {
            get {
                return _GoToWiki;
            }
            set {
                this._GoToWiki = value;
                this.OnPropertyChanged("GoToWiki");
            }
        }

        #endregion

        public InstallProgressPage() {
            _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

            Selected += new EventHandler(Page4_Selected);
            InitializeComponent();
        }

        void Page4_Selected(object sender, EventArgs e) {
            Start();
        }

        private void Start() {
            // Referring to the user configuration.
            Configuration config = (Configuration)DataContext;

            MainWindow mainwindow = System.Windows.Application.Current.MainWindow as MainWindow;

            this.IsInstalling = true;

            StartAnimation();

            ThreadPool.QueueUserWorkItem(StartInstallationThread, config);
        }


        /// <summary>
        /// Starts the installation thread.
        /// </summary>
        /// <param name="state">The state.</param>
        private void StartInstallationThread(object state) {

            // Starting the installation process.
            try {

                Configuration config = state as Configuration;

                PersonalServer personalServerComponent = config.Components[PersonalServer.Identifier] as PersonalServer;
                VisualStudio2012Integration vs2012IntegrationComponent = config.Components[VisualStudio2012Integration.Identifier] as VisualStudio2012Integration;
                VisualStudio2013Integration vs2013IntegrationComponent = config.Components[VisualStudio2013Integration.Identifier] as VisualStudio2013Integration;
                VisualStudio2015Integration vs2015IntegrationComponent = config.Components[VisualStudio2015Integration.Identifier] as VisualStudio2015Integration;

                // TODO: Send info about VS 2015!

                Starcounter.Tracking.Client.Instance.SendInstallerExecuting(Starcounter.Tracking.Client.InstallationMode.FullInstallation,
                    personalServerComponent != null && personalServerComponent.IsExecuteCommandEnabled && personalServerComponent.ExecuteCommand,
                    vs2012IntegrationComponent != null && vs2012IntegrationComponent.IsExecuteCommandEnabled && vs2012IntegrationComponent.ExecuteCommand,
                    vs2013IntegrationComponent != null && vs2013IntegrationComponent.IsExecuteCommandEnabled && vs2013IntegrationComponent.ExecuteCommand);

                config.ExecuteSettings(
                          delegate (object sender, Utilities.InstallerProgressEventArgs args) {
                              this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                      new Action(delegate {
                                                          this.Progress = args.Progress;
                                                          this.ProgressText = args.Text;
                                                      }
                                                  ));
                          },
                            delegate (object sender, Utilities.MessageBoxEventArgs args) {
                                this._dispatcher.Invoke(new Action(() => {
                                    args.MessageBoxResult = WpfMessageBox.Show(args.MessageBoxText, args.Caption, args.Button, args.Icon, args.DefaultResult);
                                }));

                            }

                          );


                // Installation succeeded.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {

                        Starcounter.Tracking.Client.Instance.SendInstallerFinish(Tracking.Client.InstallationMode.FullInstallation, true);

                        this.OnSuccess();
                    }
                ));
                //((Configuration)state).RunInstallerEngine(ConstantsBank.ScGlobalSettingsIniName, null);
            }
            catch (Exception installException) {
                // Error occurred during installation.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {

                        Starcounter.Tracking.Client.Instance.SendInstallerFinish(Tracking.Client.InstallationMode.FullInstallation, false);

                        this.OnError(installException);
                    }
                ));
            }


        }


        /// <summary>
        /// Called when [success].
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="internalDatabases">The internal databases.</param>
        private void OnSuccess() {

            try {
                this.IsInstalling = false;

                StopAnimation();
                _CanGoNext = true;
                NavigationCommands.NextPage.Execute(null, this);
                CommandManager.InvalidateRequerySuggested();

                this.DisplayName = "Finished";
            }
            catch (Exception e) {
                this.OnError(e);
            }
        }

        /// <summary>
        /// Called when [error].
        /// </summary>
        /// <param name="e">The e.</param>
        private void OnError(Exception e) {

            StopAnimation();

            NavigationCommands.GoToPage.Execute(e, this);
            CommandManager.InvalidateRequerySuggested();

            try {
#if SIMULATE_INSTALLATION
#else
                UninstallEngine.DeleteInstallationDir(false);
#endif
                this.IsInstalling = false;
                this._CanGoNext = true;
                this._CanGoBack = true;
                NavigationCommands.GoToPage.Execute(e, this);
            }
            catch (Exception ee) {
                this.IsInstalling = false;
                this._CanGoNext = true;
                this._CanGoBack = true;
                NavigationCommands.GoToPage.Execute(ee, this);
            }

            CommandManager.InvalidateRequerySuggested();
        }


        /// <summary>
        /// Starts the installation thread.
        /// </summary>
        /// <param name="state">The state.</param>
        private void StartInstallationThread_OLD(object state) {

            Configuration config = state as Configuration;


            // Starting the uninstall.
            try {

                PersonalServer personalServerComponent = config.Components[PersonalServer.Identifier] as PersonalServer;
                VisualStudio2012Integration vs2012IntegrationComponent = config.Components[VisualStudio2012Integration.Identifier] as VisualStudio2012Integration;
                VisualStudio2013Integration vs2013IntegrationComponent = config.Components[VisualStudio2013Integration.Identifier] as VisualStudio2013Integration;
                VisualStudio2015Integration vs2015IntegrationComponent = config.Components[VisualStudio2015Integration.Identifier] as VisualStudio2015Integration;

                // TODO: Send info about VS 2015!

                Starcounter.Tracking.Client.Instance.SendInstallerExecuting(Starcounter.Tracking.Client.InstallationMode.PartialInstallation,
                    personalServerComponent != null && personalServerComponent.IsExecuteCommandEnabled && personalServerComponent.ExecuteCommand,
                    vs2012IntegrationComponent != null && vs2012IntegrationComponent.IsExecuteCommandEnabled && vs2012IntegrationComponent.ExecuteCommand,
                    vs2013IntegrationComponent != null && vs2013IntegrationComponent.IsExecuteCommandEnabled && vs2013IntegrationComponent.ExecuteCommand);


                config.ExecuteSettings(
                           delegate (object sender, Utilities.InstallerProgressEventArgs args) {
                               this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                       new Action(delegate {

                                                           this.Progress = args.Progress;
                                                           this.ProgressText = args.Text;
                                                       }
                                                   ));
                           },
                             delegate (object sender, Utilities.MessageBoxEventArgs args) {
                                 this._dispatcher.Invoke(new Action(() => {
                                     args.MessageBoxResult = WpfMessageBox.Show(args.MessageBoxText, args.Caption, args.Button, args.Icon, args.DefaultResult);
                                 }));

                             }

                           );

                //                ((Configuration) state).RunInstallerEngine(ConstantsBank.ScAddIniName, null);
            }
            catch (Exception installException) {
                // Error occurred during installation.
                _dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate {
                        Starcounter.Tracking.Client.Instance.SendInstallerFinish(Tracking.Client.InstallationMode.PartialInstallation, false);
                        OnError(installException);
                    }
                ));
                return;
            }

            this._dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate {
                Starcounter.Tracking.Client.Instance.SendInstallerFinish(Tracking.Client.InstallationMode.PartialInstallation, true);
                OnSuccess();
            }));
        }

        private void StartAnimation() {
            Storyboard Element_Storyboard = (Storyboard)PART_Canvas.FindResource("canvasAnimation");
            Element_Storyboard.Begin(PART_Canvas, true);
        }

        private void StopAnimation() {
            Storyboard Element_Storyboard = (Storyboard)PART_Canvas.FindResource("canvasAnimation");
            Element_Storyboard.Stop(PART_Canvas);
        }




    }
}
