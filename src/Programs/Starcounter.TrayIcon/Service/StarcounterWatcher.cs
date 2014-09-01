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
    /// Checks Starcounter running mode and applications changes
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
                try {
                    return ServerServiceProcess.IsOnline();
                }
                catch (Exception) {
                    return false;
                }
            }
        }


        /// <summary>
        /// Background worker
        /// </summary>
        private BackgroundWorker BackgroundWorker;


        /// <summary>
        /// Last Started Application
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
        /// applications started event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ApplicationsStartedEventHandler(object sender, ScApplicationsEventArgs e);


        /// <summary>
        /// Applications started event
        /// </summary>
        public event ApplicationsStartedEventHandler ApplicationsStarted;


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
        /// Start worker thread that polls the status and the application list
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
            statusArgs.Running = this.IsStarcounterServiceOnline;
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
            // for applications.
            bool modeFlag = true; // True = Polling status, False = Polling applications

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
                        // Switch to polling applications
                        modeFlag = false;
                        continue;
                    }
                }
                else {

                    // Starcounter server is running
                    // Get applications
                    Executables applications;
                    bool result = ScApplicationsTask.Execute(this, out applications);
                    if (result) {
                        worker.ReportProgress(0, applications);
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
                // Got Application list
                OnApplicationsList(e.UserState as Executables);
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
                    OnApplicationsList(e.Result as Executables);
                }
            }
        }



        /// <summary>
        /// Got a list of running applications
        /// </summary>
        /// <param name="applications"></param>
        protected virtual void OnApplicationsList(Executables applications) {

            // Got a list of running applications
            ScApplicationsEventArgs startedApplicationsArgs;

            this.GetStartedApplications(applications.Items, out startedApplicationsArgs);

            if (startedApplicationsArgs.Items.Count > 0) {
                this.ProcessApplicationStats(startedApplicationsArgs);
            }

        }


        /// <summary>
        /// Get started applications
        /// Filter out the started applications
        /// </summary>
        /// <param name="applications"></param>
        /// <param name="startedApplicationsArgs"></param>
        private void GetStartedApplications(Arr<Executables.ItemsElementJson> applications, out ScApplicationsEventArgs startedApplicationsArgs) {

            DateTime lastCheck = DateTime.MinValue;
            startedApplicationsArgs = new ScApplicationsEventArgs();


            foreach (Executables.ItemsElementJson application in applications) {

                if (!string.IsNullOrEmpty(application.RuntimeInfo.LastRestart) || string.IsNullOrEmpty(application.RuntimeInfo.Started)) {
                    continue;
                }

                DateTime started = DateTime.Parse(application.RuntimeInfo.Started);

                if (started > this.LatestExectuableStarted) {

                    if (started > lastCheck) {
                        lastCheck = started;
                    }

                    if (ApplicationsStarted != null) {

                        ScApplication startedApplication = new ScApplication();
                        startedApplication.Name = application.Name;
                        startedApplicationsArgs.Items.Add(startedApplication);
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
        /// Proccess Applications
        /// Add port(s) information to each started started executable
        /// </summary>
        /// <param name="applicationsArgs">Started Executables</param>
        private void ProcessApplicationStats(ScApplicationsEventArgs applicationsArgs) {

            BackgroundWorker bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(GatewayBackgroundWorker);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnGatewayBackgroundWorkerCompleted);
            bgWorker.RunWorkerAsync(applicationsArgs);
        }


        /// <summary>
        /// Retrive executables stats (gwstats)
        /// Port(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GatewayBackgroundWorker(object sender, DoWorkEventArgs e) {

            Dictionary<string, IList<int>> executablesStats;
            ScApplicationsEventArgs executablesArgs = e.Argument as ScApplicationsEventArgs;

            int retryCnt = 0;
            retry:

            // Wait a bit to let the applications registered some ports.
            Thread.Sleep(1000);

            // Get Gateway stats for all running executables
            NetworkTask.Execute(this, out executablesStats);

            // Create a list with application name and it's listening ports
            foreach (ScApplication item in executablesArgs.Items) {

                if (executablesStats.ContainsKey(item.Name)) {
                    // Add this to our result list
                    item.Ports = executablesStats[item.Name];
                }
                else {

                    // Retry getting port number.
                    // Sometimes when detecting that there is a new application running the
                    // application has not yet registered some handlers, lets wait a moment and retry
                    // collection the port numbers
                    if (retryCnt < 3) {
                        retryCnt++;
                        goto retry;
                    }
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
                if (ApplicationsStarted != null) {
                    // Invoke listeners with the list
                    ApplicationsStarted(this, e.Result as ScApplicationsEventArgs);
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
