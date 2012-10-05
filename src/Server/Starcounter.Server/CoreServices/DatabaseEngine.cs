
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates and abstracts the database engine, i.e. letting code
    /// using this class act on the database engine without having to know
    /// the exact underlying details about how to actually start it or
    /// what exact input to use.
    /// </summary>
    internal sealed class DatabaseEngine {
        private static class Win32 {
            internal static UInt32 EVENT_MODIFY_STATE = 0x0002;

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern IntPtr OpenEvent(UInt32 dwDesiredAccess, Int32 bInheritHandle, String lpName);

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern Int32 CloseHandle(IntPtr hObject);
        }

        internal const string DatabaseExeFileName = "scpmm.exe";
        internal const string WorkerProcessExeFileName = "boot.exe";
        internal const string MinGWCompilerFileName = "x86_64-w64-mingw32-gcc.exe";

        /// <summary>
        /// Gets the server that has instantiated this engine.
        /// </summary>
        internal readonly ServerEngine Server;

        /// <summary>
        /// Gets the full path to the database executable.
        /// </summary>
        internal string DatabaseExePath {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full path to the worker process executable.
        /// </summary>
        internal string WorkerProcessExePath {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full path to the MinGW compiler executable.
        /// </summary>
        internal string MinGWCompilerPath {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a <see cref="DatabaseEngine"/> for the given
        /// <see cref="ServerEngine"/>.
        /// </summary>
        /// <param name="server"></param>
        internal DatabaseEngine(ServerEngine server) {
            this.Server = server;
        }

        /// <summary>
        /// Performs setup of the current <see cref="DatabaseEngine"/>.
        /// </summary>
        internal void Setup() {
            var databaseExe = Path.Combine(this.Server.InstallationDirectory, DatabaseExeFileName);
            if (!File.Exists(databaseExe)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Database engine executable not found: {0}", databaseExe));
            }
            var workerProcExe = Path.Combine(this.Server.InstallationDirectory, WorkerProcessExeFileName);
            if (!File.Exists(workerProcExe)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Worker process executable not found: {0}", databaseExe));
            }
            var compilerPath = Path.Combine(this.Server.InstallationDirectory, @"MinGW\bin", MinGWCompilerFileName);
            if (!File.Exists(compilerPath)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("MinGW compiler executable not found: {0}", compilerPath));
            }

            this.DatabaseExePath = databaseExe;
            this.WorkerProcessExePath = workerProcExe;
            this.MinGWCompilerPath = compilerPath;
        }

        /// <summary>
        /// Starts the database process for the given <see cref="Database"/>.
        /// If it turns out already started, this method silently returns.
        /// </summary>
        /// <param name="database">The <see cref="Database"/> the starting
        /// process should run.</param>
        /// <returns>Returns true if the database was actually started, false
        /// if it was not (i.e. it was already running).</returns>
        internal bool StartDatabaseProcess(Database database) {
            bool databaseRunning;
            try {
                string processControlEventName;
                EventWaitHandle processControlEvent;

                processControlEventName = GetDatabaseControlEventName(database);
                processControlEvent = EventWaitHandle.OpenExisting(
                    processControlEventName,
                    System.Security.AccessControl.EventWaitHandleRights.Synchronize
                    );
                databaseRunning = !processControlEvent.WaitOne(0);
                processControlEvent.Close();

                if (!databaseRunning) {
                    // Process is shutting down. Wait for shutdown to complete and
                    // restart it.

                    WaitForDatabaseProcessToExit(processControlEventName);
                }
            } catch (WaitHandleCannotBeOpenedException) {
                databaseRunning = false;
            }

            if (!databaseRunning) {
                ProcessStartInfo startInfo;
                Process databaseProcess;

                startInfo = GetDatabaseStartInfo(database);
                databaseProcess = Process.Start(startInfo);
                databaseProcess.Close();
            }

            return !databaseRunning;
        }

        /// <summary>
        /// Stops the database process for the given <see cref="Database"/>.
        /// If it turns out already stopped, this method silently returns.
        /// </summary>
        /// <param name="database">The <see cref="Database"/> the stopping
        /// process runs.</param>
        /// <returns>Returns true if the database was actually stopped, false
        /// if it was not (i.e. it was not running).</returns>
        internal bool StopDatabaseProcess(Database database) {
            string processControlEventName;
            EventWaitHandle processControlEvent;
            string errorReason;

            processControlEventName = GetDatabaseControlEventName(database);
            try {
                processControlEvent = EventWaitHandle.OpenExisting(processControlEventName, EventWaitHandleRights.Modify);
            } catch (WaitHandleCannotBeOpenedException) {
                processControlEvent = null;
            } catch (UnauthorizedAccessException) {
                errorReason =
                    string.Format("User '{0}' unauthorized to modify named control event \"{1}\".",
                    WindowsIdentity.GetCurrent().Name,
                    processControlEventName
                    );
                throw new ErrorInfoException(new ErrorInfo[] { new ErrorInfo("StopDatabaseFailed", database.Name, errorReason) });
            }

            if (processControlEvent == null) {
                return false;
            }

            processControlEvent.Set();
            processControlEvent.Close();
            processControlEvent = null;
            Thread.Yield();

            WaitForDatabaseProcessToExit(processControlEventName);
            return true;
        }

        internal bool IsDatabaseProcessRunning(Database database) {
            String processControlEventName = GetDatabaseControlEventName(database);
            return IsDatabaseProcessRunning(processControlEventName);
        }

        internal bool IsDatabaseProcessRunning(string processControlEventName) {
            IntPtr hProcessControlEvent;

            hProcessControlEvent = Win32.OpenEvent(Win32.EVENT_MODIFY_STATE, 0, processControlEventName);
            if (hProcessControlEvent != IntPtr.Zero) {
                Win32.CloseHandle(hProcessControlEvent);
                return true;
            } else {
                return false;
            }

#if false
            EventWaitHandle processControlEvent;
            try
            {
                processControlEvent = EventWaitHandle.OpenExisting(processControlEventName, EventWaitHandleRights.Modify);
                processControlEvent.Close();
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
#endif
        }

        internal bool StartWorkerProcess(Database database, out Process process) {
            process = Process.Start(GetWorkerProcessStartInfo(database));
            return true;
        }

        void WaitForDatabaseProcessToExit(string processControlEventName) {
            while (IsDatabaseProcessRunning(processControlEventName)) Thread.Sleep(1);
        }

        ProcessStartInfo GetDatabaseStartInfo(Database database) {
            var arguments = new StringBuilder();

            arguments.Append(database.Name.ToUpperInvariant());
            arguments.Append(' ');

            arguments.Append('\"');
            arguments.Append(database.Uri);
            arguments.Append('\"');
            arguments.Append(' ');

            arguments.Append('\"');
            arguments.Append(database.Server.Configuration.LogDirectory.TrimEnd('\\'));
            arguments.Append('\"');

            return new ProcessStartInfo(this.DatabaseExePath, arguments.ToString());
        }

        ProcessStartInfo GetWorkerProcessStartInfo(Database database) {
            ProcessStartInfo processStart;
            StringBuilder args;

            args = new StringBuilder();
            args.Append("--FLAG:attachdebugger ");  // Apply to attach a debugger to the boot sequence.
            args.Append(database.Name.ToUpper());
            args.AppendFormat(" --DatabaseDir \"{0}\"", database.Configuration.Runtime.ImageDirectory);
            args.AppendFormat(" --OutputDir \"{0}\"", database.Server.Configuration.LogDirectory);
            args.AppendFormat(" --TempDir \"{0}\"", database.Configuration.Runtime.TempDirectory);
            args.AppendFormat(" --CompilerPath \"{0}\"", this.MinGWCompilerPath);
            
            processStart = new ProcessStartInfo(this.WorkerProcessExePath, args.ToString().Trim());
            processStart.CreateNoWindow = true;
            processStart.UseShellExecute = false;
            processStart.RedirectStandardInput = true;
            processStart.RedirectStandardOutput = true;
            processStart.RedirectStandardError = true;    
            
            return processStart;
        }

        string GetDatabaseControlEventName(Database database) {
            ScUri uri = ScUri.FromString(database.Uri);
            string processControlEventName = string.Format(
                "SCPMM_EXE_{0}_{1}",
                uri.ServerName.ToUpperInvariant(),
                uri.DatabaseName.ToUpperInvariant()
                );
            return processControlEventName;
        }
    }
}