using Codeplex.Data;
using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace Starcounter.Tools {


    /// <summary>
    /// 
    /// </summary>
    public class Service {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ChangedEventHandler(object sender, StatusEventArgs e);


        /// <summary>
        /// 
        /// </summary>
        private ushort Port;

        /// <summary>
        /// 
        /// </summary>
        public event ChangedEventHandler Changed;

        private System.Windows.Forms.Timer Timer;

        /// <summary>
        /// Background worker is used to make sure the callback is on the same thread as the GUI
        /// </summary>
        private BackgroundWorker backgroundWorker = new BackgroundWorker();


        /// <summary>
        /// 
        /// </summary>
        public void Start(ushort port) {

            this.Port = port;

            this.Timer = new System.Windows.Forms.Timer();
            this.Timer.Interval = 2000; // Poll intervall 
            this.Timer.Tick += timer_Tick;

            this.backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorkerWork);
            this.backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorkerCompleted);
            this.backgroundWorker.RunWorkerAsync();
   
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e) {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;
            this.Timer.Stop();
            backgroundWorker.RunWorkerAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;

            string url = string.Format("{0}:{1}{2}", "127.0.0.1", this.Port, "/api/server");

            StatusEventArgs status;

            Response response; 
            X.GET(url, null, out response, 10000);

            if (response.IsSuccessStatusCode) {
                bool isService = false;

                try {
                    dynamic incomingJson = DynamicJson.Parse(response.Body);
                    bool bValid = incomingJson.IsDefined("Context");
                    if (bValid) {
                        string context = incomingJson.Context;
                        isService = !context.Contains('@');
                    }
                }
                catch (Exception) {
                    // TODO: Log?
                }

                status = new StatusEventArgs() { Connected = true, IsService = isService };

            }
            else {
                status = new StatusEventArgs() { Connected = false };

            }

            e.Result = status;


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.Timer.Enabled = true;
            OnChanged(e.Result as StatusEventArgs);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();
            this.backgroundWorker.RunWorkerAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnChanged(StatusEventArgs e) {
            if (Changed != null)
                Changed(this, e);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class StatusEventArgs : EventArgs {

        /// <summary>
        /// 
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsService { get; set; }

    }


}
