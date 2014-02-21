using Codeplex.Data;
using Starcounter.Advanced;
using Starcounter.Server;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Server.Service;
using Starcounter.Tools.Service.Task;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Starcounter.Tools.Service {


    /// <summary>
    /// Starcounter Service Watcher
    /// 
    /// Checks Starcounter running mode and executables changes
    /// </summary>
    public class StarcounterWatcher {

        #region Properties

        /// <summary>
        /// Starcounter Service port
        /// </summary>
        public ushort Port {
            get;
            private set;
        }


        /// <summary>
        /// Starcounter Service IP address
        /// </summary>
        public string IPAddress {
            get;
            private set;
        }


        /// <summary>
        /// Poll delay in ms
        /// </summary>
        public int PollDelay {
            get;
            private set;
        }


        /// <summary>
        /// Starcounter Service online status
        /// </summary>
        public bool IsStarcounterServiceOnline {
            get {
                return ServerServiceProcess.IsOnline();
            }
        }


        /// <summary>
        /// Background worker
        /// </summary>
        private BackgroundWorker BackgroundWorker;


        /// <summary>
        /// Last Started Executable
        /// </summary>
        private DateTime LatestExectuableStarted = DateTime.Now;

        #endregion

        #region Event

        /// <summary>
        /// Status changed event handler
        /// Starcounter Service Online/Offline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void StatusChangedEventHandler(object sender, StatusEventArgs e);


        /// <summary>
        /// Status changed event
        /// Starcounter Service Online/Offline
        /// </summary>
        public event StatusChangedEventHandler StatusChanged;


        /// <summary>
        /// Executable started event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ExecutablesStartedEventHandler(object sender, ExecutableMessageEventArgs e);


        /// <summary>
        /// Executable started event
        /// </summary>
        public event ExecutablesStartedEventHandler ExecutablesStarted;


        /// <summary>
        /// Error event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);


        /// <summary>
        /// Error event
        /// </summary>
        public event ErrorEventHandler Error;

        #endregion


        /// <summary>
        /// Start Starcounter Service
        /// </summary>
        /// <param name="serviceName"></param>
        public static void StartStarcounterService(string serviceName = ServerService.Name) {

            if (!ServerServiceProcess.IsOnline()) {
                ServerServiceProcess.StartInteractiveOnDemand();
            }

        }


        /// <summary>
        /// Start worker thread that polls the status and the executable list
        /// </summary>
        public void Start(string ipAddress, ushort port) {

            this.Port = port;
            this.IPAddress = ipAddress;
            this.PollDelay = 2000;  // Delay 2sec between polls

            this.BackgroundWorker = new BackgroundWorker();
            this.BackgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorkerThread);
            this.BackgroundWorker.WorkerReportsProgress = true;
            this.BackgroundWorker.WorkerSupportsCancellation = true;
            this.BackgroundWorker.ProgressChanged += OnBackgroundWorkerProgressChanged;
            this.BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnBackgroundWorkCompleted);
            this.BackgroundWorker.RunWorkerAsync();

            // Send current status
            StatusEventArgs statusArgs = new StatusEventArgs();
            statusArgs.Connected = ServerServiceProcess.IsOnline();
            OnStatusChanged(statusArgs);
        }


        /// <summary>
        /// Stop worker thread that polls the status
        /// </summary>
        public void Stop() {

            if (this.BackgroundWorker != null && this.BackgroundWorker.CancellationPending == false) {
                this.BackgroundWorker.CancelAsync();
            }

        }


        #region BackgroundWorker


        /// <summary>
        /// Retrive the Starcounter Service Status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerThread(object sender, DoWorkEventArgs e) {

            BackgroundWorker worker = sender as BackgroundWorker;

            // First we poll the onlone status, when status is online we poll
            // for Executables.
            bool modeFlag = true; // True = Polling status, False = Polling executables

            while (true) {

                if (worker.CancellationPending == true) {
                    e.Cancel = true;
                    break;
                }

                if (modeFlag) {
                    StatusEventArgs statusEventArgs;
                    StatusTask.Execute(this, out statusEventArgs);

                    worker.ReportProgress(0, statusEventArgs);

                    if (statusEventArgs.Connected) {
                        // Switch to polling Executables
                        modeFlag = false;
                        continue;
                    }
                }
                else {

                    try {
                        ExecutablesEventArgs executablesArgs;
                        ExecutablesTask.Execute(this, out executablesArgs);
                        worker.ReportProgress(0, executablesArgs);
                    }
                    catch (TaskCanceledException) {
                        // Switch to polling the service status
                        modeFlag = true;
                        continue;
                    }
                }

                Thread.Sleep(this.PollDelay); // Pause between status calls
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e) {

            if (e.UserState is StatusEventArgs) {
                // Status changed
                OnStatusChanged(e.UserState as StatusEventArgs);
            }
            else if (e.UserState is ExecutablesEventArgs) {
                // Got Executable list
                OnExecutablesList(e.UserState as ExecutablesEventArgs);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackgroundWorkCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if ((e.Cancelled == true)) {
                // Canceled
            }
            else if (!(e.Error == null)) {
                // Error
                OnError(new ErrorEventArgs() { ErrorMessage = e.Error.Message });
            }
            else {

                if (e.Result is StatusEventArgs) {
                    OnStatusChanged(e.Result as StatusEventArgs);
                }
                else if (e.Result is ExecutablesEventArgs) {
                    OnExecutablesList(e.Result as ExecutablesEventArgs);
                }
            }
        }


        /// <summary>
        /// Starcounter service status changed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnStatusChanged(StatusEventArgs e) {
            if (StatusChanged != null) {
                StatusChanged(this, e);
            }
        }


        /// <summary>
        /// Got a list of running Executables
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnExecutablesList(ExecutablesEventArgs e) {

            this.InvokeEventForStartedExecutables(e.Executables.Items);

        }


        /// <summary>
        /// Invoke an event for newly started executables
        /// </summary>
        /// <param name="executables"></param>
        private void InvokeEventForStartedExecutables(Arr<Executables.ItemsElementJson> executables) {

            DateTime lastCheck = DateTime.MinValue;

            foreach (Executables.ItemsElementJson executable in executables) {

                if (!string.IsNullOrEmpty(executable.RuntimeInfo.LastRestart) || string.IsNullOrEmpty(executable.RuntimeInfo.Started)) {
                    continue;
                }

                DateTime started = DateTime.Parse(executable.RuntimeInfo.Started);

                if (started > this.LatestExectuableStarted) {

                    if (started > lastCheck) {
                        lastCheck = started;
                    }

                    if (ExecutablesStarted != null) {
                        // Create message and invoke event
                        ExecutableMessageEventArgs message = new ExecutableMessageEventArgs();
                        message.Header = "Starcounter Executable Started";
                        // TODO: Add the port(s) that the executable is listening to.
                        message.Content = string.Format("Your starcounter application {0} is now running", executable.Name); ;
                        ExecutablesStarted(this, message);
                    }
                }

            }

            if (lastCheck != DateTime.MinValue) {
                this.LatestExectuableStarted = lastCheck;
            }
        }


        #endregion


        #region Shutdown Task


        /// <summary>
        /// Shutdown Starcounter Service
        /// </summary>
        public void Shutdown() {

            BackgroundWorker bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(ShutdownBackgroundWorkerWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnShutdownBackgroundWorkerCompleted);
            bgWorker.RunWorkerAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShutdownBackgroundWorkerWork(object sender, DoWorkEventArgs e) {
            ShutdownTask.Execute(this);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnShutdownBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

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
        /// Invoke Errors to listeners
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(ErrorEventArgs e) {

            if (Error != null) {
                Error(this, e);
            }

        }
    }
}
