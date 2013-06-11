using Codeplex.Data;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Tracking {

    /// <summary>
    /// Client for sending Usage Tracking to Tracking Server
    /// </summary>
    public sealed class Client {

        #region Singelton

        private static readonly Client instance = new Client();

        private Client() {
            this.ServerIP = Starcounter.Tracking.Environment.StarcounterTrackerIp;
            this.ServerPort = Starcounter.Tracking.Environment.StarcounterTrackerPort;
        }

        /// <summary>
        /// Singelton Instance
        /// </summary>
        public static Client Instance {
            get {
                return instance;
            }
        }
        #endregion

        #region Properties


        private ushort ServerPort { get; set; }
        private string ServerIP { get; set; }

        /// <summary>
        /// tru if the tracking is acrive
        /// </summary>
        public bool IsTracking { get; private set; }

        private AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private bool abortTracking = false;

        #endregion
        /// <summary>
        /// Different installation modes
        /// </summary>
        public enum InstallationMode {
            /// <summary>
            /// Full Installation
            /// </summary>
            FullInstallation = 1,
            /// <summary>
            /// Partial installation
            /// </summary>
            PartialInstallation = 2,
            /// <summary>
            /// Full uninstallation
            /// </summary>
            FullUninstallation = 3,
            /// <summary>
            /// Partial uninstallation
            /// </summary>
            PartialUninstallation = 4
        }

        /// <summary>
        /// Start the Usage tracking
        /// </summary>
        /// <param name="ServerInterface"></param>
        public void StartTrackUsage(IServerRuntime ServerInterface) {

            if (this.IsTracking) throw new InvalidOperationException("Trying to start Tracking when is was already running");

            ThreadPool.QueueUserWorkItem(PollThread, ServerInterface);
        }

        /// <summary>
        /// Stops the stracking usage
        /// </summary>
        public void StopTrackUsage() {

            if (this.IsTracking == false) throw new InvalidOperationException("Trying to stop Tracking when it was not running");

            this.abortTracking = true;
            this.autoResetEvent.Set();

        }

        private void PollThread(object state) {

            IServerRuntime serverInterface = state as IServerRuntime;

            if (serverInterface == null) throw new ArgumentNullException("state");

            DatabaseInfo[] databaseInfos;
            int runningDatabases;
            int runningexecutables;

            while (true) {


                // Collect usage statistics
                databaseInfos = serverInterface.GetDatabases();
                int databases = databaseInfos.Length;
                runningDatabases = 0;
                runningexecutables = 0;

                foreach (DatabaseInfo dbInfo in databaseInfos) {
                    if (dbInfo.Engine != null) {
                        if (dbInfo.Engine.HostProcessId > 0) {
                            runningDatabases++;
                        }
                        if (dbInfo.Engine.HostedApps != null) {
                            runningexecutables += dbInfo.Engine.HostedApps.Length;
                        }
                    }
                }

                // Send usage statistics
                this.SendStarcounterUsage(databases, -1, runningDatabases, runningexecutables);

                this.autoResetEvent.WaitOne(1000 * 60 * 60); // 1h
                if (this.abortTracking == true) {
                    this.abortTracking = false;
                    break;
                }
            }

            this.IsTracking = false;

        }


        /// <summary>
        /// Send installer start tracking message
        /// When the Installer EXE is started
        /// </summary>
        /// <param name="completeCallback"></param>
        public void SendInstallerStart(EventHandler<CompletedEventArgs> completeCallback) {

            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.start = new { };
                this.AddHeader(contentJson.start);
                this.AddEnvironment(contentJson.start);

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/start", content, completeCallback);

                // sequenceNo

            }
            catch (Exception) {
                // TODO: Logging
            }

        }

        /// <summary>
        /// Send installer start tracking message
        /// When the Installer EXE is started
        /// </summary>
        public void SendInstallerStart() {
            this.SendInstallerStart(null);
        }


        /// <summary>
        /// Send installer executing tracking message
        /// When the installer starts an executing (installing/uninstalling)
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="personalServer"></param>
        /// <param name="vs2012Extention"></param>
        public void SendInstallerExecuting(InstallationMode mode, bool personalServer, bool vs2012Extention) {

            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.executing = new { };
                this.AddHeader(contentJson.executing);

                contentJson.executing.mode = (int)mode;  // 1 = Full installation, 2= Partial installation, 3 = Full uninstallation, 4 = Partial uninstallation

                contentJson.executing.personalServer = personalServer;
                contentJson.executing.vs2012Extention = vs2012Extention;

                //contentJson.executing.options = new object[] { };
                //contentJson.executing.options[0] = new { id = "personalServer", value = true };
                //contentJson.executing.options[1] = new { id = "vs2012Extentsion", value = true };

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/executing", content);
            }
            catch (Exception) {
                // TODO: Logging
            }

        }

        /// <summary>
        /// Send installer abortion tracking message
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="message"></param>
        public void SendInstallerAbort(InstallationMode mode, string message) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.abort = new { };

                this.AddHeader(contentJson.abort);

                contentJson.abort.mode = (int)mode;  // 1 = Full installation, 2= Partial installation, 3 = Full uninstallation, 4 = Partial uninstallation

                contentJson.abort.message = message;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/abort", content);
            }
            catch (Exception) {
                // TODO: Logging
            }

        }

        /// <summary>
        /// Send installer finish tracking message
        /// When installation/uninstallation is finished
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="success"></param>
        public void SendInstallerFinish(InstallationMode mode, bool success) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.finish = new { };
                this.AddHeader(contentJson.finish);

                contentJson.finish.mode = (int)mode;  // 1 = Full installation, 2= Partial installation, 3 = Full uninstallation, 4 = Partial uninstallation

                contentJson.finish.success = success;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/finish", content);
            }
            catch (Exception) {
                // TODO: Logging
            }

        }

        /// <summary>
        /// Send installer end tracking message
        /// When the Installer EXE exits
        /// </summary>
        /// <param name="linksUserClickedOn"></param>
        public void SendInstallerEnd(string linksUserClickedOn) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.end = new { };

                this.AddHeader(contentJson.end);

                contentJson.end.linksClicked = linksUserClickedOn;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/end", content);
            }
            catch (Exception) {
                // TODO: Logging
            }


        }

        /// <summary>
        /// Send starcounter usage tracking message
        /// </summary>
        /// <param name="databases"></param>
        /// <param name="transactions"></param>
        /// <param name="runningDatabases"></param>
        /// <param name="runningExecutables"></param>
        public void SendStarcounterUsage(long databases, long transactions, long runningDatabases, long runningExecutables) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.usage = new { };

                this.AddHeader(contentJson.usage);
                this.AddEnvironment(contentJson.usage);

                // Add Statistics
                contentJson.usage.transactions = transactions;
                contentJson.usage.runningDatabases = runningDatabases;
                contentJson.usage.runningExecutables = runningExecutables;
                contentJson.usage.databases = databases;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/starcounter", content);
            }
            catch (Exception) {
                // TODO: Logging
            }


        }

        /// <summary>
        /// Send starcounter general tracking message
        /// </summary>
        /// <param name="module"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        public void SendStarcounterGeneral(string module, string type, string message) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.general = new { };
                this.AddHeader(contentJson.general);

                contentJson.general.module = module;
                contentJson.general.type = type;
                contentJson.general.message = message;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/general", content);
            }
            catch (Exception) {
                // TODO: Logging
            }

        }

        private void SendData(string uri, string content) {
            this.SendData(uri, content, null);

        }
        private void SendData(string uri, string content, EventHandler<CompletedEventArgs> completeCallback) {

            // This is a temporary solution until we have true Node async mode.
            ThreadPool.QueueUserWorkItem(SendThread, new object[] { uri, content, completeCallback });


        }

        /// <summary>
        /// This is a temporary solution until we have true Node async mode.
        /// </summary>
        /// <param name="state"></param>
        private void SendThread(object state) {

            // Send json content to server
            string uri = ((object[])state)[0] as string;
            string content = ((object[])state)[1] as string;
            EventHandler<CompletedEventArgs> completeCallback = ((object[])state)[2] as EventHandler<CompletedEventArgs>;

            try {

                RequestHandler.InitREST();  // TODO: Add a check if we need to do this or not (currently there is now way to check)
                // https://github.com/Starcounter/Starcounter/issues/473

                Node node = new Node(this.ServerIP, this.ServerPort);

                Advanced.Response response = node.POST(uri, content, "Accept: application/starcounter.tracker.usage-v1+json\r\n", null);

                if (response.StatusCode >= 200 && response.StatusCode < 300) {
                    // Success

                    // If the tracking server response with a new sequenceNo we will use it
                    // NOTE: This is only for the InstallerStart request, but att the moment we dont know the calling type
                    //       It will be fixed when Node async bug if solved
                    String responseContent = response.GetBodyStringUtf8_Slow();
                    if (!string.IsNullOrEmpty(responseContent)) {
                        dynamic incomingJson = DynamicJson.Parse(responseContent);
                        if (incomingJson.IsDefined("installation")) {
                            if (incomingJson.installation.IsDefined("sequenceNo")) {
                                Environment.SaveInstallationNo(int.Parse(incomingJson.installation.sequenceNo.ToString()));
                            }

                        }
                    }

                }
                else {
                    // Error
                    string message = "ERROR: UsageTracker http-StatusCode:" + response.StatusCode;
                    //Console.WriteLine("ERROR: UsageTracker http-StatusCode:" + response.StatusCode);

                    if (completeCallback != null) {
                        completeCallback(this, new CompletedEventArgs(message));
                    }

                }
            }
            catch (SocketException s) {

                if (completeCallback != null) {
                    completeCallback(this, new CompletedEventArgs(s));
                }

                if (s.SocketErrorCode == SocketError.ConnectionRefused) {
                    // Conneciton Refused
                }
                else {

                }
            }
            catch (Exception e) {

                if (completeCallback != null) {
                    completeCallback(this, new CompletedEventArgs(e));
                }

                //Console.WriteLine("ERROR: UsageTracker.usage " + e.Message + System.Environment.NewLine + e.ToString());
            }


        }


        private void AddHeader(dynamic json) {

            json.date = DateTime.UtcNow.ToString("s") + "Z"; // "2012-04-23T18:25:43.511Z"
            json.downloadId = CurrentVersion.IDFullBase32;
            json.mac = Environment.GetTruncatedMacAddress();
            json.installationNo = Environment.GetInstallationNo();

        }

        private void AddEnvironment(dynamic json) {

            json.installedRam = Environment.GetInstalledRAM();
            json.availableRam = -1; // TODO
            json.cpu = Environment.GetCPUFriendlyName();
            json.os = Environment.GetOSFriendlyName();
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class CompletedEventArgs : EventArgs {

        /// <summary>
        /// 
        /// </summary>
        public bool HasData {
            get {
                return this.Data != null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public object Data { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public bool HasError {
            get {
                return this.Error != null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Exception Error { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public CompletedEventArgs() {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public CompletedEventArgs(object data) {
            this.Data = data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public CompletedEventArgs(Exception e) {
            this.Error = e;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        public CompletedEventArgs(Exception e, object data) {
            this.Error = e;
            this.Data = data;
        }
    }
}
