
using Starcounter.Server.Service;
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace Starcounter.Server {
    /// <summary>
    /// Represents a running instance of the server, hosted in a
    /// Windows service process.
    /// </summary>
    public sealed class SystemServerProcess : ServiceBase {
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
    }
}
