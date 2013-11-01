
using Starcounter.Internal;
using Starcounter.Server.Service;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace Starcounter.Server {
    /// <summary>
    /// Represents a running instance of the server, hosted in a
    /// Windows service process.
    /// </summary>
    public sealed class SystemServerProcess : ServiceBase {
        const string serverOnlineEventName = "SCCODE_EXE_ADMINISTRATOR";
        Thread monitorThread;

        [DllImport("scservicelib.dll", EntryPoint = "Start", CallingConvention = CallingConvention.StdCall)]
        static extern unsafe int StartMonitor(string server, bool logSteps);

        [DllImport("scservicelib.dll", EntryPoint = "Stop", CallingConvention = CallingConvention.StdCall)]
        static extern unsafe int StopMonitor();

        /// <summary>
        /// Gets or sets a value indicating how the current
        /// process was told to be hosted when started; as a Windows
        /// service, or as a standard, user interactive process.
        /// </summary>
        public bool RunService { get; private set; }

        /// <summary>
        /// Gets or sets the name of the server to start, passed 
        /// to the core service library.
        /// </summary>
        public readonly string ServerName;

        /// <summary>
        /// Gets or sets a value indicating of the core service
        /// library should be invoked with the parameter with the
        /// same name.
        /// </summary>
        public bool LogSteps { get; set; }

        /// <summary>
        /// Checks if the personal server is up and running and is available for new requests.
        /// </summary>
        /// <returns><c>true</c> if the server is running and considered online; <c>false</c>
        /// if not running.</returns>
        public static bool IsOnline() {
            string serviceName = StarcounterConstants.ProgramNames.ScService;
            foreach (var p in Process.GetProcesses()) {
                if (serviceName.Equals(p.ProcessName)) {
                    // We have the a process with the correct name, lets check if it's the system or personal service.
                    // If it's running in the current interactive session, we wait for it to come online. Else (if its
                    // running as a service), this should be guranteed by the startup of the server service bootstrap
                    // itself).
                    if (p.SessionId != 0) {
                        WaitUntilServerIsOnline(p);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Starts the server in interactive mode, on demand. When the method returns it is assured
        /// that the server is running and available for new requests. Exception will be thrown for
        /// any failure that happens during startup.
        /// </summary>
        /// <remarks>
        /// This method does not check for an existing running server. If the server is already 
        /// running an exception will be thrown.
        /// </remarks>
        public static void StartInteractiveOnDemand() {
            string scBin = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
            string exePath = Path.Combine(scBin, StarcounterConstants.ProgramNames.ScService) + ".exe";

            Process p = Process.Start(exePath);
            WaitUntilServerIsOnline(p);
        }
        
        /// <summary>
        /// Initialize a <see cref="SystemServerProcess"/> with default
        /// values.
        /// </summary>
        public SystemServerProcess(string serverName) {
            if (string.IsNullOrEmpty(serverName)) {
                throw new ArgumentNullException("serverName");
            }

            ServerName = serverName;
            ServiceName = SystemServerService.Name;
            CanStop = true;
            CanPauseAndContinue = false;
        }

        /// <summary>
        /// Executes the server.
        /// </summary>
        /// <param name="runService">If <c>true</c>, the current
        /// server process will attach and run under the Windows
        /// Service Control Manager as a service; otherwise, as
        /// a standard user-interactive process.</param>
        public void Launch(bool runService = false) {
            if (runService) {
                ServiceBase.Run(this);
            } else {
                RunInteractive();
            }
        }

        /// <summary>
        /// Runs the server in an interactive process, i.e. under a
        /// logged in user.
        /// </summary>
        void RunInteractive() {
            RunUntilStopped();
        }

        protected override void OnStart(string[] args) {
            monitorThread = new Thread(new ThreadStart(MonitorThreadProcedure));
            monitorThread.Start();

            // Should we wait for the principal startup to
            // be considered succeeded? I.e. have some event
            // that we wait for, and that the core service
            // set it?

            base.OnStart(args);
        }

        protected override void OnStop() {
            StopMonitor();
            monitorThread.Join();
            base.OnStop();
        }

        void MonitorThreadProcedure() {
            RunUntilStopped();
        }

        void RunUntilStopped() {
            int x = StartMonitor(ServerName, LogSteps);
            Environment.ExitCode = x;
        }

        /// <summary>
        /// Listens to the online event for the administrator server. If the server is already 
        /// online the method will return immediately. 
        /// </summary>
        /// <param name="serverProcess"></param>
        private static void WaitUntilServerIsOnline(Process serverProcess) {
            int retries = 60;
            int timeout = 1000; // timeout per wait for signal, not total timeout wait.
            bool signaled;
            uint errorCode = 0;
            EventWaitHandle serverOnlineEvent = null;

            try {
                while (true) {
                    retries--;
                    if (retries == 0)
                        throw ErrorCode.ToException(Error.SCERRWAITTIMEOUT);

                    if (serverOnlineEvent == null && !EventWaitHandle.TryOpenExisting(serverOnlineEventName, out serverOnlineEvent)) {
                        Thread.Sleep(100);
                    } else {
                        signaled = serverOnlineEvent.WaitOne(timeout);
                        if (signaled)
                            break;
                    }

                    if (serverProcess.HasExited) {

                        try {
                            // Sometimes we are not allowed to read the exitcode. In that case 
                            // we just ignore it and send a general starcounter error.
                            errorCode = (uint)serverProcess.ExitCode;
                        } catch (InvalidOperationException) { }


                        if (errorCode != 0) {
                            throw ErrorCode.ToException(errorCode);
                        }
                        throw ErrorCode.ToException(Error.SCERRSERVERNOTRUNNING);
                    }
                }
            } finally {
                if (serverOnlineEvent != null) {
                    serverOnlineEvent.Close();
                }
            }
        }
    }
}
