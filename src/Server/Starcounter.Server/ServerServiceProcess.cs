
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
    /// Represents a running instance of the server service, 
    /// hosted in a Windows service- or interactive, user-mode
    /// process.
    /// </summary>
    public sealed class ServerServiceProcess : ServiceBase {
        const string serverOnlineEventName = "SCCODE_EXE_ADMINISTRATOR";
        Thread monitorThread;
        volatile uint monitorExitCode;

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
        /// <param name="withNoWindow">
        /// Specifies if the about-to-be-started server should be started in a new window or not.
        /// </param>
        public static void StartInteractiveOnDemand(bool withNoWindow = true) {
            string scBin = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
            string exePath = Path.Combine(scBin, StarcounterConstants.ProgramNames.ScService) + ".exe";

            var arguments = string.Empty;
            if (Debugger.IsAttached) {
                Debugger.Break();
                var debugService = false;
                if (debugService) {
                    arguments += "--sc-debug";
                }
            }

            var startInfo = new ProcessStartInfo(exePath, arguments);
            if (withNoWindow) {
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
            }

            Process p = Process.Start(startInfo);
            WaitUntilServerIsOnline(p);
        }
        
        /// <summary>
        /// Initialize a <see cref="ServerServiceProcess"/> with default
        /// values.
        /// </summary>
        public ServerServiceProcess(string serverName) {
            if (string.IsNullOrEmpty(serverName)) {
                throw new ArgumentNullException("serverName");
            }

            ServerName = serverName;
            ServiceName = ServerService.Name;
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
            monitorExitCode = 0;
            monitorThread = new Thread(new ThreadStart(MonitorThreadProcedure));
            monitorThread.Start();

            WaitUntilServerIsOnlineOrSignalExit((ignored) => { return monitorExitCode; }, Process.GetCurrentProcess());

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
            monitorExitCode = (uint)x;
        }

        /// <summary>
        /// Listens to the online event for the administrator server. If the server is already 
        /// online the method will return immediately. 
        /// </summary>
        /// <param name="serverProcess">The process to wait for, hosting the admin server.</param>
        private static void WaitUntilServerIsOnline(Process serverProcess) {
            WaitUntilServerIsOnlineOrSignalExit<Process>((proc) => {
                uint result = 0;
                proc.Refresh();
                if (proc.HasExited) {
                    try {
                        // Sometimes we are not allowed to read the exitcode. In that case 
                        // we just ignore it and send a general starcounter error.
                        result = (uint)serverProcess.ExitCode;
                    } catch (InvalidOperationException) {
                        result = Error.SCERRSERVERNOTRUNNING;
                    }

                }
                return result;
            }, serverProcess);        
        }

        static void WaitUntilServerIsOnlineOrSignalExit<T>(Func<T, uint> checkExited, T context) {
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
                        Thread.Sleep(50);
                        retries++;
                    } else {
                        signaled = serverOnlineEvent.WaitOne(timeout);
                        if (signaled)
                            break;
                    }

                    errorCode = checkExited(context);
                    if (errorCode != 0) {
                        throw ErrorCode.ToException(errorCode);
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
