﻿using Codeplex.Data;
using Starcounter.Advanced;
using Starcounter.Tools.Service.Task;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace Starcounter.Tools.Service {


    /// <summary>
    /// 
    /// </summary>
    public class StarcounterService {

        #region Properties

        /// <summary>
        /// Service port
        /// </summary>
        public ushort Port {
            get;
            private set;
        }

        /// <summary>
        /// Service IP address
        /// </summary>
        public string IPAddress {
            get;
            private set;
        }

        private BackgroundWorker StatusBackgroundWorker;

        #endregion

        #region Event
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void StatusChangedEventHandler(object sender, StatusEventArgs e);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);


        /// <summary>
        /// 
        /// </summary>
        public event StatusChangedEventHandler StatusChanged;

        /// <summary>
        /// 
        /// </summary>
        public event ErrorEventHandler Error;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void Start(string ipAddress, ushort port) {

            this.Port = port;
            this.IPAddress = ipAddress;

            this.StatusBackgroundWorker = new BackgroundWorker();
            this.StatusBackgroundWorker.DoWork += new DoWorkEventHandler(StatusBackgroundWork);
            this.StatusBackgroundWorker.WorkerReportsProgress = true;
            this.StatusBackgroundWorker.WorkerSupportsCancellation = true;
            this.StatusBackgroundWorker.ProgressChanged += StatusBackgroundWorker_ProgressChanged;
            this.StatusBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(StatusBackgroundWorkCompleted);
            this.StatusBackgroundWorker.RunWorkerAsync();
        }

        private void StatusBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            OnChanged(e.UserState as StatusEventArgs);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop() {

            if (this.StatusBackgroundWorker != null && this.StatusBackgroundWorker.CancellationPending == false) {
                this.StatusBackgroundWorker.CancelAsync();
            }

        }

        #region Status

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusBackgroundWork(object sender, DoWorkEventArgs e) {

            BackgroundWorker worker = sender as BackgroundWorker;

            while (true) {

                if (worker.CancellationPending == true) {
                    e.Cancel = true;
                    break;
                }

                StatusEventArgs status;
                StatusTask.Execute(this, out status);

                worker.ReportProgress(0, status);

                Thread.Sleep(2000); // Pause between status calls
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusBackgroundWorkCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if ((e.Cancelled == true)) {
                // Canceled
            }
            else if (!(e.Error == null)) {
                // Error
                OnError(new ErrorEventArgs() { ErrorMessage = e.Error.Message });
            }
            else {
                OnChanged(e.Result as StatusEventArgs);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnChanged(StatusEventArgs e) {
            if (StatusChanged != null) {
                StatusChanged(this, e);
            }
        }


        #endregion

        #region Shutdown

        /// <summary>
        /// 
        /// </summary>
        public void Shutdown() {

            BackgroundWorker bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(ShutdownBackgroundWorkerWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ShutdownBackgroundWorkerCompleted);
            bgWorker.RunWorkerAsync();
        }

        private void ShutdownBackgroundWorkerWork(object sender, DoWorkEventArgs e) {
            ShutdownTask.Execute(this);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShutdownBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if ((e.Cancelled == true)) {
                // Canceled
            }
            else if (!(e.Error == null)) {
                // Error
                OnError(new ErrorEventArgs() { ErrorMessage = e.Error.Message });
            }
            else {
                // Done
            }

        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(ErrorEventArgs e) {
            if (Error != null) {
                Error(this, e);
            }
        }

    }


}
