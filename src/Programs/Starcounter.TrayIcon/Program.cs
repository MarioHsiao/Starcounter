
using Starcounter.Internal;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
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
        private ToolStripMenuItem mDisplayForm;
        private ToolStripMenuItem mExitApplication;
        private Icon Connected;
        private Icon Disconnected;

        private ushort Port;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        [STAThread]
        public static void Main(string[] args) {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            TrayIconApp oContext = new TrayIconApp();
            Application.Run(oContext);
        }

        /// <summary>
        /// 
        /// </summary>
        public TrayIconApp() {

            Application.ThreadException += Application_ThreadException;
            Application.ApplicationExit += Application_ApplicationExit;


            //Instantiate the component Module to hold everything    
            this.applicationContainer = new System.ComponentModel.Container();

            this.SetupPort();

            // Setup icons
            this.SetupIcons();

            // Create System tray 
            this.CreateSystemTray();

            // Setup and start polling service
            Service service = new Service();
            service.Changed += service_Changed;

            service.Start(this.Port);

        }


        /// <summary>
        /// 
        /// </summary>
        private void SetupPort() {

            ushort port;
            string errorMessage;
            bool result = Utils.GetPort(out port, out errorMessage);
            if (result == false) {
                MessageBox.Show(errorMessage, "Starcounter scTrayIcon Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Port = Starcounter.Internal.StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
            }
            else {
                this.Port = port;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void service_Changed(object sender, StatusEventArgs e) {
            this.SetStatus(e);
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateSystemTray() {

            //Instantiate the NotifyIcon attaching it to the components container and    
            //provide it an icon, note, you can imbed this resource   
            mNotifyIcon = new NotifyIcon(this.applicationContainer);
            //mNotifyIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Starcounter.Tools.sc.ico"));
            //mNotifyIcon.Icon = new Icon(this.GetType(), "sc.ico");
            mNotifyIcon.Icon = this.Disconnected;
            mNotifyIcon.Text = "Starcounter" + Environment.NewLine + "Connecting...";
            mNotifyIcon.Visible = true;

            //Instantiate the context menu and items   
            mContextMenu = new ContextMenuStrip();
            mDisplayForm = new ToolStripMenuItem();
            mExitApplication = new ToolStripMenuItem();

            //Attach the menu to the notify icon    
            mNotifyIcon.ContextMenuStrip = mContextMenu;

            //Setup the items and add them to the menu strip, adding handlers to be created later   
            mDisplayForm.Text = "Administrator";
            mDisplayForm.Click += new EventHandler(mDisplayForm_Click);
            mContextMenu.Items.Add(mDisplayForm);
            mContextMenu.Items.Add("-");
            mExitApplication.Text = "Exit";
            mExitApplication.Click += new EventHandler(mExitApplication_Click);
            mContextMenu.Items.Add(mExitApplication);
        }


        /// <summary>
        /// Setup icons
        /// </summary>
        private void SetupIcons() {

            // http://stackoverflow.com/questions/2338518/distorted-system-tray-icons

            // Setup icons
            this.Connected = new Icon(this.GetType(), "sc_logo.ico");
            //this.Connected = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Starcounter.Tools.sc_logo.ico"),16,16);

            Image img = this.Connected.ToBitmap();
            Image geyImage = Utils.MakeGrayscale(img);
            Bitmap bmp = new Bitmap(geyImage);
            this.Disconnected = System.Drawing.Icon.FromHandle(bmp.GetHicon());

            System.Drawing.Image canvas = new Bitmap(this.Connected.Width, this.Connected.Height);
            Graphics artist = Graphics.FromImage(canvas);
            artist.DrawString("H", new Font("Arial", 4), System.Drawing.Brushes.Black, (float)(this.Connected.Width), (float)(this.Connected.Height));
            artist.Save();

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="statusArgs"></param>
        private void SetStatus(StatusEventArgs statusArgs) {

            mNotifyIcon.Text = "Starcounter " + CurrentVersion.Version + Environment.NewLine;

            if (statusArgs.Connected) {
                mNotifyIcon.Icon = this.Connected;

                if (statusArgs.IsService) {
                    mNotifyIcon.Text += "Service running";
                }
                else {
                    mNotifyIcon.Text += "Server running";
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
            mNotifyIcon.Dispose();
        }


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
        /// User choosed the 'Show Administrator' menu choice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mDisplayForm_Click(object sender, EventArgs e) {

            string parameter = string.Format("http://127.0.0.1:{0}", this.Port);

            try {
                Process.Start(new ProcessStartInfo(parameter));
            }
            catch (Win32Exception) {

                try {
                    Process.Start(new ProcessStartInfo("explorer.exe", parameter));
                }
                catch (Win32Exception ee) {
                    string message = "Can not open external browser." + Environment.NewLine + ee.Message + Environment.NewLine + parameter;
                    Console.WriteLine(message);
                }

            }

        }

    }



}
