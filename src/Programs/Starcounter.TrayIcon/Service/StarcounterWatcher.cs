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
        public delegate void ExecutablesStartedEventHandler(object sender, ExecutablesEventArgs e);


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
            statusArgs.Running = ServerServiceProcess.IsOnline();
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

                    // Get starcounter server status
                    StatusEventArgs statusEventArgs;
                    StatusTask.Execute(this, out statusEventArgs);

                    // Report starcounter server status
                    worker.ReportProgress(0, statusEventArgs);

                    if (statusEventArgs.Running) {
                        // Switch to polling Executables
                        modeFlag = false;
                        continue;
                    }
                }
                else {

                    // Starcounter server is running
                    // Get Executables
                    Executables executables;
                    bool result = ExecutablesTask.Execute(this, out executables);
                    if (result) {
                        worker.ReportProgress(0, executables);
                    }
                    else {
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
            else if (e.UserState is Executables) {
                // Got Executable list
                OnExecutablesList(e.UserState as Executables);
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
                else if (e.Result is Executables) {
                    OnExecutablesList(e.Result as Executables);
                }
            }
        }



        /// <summary>
        /// Got a list of running Executables
        /// </summary>
        /// <param name="executables"></param>
        protected virtual void OnExecutablesList(Executables executables) {

            // Got a list of running executables
            ExecutablesEventArgs startedExecutablesArgs;

            this.GetStartedExecutables(executables.Items, out startedExecutablesArgs);

            if (startedExecutablesArgs.Items.Count > 0) {
                this.ProcessExecutableStats(startedExecutablesArgs);
            }

        }


        /// <summary>
        /// Get started executables
        /// Filter out the started executables
        /// </summary>
        /// <param name="executables"></param>
        /// <param name="startedExecutablesArgs"></param>
        private void GetStartedExecutables(Arr<Executables.ItemsElementJson> executables, out ExecutablesEventArgs startedExecutablesArgs) {

            DateTime lastCheck = DateTime.MinValue;
            startedExecutablesArgs = new ExecutablesEventArgs();


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

                        Executable startedExecutable = new Executable();
                        startedExecutable.Name = executable.Name;
                        startedExecutablesArgs.Items.Add(startedExecutable);
                    }
                }

            }

            if (lastCheck != DateTime.MinValue) {
                this.LatestExectuableStarted = lastCheck;
            }

        }


        #endregion

        #region Gatewaystats task


        /// <summary>
        /// Proccess Executables
        /// Add port(s) information to each started started executable
        /// </summary>
        /// <param name="executablesArgs">Started Executables</param>
        private void ProcessExecutableStats(ExecutablesEventArgs executablesArgs) {

            BackgroundWorker bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(GatewayBackgroundWorker);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnGatewayBackgroundWorkerCompleted);
            bgWorker.RunWorkerAsync(executablesArgs);
        }


        /// <summary>
        /// Retrive executables stats (gwstats)
        /// Port(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GatewayBackgroundWorker(object sender, DoWorkEventArgs e) {

            Dictionary<string, IList<int>> executablesStats;
            ExecutablesEventArgs executablesArgs = e.Argument as ExecutablesEventArgs;

            // Get Gateway stats for all running executables
            GatewayTask.Execute(this, out executablesStats);

            // Create a list with application name and it's listening ports
            foreach (Executable item in executablesArgs.Items) {

                if (executablesStats.ContainsKey(item.Name)) {
                    // Add this to our result list
                    item.Ports = executablesStats[item.Name];
                }
                else {
                    item.Ports = new List<int>();
                }
            }

            e.Result = executablesArgs;

        }

        /// <summary>
        /// Event when retriving the gateway stats is done 
        /// (with or without errors)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGatewayBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if ((e.Cancelled == true)) {
                // Canceled
            }
            else if (!(e.Error == null)) {
                // Error
                OnError(new ErrorEventArgs() { ErrorMessage = e.Error.Message });
            }
            else {
                // Done
                if (ExecutablesStarted != null) {
                    // Invoke listeners with the list
                    ExecutablesStarted(this, e.Result as ExecutablesEventArgs);
                }
            }

        }

        #endregion

        #region Shutdown Task


        /// <summary>
        /// Shutdown Starcounter Service
        /// </summary>
        public void Shutdown() {

            BackgroundWorker bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(ShutdownBackgroundWorker);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnShutdownBackgroundWorkerCompleted);
            bgWorker.RunWorkerAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShutdownBackgroundWorker(object sender, DoWorkEventArgs e) {
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
        /// Invoke Starcounter service status changed
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnStatusChanged(StatusEventArgs e) {
            if (StatusChanged != null) {
                StatusChanged(this, e);
            }
        }


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
