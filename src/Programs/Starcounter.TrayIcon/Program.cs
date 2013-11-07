
using System;
using System.Diagnostics;
using System.Drawing;
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
        private System.ComponentModel.IContainer mComponents;
        private NotifyIcon mNotifyIcon;
        private ContextMenuStrip mContextMenu;
        private ToolStripMenuItem mDisplayForm;
        private ToolStripMenuItem mExitApplication;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        [STAThread]
        public static void Main() {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            TrayIconApp oContext = new TrayIconApp();
            Application.Run(oContext);
        }

        /// <summary>
        /// 
        /// </summary>
        public TrayIconApp() {     

            //Instantiate the component Module to hold everything    
            mComponents = new System.ComponentModel.Container();

            //Instantiate the NotifyIcon attaching it to the components container and    
            //provide it an icon, note, you can imbed this resource   
            mNotifyIcon = new NotifyIcon(this.mComponents);
            mNotifyIcon.Icon = new Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Starcounter.Tools.sc.ico"));
            mNotifyIcon.Text = "Starcounter";
            mNotifyIcon.Visible = true;

            //Instantiate the context menu and items   
            mContextMenu = new ContextMenuStrip();
            mDisplayForm = new ToolStripMenuItem();
            mExitApplication = new ToolStripMenuItem();

            //Attach the menu to the notify icon    
            mNotifyIcon.ContextMenuStrip = mContextMenu;

            //Setup the items and add them to the menu strip, adding handlers to be created later   
            mDisplayForm.Text = "Show Administrator";
            mDisplayForm.Click += new EventHandler(mDisplayForm_Click);
            mContextMenu.Items.Add(mDisplayForm);
            mContextMenu.Items.Add("-");
            mExitApplication.Text = "Exit";
            mExitApplication.Click += new EventHandler(mExitApplication_Click);
            mContextMenu.Items.Add(mExitApplication);
        }


        /// <summary>
        /// User choosed the 'Exit' menu choice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mExitApplication_Click(object sender, EventArgs e) {

            // Exit
            ExitThreadCore();
        }

         
        /// <summary>
        /// User choosed the 'Show Administrator' menu choice
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mDisplayForm_Click(object sender, EventArgs e) {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo("http://localhost:8181");
            p.Start();
        }

    }


}
