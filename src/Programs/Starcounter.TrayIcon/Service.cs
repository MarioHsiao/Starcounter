using Codeplex.Data;
using Starcounter.Advanced;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 
        /// </summary>
        public void Start(ushort port) {

            this.Port = port;

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 2000; // Poll intervall 
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, ElapsedEventArgs e) {

            System.Timers.Timer timer = sender as System.Timers.Timer;
            timer.Stop();

            string url = string.Format("{0}:{1}{2}", "127.0.0.1", this.Port, "/api/server");

            X.GET(url, null, null, (Response respAsync, Object userObject) => {

                timer.Enabled = true;

                if (respAsync.IsSuccessStatusCode) {
                    bool isService = false;

                    try {
                        dynamic incomingJson = DynamicJson.Parse(respAsync.Body);
                        bool bValid = incomingJson.IsDefined("Context");
                        if (bValid) {
                            string context = incomingJson.Context;
                            isService = !context.Contains('@');
                        }
                    }
                    catch (Exception) {
                        // TODO: Log?
                    }

                    OnChanged(new StatusEventArgs() { Connected = true, IsService = isService });
                }
                else {
                    OnChanged(new StatusEventArgs() { Connected = false });
                }

            }, 10000); // 10 Sec timeout
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
