﻿
using Starcounter.Internal;
using Starcounter.Tools.Service;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Starcounter.Tools {

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Found ideas from:
    /// http://mitchelsellers.com/blogs/2006/10/28/lightweight-system-tray-application-(notifyicon-based).aspx
    /// </remarks>
    public class TrayIconApp : ApplicationContext {

        #region Private Members
        private System.ComponentModel.IContainer applicationContainer;
        private NotifyIcon mNotifyIcon;
        private ContextMenuStrip mContextMenu;
        private ToolStripMenuItem mShowNotifications;
        private ToolStripMenuItem mDisplayForm;
        private ToolStripMenuItem mShutDown;
        private ToolStripMenuItem mStartServer;
        private ToolStripMenuItem mExitApplication;
        private Icon Connected;
        private Icon Disconnected;

        private ushort Port;
        private string IPAddress;

        private StarcounterWatcher service;

        // True if this program was started from the StartUp Folder
        private bool AutoStarted;

        static Mutex mutex = new Mutex(true, "b2d2e3ea-94c9-4252-b721-1de76234b700");

        #endregion


        /// <summary>
        /// The parameter '-autostarted' is set on the shortcut that will auto start
        /// this program from the windows StartUp folder. This is done so we can 
        /// detect if it was auto started or started in another way (from scservice,manually)
        /// </summary>
        [STAThread]
        public static void Main(string[] args) {

            // Assure single instance
            if (mutex.WaitOne(TimeSpan.Zero, true)) {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                TrayIconApp oContext = new TrayIconApp();
                oContext.AutoStarted = (args.Length > 0 && args[0] == "-autostarted");
                oContext.Setup();

                Application.Run(oContext);
                mutex.ReleaseMutex();
            }
            else {
                // An instance of scTrayIcon is already running.
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public TrayIconApp() {

            Application.ThreadException += Application_ThreadException;
            Application.ApplicationExit += Application_ApplicationExit;

            //Instantiate the component Module to hold everything    
            this.applicationContainer = new System.ComponentModel.Container();

        }


        /// <summary>
        /// Setup 
        /// </summary>
        private void Setup() {

            // Setup icons
            this.SetupIcons();

            // Create System tray 
            this.CreateSystemTray();

            // Setup endpoint (read starcounter configuration files)
            this.SetupEndPoint();

            // Setup and start polling service
            this.service = new StarcounterWatcher();
            this.service.StatusChanged += OnServiceStatusChanged;
            this.service.ExecutablesStarted += OnExecutablesStarted;

            this.service.Error += OnServiceError;
            this.service.Start(this.IPAddress, this.Port);
        }


        /// <summary>
        /// 
        /// </summary>
        private void SetupEndPoint() {

            ushort port;
            string errorMessage;
            bool result = Utils.GetPort(out port, out errorMessage);
            if (result == false) {
                //MessageBox.Show(errorMessage, "Starcounter scTrayIcon Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.ShowError("Starcounter Error", errorMessage);

                this.Port = Starcounter.Internal.StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
            }
            else {
                this.Port = port;
            }

            this.IPAddress = "127.0.0.1";

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnServiceError(object sender, ErrorEventArgs e) {

            if (e.HasError) {
                this.ShowError("Starcounter service error", e.ErrorMessage);
            }

        }

        /// <summary>
        /// Show error message as an BallonTip
        /// and log error to EventViewer
        /// </summary>
        /// <param name="header"></param>
        /// <param name="content"></param>
        private void ShowError(string header, string content) {
            if (this.mNotifyIcon != null) {
                this.mNotifyIcon.ShowBalloonTip(0, header, content, ToolTipIcon.Error);
            }
            else {
                Console.WriteLine("ERROR: {0}, {1}", header, content);
            }

            this.WriteLog(header + Environment.NewLine + content);

        }


        /// <summary>
        /// Write log to Event viewer
        /// </summary>
        /// <param name="message"></param>
        private void WriteLog(string message) {

            string sSource = "Starcounter TrayIcon";
            string sLog = "Application";

            if (!EventLog.SourceExists(sSource)) {
                EventLog.CreateEventSource(sSource, sLog);
            }

            EventLog.WriteEntry(sSource, message, EventLogEntryType.Error);

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnServiceStatusChanged(object sender, StatusEventArgs e) {

            // Exit program if it was auto started (from StartUp folder) and if
            // scservice is not running
            if (this.AutoStarted && e.Connected == false) {
                ExitThreadCore();
                return;
            }

            this.SetStatus(e);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnExecutablesStarted(object sender, ExecutableMessageEventArgs e) {

            if (this.mShowNotifications != null && this.mShowNotifications.Checked) {
                this.mNotifyIcon.ShowBalloonTip(3000, e.Header, e.Content, ToolTipIcon.Info);
            }

        }


        /// <summary>
        /// 
        /// </summary>
        private void CreateSystemTray() {

            mNotifyIcon = new NotifyIcon(this.applicationContainer);
            mNotifyIcon.Icon = this.Disconnected;

            mNotifyIcon.Text = "Starcounter " + CurrentVersion.Version + Environment.NewLine;
            mNotifyIcon.Text += "Connecting...";

            mNotifyIcon.Visible = true;

            //mNotifyIcon.MouseClick += mNotifyIcon_MouseClick;
            mNotifyIcon.MouseDoubleClick += mNotifyIcon_MouseDoubleClick;

            //Instantiate the context menu and items   
            mContextMenu = new ContextMenuStrip();

            //Attach the menu to the notify icon    
            mNotifyIcon.ContextMenuStrip = mContextMenu;

            // Administrator
            mDisplayForm = new ToolStripMenuItem();
            mDisplayForm.Text = "Administrator";
            mDisplayForm.Image = this.Connected.ToBitmap();
            mDisplayForm.Click += new EventHandler(mDisplayForm_Click);
            mDisplayForm.Enabled = false;
            mContextMenu.Items.Add(mDisplayForm);

            // Devider
            mContextMenu.Items.Add("-");

            // Shutdown
            mStartServer = new ToolStripMenuItem();
            mStartServer.Text = "Start Server";
            mStartServer.Click += mStartServer_Click;
            mStartServer.Enabled = false;

            mContextMenu.Items.Add(mStartServer);

            // Shutdown
            mShutDown = new ToolStripMenuItem();
            mShutDown.Text = "Stop Server";
            mShutDown.Click += new EventHandler(mShutDown_Click);
            mShutDown.Enabled = false;
            mContextMenu.Items.Add(mShutDown);

            // Devider
            mContextMenu.Items.Add("-");

            // Display notifications
            mShowNotifications = new ToolStripMenuItem();
            mShowNotifications.Text = "Display Notifications";
            mShowNotifications.CheckOnClick = true;
            mShowNotifications.Checked = Properties.Settings.Default.ShowNotifications;
            mShowNotifications.CheckedChanged += mShowNotifications_CheckedChanged;
            mShowNotifications.Enabled = true;
            mContextMenu.Items.Add(mShowNotifications);

            // Devider
            mContextMenu.Items.Add("-");

            // Exit Trayicon program
            mExitApplication = new ToolStripMenuItem();
            mExitApplication.Text = "Exit";
            mExitApplication.Click += new EventHandler(mExitApplication_Click);
            mContextMenu.Items.Add(mExitApplication);
        }


        /// <summary>
        /// Show Notifications property changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mShowNotifications_CheckedChanged(object sender, EventArgs e) {

            // Save user settings
            Properties.Settings.Default.ShowNotifications = mShowNotifications.Checked;
            Properties.Settings.Default.Save();

        }


        /// <summary>
        /// Setup icons
        /// </summary>
        private void SetupIcons() {

            // http://stackoverflow.com/questions/2338518/distorted-system-tray-icons
            // http://mytechsolutions.blogspot.se/2008/04/icon-size-in-notifyicon.html
            // Windows throws out all the even-numbered rows and columns
            // http://www.hhhh.org/cloister/csharp/icons/

            // Setup icons
            this.Connected = new Icon(this.GetType(), "sc.ico");
            this.Disconnected = new Icon(this.GetType(), "sc_grayscale.ico");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="statusArgs"></param>
        private void SetStatus(StatusEventArgs statusArgs) {

            // If it has been disposed 
            if (mNotifyIcon == null) return;

            mNotifyIcon.Text = "Starcounter " + CurrentVersion.Version + Environment.NewLine;

            this.mDisplayForm.Enabled = statusArgs.Connected;

            this.mStartServer.Enabled = !statusArgs.Connected;
            this.mShutDown.Enabled = statusArgs.Connected;

            if (statusArgs.Connected) {
                mNotifyIcon.Icon = this.Connected;

                if (statusArgs.InteractiveMode) {
                    mNotifyIcon.Text += "Running (developer mode)";
                }
                else {
                    mNotifyIcon.Text += "Running";
                }
            }
            else {
                mNotifyIcon.Icon = this.Disconnected;
                mNotifyIcon.Text += "Not running";
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Application_ApplicationExit(object sender, EventArgs e) {
            CleanUpResources();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) {
            CleanUpResources();
        }


        /// <summary>
        /// 
        /// </summary>
        private void CleanUpResources() {

            if (this.service != null) {
                this.service.Stop();
            }

            if (mNotifyIcon != null) {
                mNotifyIcon.Dispose();
            }
        }


        /// <summary>
        /// Open starcounter Administrator in browser
        /// </summary>
        private void OpenStarcounterAdministrator() {

            string parameter = string.Format("http://{0}:{1}", this.IPAddress, this.Port);

            try {
                Process.Start(new ProcessStartInfo(parameter));
            }
            catch (Win32Exception) {

                try {
                    Process.Start(new ProcessStartInfo("explorer.exe", parameter));
                }
                catch (Win32Exception ee) {
                    string message = "Can not open external browser." + Environment.NewLine + ee.Message + Environment.NewLine + parameter;
                    this.ShowError("Starcounter TrayIcon  error", message);
                }

            }
        }


        #region UserInteraction


        /// <summary>
        /// User choosed the 'Exit' menu choice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mExitApplication_Click(object sender, EventArgs e) {
            // Exit
            ExitThreadCore();
        }


        /// <summary>
        /// Start Starcounter server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mStartServer_Click(object sender, EventArgs e) {

            try {
                mNotifyIcon.Text = "Starcounter " + CurrentVersion.Version + Environment.NewLine;
                mNotifyIcon.Text += "Starting...";
                StarcounterWatcher.StartStarcounterService();
            }
            catch (Exception ee) {
                this.ShowError("Starcounter Server Error", ee.Message);
            }
        }


        /// <summary>
        /// Stop starcounter server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mShutDown_Click(object sender, EventArgs e) {
            mNotifyIcon.Text = "Starcounter " + CurrentVersion.Version + Environment.NewLine;
            mNotifyIcon.Text += "Stopping...";
            this.service.Shutdown();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) {

            if (mDisplayForm.Enabled) {
                this.OpenStarcounterAdministrator();
            }

        }


        /// <summary>
        /// User choosed the 'Show Administrator' menu choice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mDisplayForm_Click(object sender, EventArgs e) {

            this.OpenStarcounterAdministrator();

        }

        #endregion

    }
}
