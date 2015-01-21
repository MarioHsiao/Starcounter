using Codeplex.Data;
using Starcounter.ErrorReporting;
using Starcounter.Internal;
using Starcounter.Logging;
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
    using Starcounter.Advanced;
    using TrackingEnvironment = Starcounter.Tracking.Environment;

    /// <summary>
    /// Client for sending Usage Tracking to Tracking Server
    /// </summary>
    public sealed class Client {

        #region Singelton

        private static readonly Client instance = new Client();

        private Client() {
            this.ServerIP = TrackingEnvironment.StarcounterTrackerIp;
            try {
                var server = System.Environment.GetEnvironmentVariable("STAR_TRACKER_IP");
                if (!string.IsNullOrWhiteSpace(server)) {
                    this.ServerIP = server;
                }
            }
            catch { }

            this.ServerPort = TrackingEnvironment.StarcounterTrackerPort;
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
        LogSource serverLog;

        Thread thread;
        AutoResetEvent stop = new AutoResetEvent(false);

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
        /// <param name="serverInterface"></param>
        /// <param name="log"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void StartTrackUsage(
            IServerRuntime serverInterface,
            LogSource log = null,
            string host = null,
            ushort port = 0) {
            if (serverInterface == null) {
                throw new ArgumentNullException("serverInterface");
            }
            if (host != null) {
                this.ServerIP = host;
            }
            if (port > 0) {
                this.ServerPort = port;
            }

            serverLog = log;
            lock (this) {
                if (thread == null) {
                    thread = new Thread(new ParameterizedThreadStart(new WaitCallback((object state) => {
                        var server = state as IServerRuntime;
                        var pulse = TimeSpan.FromMinutes(1);
                        var usageIntervall = TimeSpan.FromHours(1);
                        var next = DateTime.Now;
                        var reporter = new ErrorReporter(server.GetServerInfo().Configuration.LogDirectory, serverLog, ServerIP, ServerPort);

                        do {
                            var now = DateTime.Now;

                            if (now >= next) {
                                // Collect usage statistics
                                var databaseInfos = server.GetDatabases();
                                var databases = databaseInfos.Length;
                                var runningDatabases = 0;
                                var runningexecutables = 0;

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
                                SendStarcounterUsage(databases, -1, runningDatabases, runningexecutables);

                                next = next.Add(usageIntervall);
                            }

                            // Send error reports
                            reporter.CheckAndSendErrorReports();
                        }
                        while (!stop.WaitOne(pulse));

                    })));

                    thread.Name = "Starcounter usage statistics thread";
                    thread.Start(serverInterface);
                }
            }
        }

        /// <summary>
        /// Stops the stracking usage
        /// </summary>
        public void StopTrackUsage() {
            lock (this) {
                if (thread != null) {
                    stop.Set();
                    thread.Join();
                    thread = null;
                }
            }
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
        /// <param name="vs2013Extention"></param>
        /// <param name="completeCallback"></param>
        public void SendInstallerExecuting(InstallationMode mode, bool personalServer, bool vs2012Extention, bool vs2013Extention, EventHandler<CompletedEventArgs> completeCallback) {

            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.executing = new { };
                this.AddHeader(contentJson.executing);

                contentJson.executing.mode = (int)mode;  // 1 = Full installation, 2= Partial installation, 3 = Full uninstallation, 4 = Partial uninstallation

                contentJson.executing.personalServer = personalServer;
                contentJson.executing.vs2012Extention = vs2012Extention;
                contentJson.executing.vs2013Extention = vs2013Extention;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/executing", content, completeCallback);
            }
            catch (Exception) {
                // TODO: Logging
            }

        }


        /// <summary>
        /// Send installer executing tracking message
        /// When the installer starts an executing (installing/uninstalling)
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="personalServer"></param>
        /// <param name="vs2012Extention"></param>
        /// <param name="vs2013Extention"></param>
        public void SendInstallerExecuting(InstallationMode mode, bool personalServer, bool vs2012Extention, bool vs2013Extention) {
            this.SendInstallerExecuting(mode, personalServer, vs2012Extention, vs2013Extention, null);
        }



        /// <summary>
        /// Send installer abortion tracking message
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="message"></param>
        /// <param name="completeCallback"></param>
        public void SendInstallerAbort(InstallationMode mode, string message, EventHandler<CompletedEventArgs> completeCallback) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.abort = new { };

                this.AddHeader(contentJson.abort);

                contentJson.abort.mode = (int)mode;  // 1 = Full installation, 2= Partial installation, 3 = Full uninstallation, 4 = Partial uninstallation

                contentJson.abort.message = message;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/abort", content, completeCallback);
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
            this.SendInstallerAbort(mode, message, null);

        }


        /// <summary>
        /// Send installer exception to tracking message
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <param name="completeCallback"></param>
        public void SendInstallerException(Exception e, EventHandler<CompletedEventArgs> completeCallback) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.exception = new { };

                this.AddHeader(contentJson.exception);

                contentJson.exception.message = e.Message;
                contentJson.exception.stackTrace = e.StackTrace;
                contentJson.exception.helpLink = e.HelpLink;

                // Send json content to server
                this.SendData("/api/usage/installer/exceptions", contentJson.ToString(), completeCallback);
            }
            catch (Exception) {
                // TODO: Logging
            }

        }

        /// <summary>
        /// Send installer exception to tracking message
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        public void SendInstallerException(Exception e) {
            this.SendInstallerException(e, null);

        }


        /// <summary>
        /// Send installer finish tracking message
        /// When installation/uninstallation is finished
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="success"></param>
        /// <param name="completeCallback"></param>
        public void SendInstallerFinish(InstallationMode mode, bool success, EventHandler<CompletedEventArgs> completeCallback) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.finish = new { };
                this.AddHeader(contentJson.finish);

                contentJson.finish.mode = (int)mode;  // 1 = Full installation, 2= Partial installation, 3 = Full uninstallation, 4 = Partial uninstallation

                contentJson.finish.success = success;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/finish", content, completeCallback);
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
            this.SendInstallerFinish(mode, success, null);
        }

        /// <summary>
        /// Send installer end tracking message
        /// When the Installer EXE exits
        /// </summary>
        /// <param name="linksUserClickedOn"></param>
        /// <param name="completeCallback"></param>
        public void SendInstallerEnd(string linksUserClickedOn, EventHandler<CompletedEventArgs> completeCallback) {
            try {
                // Build json content
                dynamic contentJson = new DynamicJson();

                contentJson.end = new { };

                this.AddHeader(contentJson.end);

                contentJson.end.linksClicked = linksUserClickedOn;

                string content = contentJson.ToString();

                // Send json content to server
                this.SendData("/api/usage/installer/end", content, completeCallback);
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
            this.SendInstallerEnd(linksUserClickedOn, null);
        }

        /// <summary>
        /// Send starcounter usage tracking message
        /// </summary>
        /// <param name="databases"></param>
        /// <param name="transactions"></param>
        /// <param name="runningDatabases"></param>
        /// <param name="runningExecutables"></param>
        /// <param name="completeCallback"></param>
        public void SendStarcounterUsage(long databases, long transactions, long runningDatabases, long runningExecutables, EventHandler<CompletedEventArgs> completeCallback) {
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
                this.SendData("/api/usage/starcounter", content, completeCallback);
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
            this.SendStarcounterUsage(databases, transactions, runningDatabases, runningExecutables, null);
        }

        /// <summary>
        /// Send starcounter general tracking message
        /// </summary>
        /// <param name="module"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="completeCallback"></param>
        public void SendStarcounterGeneral(string module, string type, string message, EventHandler<CompletedEventArgs> completeCallback) {
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
                this.SendData("/api/usage/general", content, completeCallback);
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
            this.SendStarcounterGeneral(module, type, message, null);
        }


        /// <summary>
        /// Send data to tracker
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="content"></param>
        /// <param name="completeCallback"></param>
        private void SendData(string uri, string content, EventHandler<CompletedEventArgs> completeCallback) {

            Node node = new Node(this.ServerIP, this.ServerPort);

            // ==================================================================================================
            // Protocol version history
            // --------------------------------------------------------------------------------------------------
            // v1 - 2013-06-01 Firstversion
            // v2 - 2013-06-14 Added "version" to the header
            //                 The response "installation.sequenceNo" was changed to "installation.installationNo" 
            // ==================================================================================================

            Dictionary<String, String> headers = new Dictionary<String, String> { { "Accept", "application/starcounter.tracker.usage-v2+json" } };
            node.POST(uri, content, headers, null, (Response respAsync, Object userObject) => {

                if (respAsync.IsSuccessStatusCode) {

                    // If the tracking server response with a new sequenceNo we will use it
                    // NOTE: This is only for the InstallerStart request, but at the moment we dont know the calling type
                    String responseContent = respAsync.Body;
                    if (!string.IsNullOrEmpty(responseContent)) {
                        dynamic incomingJson = DynamicJson.Parse(responseContent);
                        if (incomingJson.IsDefined("installation")) {
                            if (incomingJson.installation.IsDefined("installationNo")) {
                                Environment.SaveInstallationNo((Int64)incomingJson.installation.installationNo);
                            }

                        }
                    }

                    if (completeCallback != null) {
                        completeCallback(this, new CompletedEventArgs());
                    }

                }
                else {
                    string message = "ERROR: UsageTracker http-StatusCode:" + respAsync.StatusCode;
                    if (completeCallback != null) {
                        completeCallback(this, new CompletedEventArgs(new Exception(message)));
                    }
                }

            }, 10000); // 10 Sec timeout


        }


        private void SendData(string uri, string content) {
            this.SendData(uri, content, null);
        }


        private void AddHeader(dynamic json) {

            json.date = DateTime.UtcNow.ToString("s") + "Z"; // "2012-04-23T18:25:43.511Z"
            json.downloadId = Environment.UniqueDownloadKey;
            json.mac = Environment.GetTruncatedMacAddress();
            json.installationNo = Environment.GetInstallationNo();
            json.version = CurrentVersion.Version;

        }

        private void AddEnvironment(dynamic json) {

            json.installedRam = Environment.GetInstalledRAM();
            json.availableRam = -1; // TODO
            json.cpu = Environment.GetCPUFriendlyName();
            json.os = Environment.GetOSFriendlyName();
        }

    }

    /// <summary>
    /// Completed Event
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
