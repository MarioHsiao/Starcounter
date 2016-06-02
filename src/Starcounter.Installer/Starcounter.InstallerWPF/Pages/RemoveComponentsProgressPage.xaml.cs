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
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using Starcounter.InstallerEngine;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Starcounter.Controls;
using Starcounter.InstallerWPF.Components;

namespace Starcounter.InstallerWPF.Pages
{
    /// <summary>
    /// Interaction logic for RemoveComponentsProgressPage.xaml
    /// </summary>
    public partial class RemoveComponentsProgressPage : BasePage, IProgressPage {

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
        public override bool CanGoNext
        {
            get
            {
                return base.CanGoNext && _CanGoNext;
            }
        }

        private bool _CanGoBack = false;
        public override bool CanGoBack
        {
            get
            {
                return base.CanGoBack && _CanGoBack;
            }
        }

        public override bool CanClose
        {
            get
            {
                return !this._IsInstalling;
            }

        }

        private bool _IsInstalling = false;
        public bool IsInstalling
        {
            get
            {
                return this._IsInstalling;
            }
            set
            {
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
        private bool CloseSystemMenuButtonIsEnabled
        {
            get
            {
                return this._CloseSystemMenuButtonIsEnabled;
            }
            set
            {
                if (this._CloseSystemMenuButtonIsEnabled == value) return;
                this._CloseSystemMenuButtonIsEnabled = value;

                var hwnd = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
                IntPtr menu = GetSystemMenu(hwnd, false);

                if (value)
                {
                    EnableMenuItem(menu, SC_CLOSE, MF_ENABLED);
                }
                else
                {
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

        //#region Properties

        //private bool _CanGoNext = false;
        //public override bool CanGoNext
        //{
        //    get
        //    {
        //        return base.CanGoNext && _CanGoNext;
        //    }
        //}

        //private bool _CanGoBack = false;
        //public override bool CanGoBack
        //{
        //    get
        //    {
        //        return base.CanGoBack && _CanGoBack;
        //    }
        //}


        //public override bool CanClose
        //{
        //    get
        //    {
        //        return false;
        //    }
        //}

        //private Dispatcher _dispatcher;

        //#endregion

        public RemoveComponentsProgressPage()
        {
            this._dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

            this.Selected += new EventHandler(Page4_Selected);
            InitializeComponent();
        }

        void Page4_Selected(object sender, EventArgs e)
        {
            //this.ProgressBar.Value = 0;
            this.Start();
        }

        private void Start()
        {
            // Referring to the user configuration.
            Configuration config = (Configuration)this.DataContext;

            this.IsInstalling = true;

            this.StartAnimation();


            ThreadPool.QueueUserWorkItem(this.StartInstallationThread, config);
        }

        /// <summary>
        /// Starts the installation thread.
        /// </summary>
        /// <param name="state">The state.</param>
        private void StartInstallationThread(object state)
        {
            // Starting the remove components process.
            try
            {
                Configuration config = state as Configuration;

                PersonalServer personalServerComponent = config.Components[PersonalServer.Identifier] as PersonalServer;
                VisualStudio2012Integration vs2012IntegrationComponent = config.Components[VisualStudio2012Integration.Identifier] as VisualStudio2012Integration;
                VisualStudio2013Integration vs2013IntegrationComponent = config.Components[VisualStudio2013Integration.Identifier] as VisualStudio2013Integration;
                VisualStudio2015Integration vs2015IntegrationComponent = config.Components[VisualStudio2015Integration.Identifier] as VisualStudio2015Integration;

                Starcounter.Tracking.Client.Instance.SendInstallerExecuting(Starcounter.Tracking.Client.InstallationMode.PartialUninstallation,
                    personalServerComponent != null && personalServerComponent.IsExecuteCommandEnabled && personalServerComponent.ExecuteCommand,
                    vs2012IntegrationComponent != null && vs2012IntegrationComponent.IsExecuteCommandEnabled && vs2012IntegrationComponent.ExecuteCommand,
                    vs2013IntegrationComponent != null && vs2013IntegrationComponent.IsExecuteCommandEnabled && vs2013IntegrationComponent.ExecuteCommand
                    );


                config.ExecuteSettings(
                           delegate(object sender, Utilities.InstallerProgressEventArgs args)
                           {
                               this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                       new Action(delegate
                                                       {

                                                           this.Progress = args.Progress;
                                                           this.ProgressText = args.Text;
                                                       }
                                                   ));
                           },
                             delegate(object sender, Utilities.MessageBoxEventArgs args)
                             {
                                 this._dispatcher.Invoke(new Action(() =>
                                 {
                                     args.MessageBoxResult = WpfMessageBox.Show(args.MessageBoxText, args.Caption, args.Button, args.Icon, args.DefaultResult);
                                 }));

                             }

                           );

            }
            catch (Exception installException)
            {
                // Error occurred during installation.
                this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(delegate
                    {
                        Starcounter.Tracking.Client.Instance.SendInstallerFinish(Tracking.Client.InstallationMode.PartialUninstallation, false);
                        this.OnError(installException);
                    }
                ));
                return;
            }

            // Installation succeeded.
            this._dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(delegate
                {
                    Starcounter.Tracking.Client.Instance.SendInstallerFinish(Tracking.Client.InstallationMode.PartialUninstallation, true);
                    this.OnSuccess();
                }
            ));
        }

        private void StartAnimation()
        {
            Storyboard Element_Storyboard = this.PART_Canvas.FindResource("canvasAnimation") as Storyboard;
            Element_Storyboard.Begin(this.PART_Canvas, true);
        }

        private void StopAnimation()
        {
            Storyboard Element_Storyboard = this.PART_Canvas.FindResource("canvasAnimation") as Storyboard;
            Element_Storyboard.Stop(this.PART_Canvas);
        }

        /// <summary>
        /// Called when [success].
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="internalDatabases">The internal databases.</param>
        private void OnSuccess()
        {
            this.IsInstalling = false;

            this.StopAnimation();
            this._CanGoNext = true;


            NavigationCommands.NextPage.Execute(null, this);

            //NavigationCommands.GoToPage.Execute(new RemoveComponentsFinishedPage(), this);


            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Called when [error].
        /// </summary>
        /// <param name="e">The e.</param>
        private void OnError(Exception e)
        {
            this.IsInstalling = false;

            this.StopAnimation();
            this._CanGoNext = true;
            this._CanGoBack = true;

            NavigationCommands.GoToPage.Execute(e, this);
            CommandManager.InvalidateRequerySuggested();
        }

    }
}
