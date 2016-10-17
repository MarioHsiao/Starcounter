// ***********************************************************************
// <copyright file="DatabaseEngine.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Bootstrap.Management;
using Starcounter.CommandLine;
using Starcounter.Hosting;
using Starcounter.Internal;
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using StarcounterInternal.Bootstrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Logging;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates and abstracts the database engine, i.e. letting code
    /// using this class act on the database engine without having to know
    /// the exact underlying details about how to actually start it or
    /// what exact input to use.
    /// </summary>
    internal sealed class DatabaseEngine {
        readonly LogSource log = ServerLogSources.Default;

        private static class Win32 {
            internal static UInt32 EVENT_MODIFY_STATE = 0x0002;

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern IntPtr OpenEvent(UInt32 dwDesiredAccess, Int32 bInheritHandle, String lpName);

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern Int32 CloseHandle(IntPtr hObject);

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal static extern int WaitNamedPipe(string lpNamedPipeName, int nTimeOut);
        }

        static class ScDataEvents {
            public const string SC_S2MM_CONTROL_EVENT_NAME_BASE = "SCDATA_EXE_";
        }

        public static class ScCodeEvents {
            /// <summary>
            /// The base name used for the event signaled by the code
            /// host when it's services are considered available.
            /// </summary>
            public const string OnlineBaseName = "SCCODE_EXE_";
        }

        internal const string DatabaseExeFileName = StarcounterConstants.ProgramNames.ScData + ".exe";
        internal const string CodeHostExeFileName = StarcounterConstants.ProgramNames.ScCode + ".exe";

        /// <summary>
        /// Gets the server that has instantiated this engine.
        /// </summary>
        internal readonly ServerEngine Server;

        /// <summary>
        /// Gets the database engine monitor service
        /// </summary>
        internal readonly DatabaseEngineMonitor Monitor;

        /// <summary>
        /// Gets the full path to the database executable.
        /// </summary>
        internal string DatabaseExePath {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full path to the code host executable.
        /// </summary>
        internal string CodeHostExePath {
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
            this.Monitor = new DatabaseEngineMonitor(server);
        }

        /// <summary>
        /// Performs setup of the current <see cref="DatabaseEngine"/>.
        /// </summary>
        internal void Setup() {
            var databaseExe = Path.Combine(this.Server.InstallationDirectory, DatabaseExeFileName);
            if (!File.Exists(databaseExe)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Database engine executable not found: {0}", databaseExe));
            }
            
            var codeHostExe = Path.Combine(this.Server.InstallationDirectory, CodeHostExeFileName);
            if (!File.Exists(codeHostExe)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Code host executable not found: {0}", databaseExe));
            }

            this.DatabaseExePath = databaseExe;
            this.CodeHostExePath = codeHostExe;

            this.Monitor.Setup();
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
            string eventName;
            EventWaitHandle eventHandle;
            bool databaseRunning;

            eventName = GetDatabaseControlEventName(database);
            try {
                eventHandle = EventWaitHandle.OpenExisting(eventName, EventWaitHandleRights.Synchronize);
                databaseRunning = !eventHandle.WaitOne(0);
                eventHandle.Close();

                if (!databaseRunning) {
                    // Process is shutting down. Wait for shutdown to complete and
                    // restart it.

                    WaitForDatabaseProcessToExit(eventName);
                }
            } catch (WaitHandleCannotBeOpenedException) {
                databaseRunning = false;
            }

            if (!databaseRunning) {
                var startInfo = GetDatabaseStartInfo(database);
                var process = DoStartEngineProcess(startInfo, database);

                // Wait until DM accepts connections before proceeding to make sure that code host
                // doesn't fail to connect later on because DM isn't available.
                //
                // By this time DM will have made sure that there is no duplicate instances and that
                // no other instance is accessing the specific database (thus also confirming that
                // the database exists). Database will not have been loaded into memory however.
                // This is done in parallel to starting up the code host.

                var pipeName = GetDatabaseIpcPipeName(database);
                const int interval = 50;
                const int tries = 100;
                bool operational = false;

                for (int i = 0; i < tries; i++) {
                    // Fails immediatly if no pipe with the specific name. Waits forever if there is
                    // a pipe with the specific name but it is not ready to receive a connection.

                    operational = Win32.WaitNamedPipe(pipeName, -1) != 0;
                    if (operational) break;

                    // Pipe does not exist.

                    process.Refresh();
                    if (process.HasExited) {
                        throw CreateDatabaseTerminated(process, database);
                    }

                    Thread.Sleep(interval);
                }

                if (!operational) {
                    // The process didn't signal in time. Our current strategy
                    // is to log a warning about this and consider the process
                    // operational. If it's not, outer code will notice.
                    ServerLogSources.Default.LogWarning(
                        ErrorCode.ToMessage(Error.SCERRDBPROCNOTSIGNALING,
                        string.Format("{0}. Start time: {1}; time spent waiting: {2}.",
                        FormatDatabaseEngineProcessInfoString(database, process, false),
                        process.StartTime,
                        TimeSpan.FromMilliseconds(interval * tries)))
                        );
                }
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
            var processControlEventName = GetDatabaseControlEventName(database);
            return StopEngineProcess(database, processControlEventName);
        }
        
        private bool StopEngineProcess(Database database, string processControlEventName) {
            EventWaitHandle processControlEvent;
            string errorReason;

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
            return IsEngineProcessRunning(processControlEventName);
        }

        internal bool IsEngineProcessRunning(string processControlEventName) {
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

        internal bool IsCodeHostProcessRunning(Database database) {
            return database.GetRunningCodeHostProcess() != null;
        }

        internal bool StartCodeHostProcess(Database database, out Process process, string commandLineAdditions = null) {
            return StartCodeHostProcess(database, false, false, out process);
        }

        internal bool StartCodeHostProcess(Database database, bool startWithNoDb, bool applyLogSteps, out Process process, string commandLineAdditions = null) {
            process = database.GetRunningCodeHostProcess();
            if (process != null) 
                return false;

            // No process referenced, or the referenced process was not
            // alive. Start a code host process.

            var startInfo = GetCodeHostProcessStartInfo(database, startWithNoDb, applyLogSteps, commandLineAdditions);
            process = DoStartEngineProcess(startInfo, database, (sender, e) => { database.CodeHostErrorOutput.Add(e.Data); });
            process.BeginErrorReadLine();
            database.CodeHostArguments = startInfo.Arguments;
            return true;
        }

        internal void WaitUntilCodeHostOnline(Process codeHostProcess, Database database) {
            // Wait until either the host comes online or until the process
            // terminates, whichever comes first.
            EventWaitHandle online = null;
            var name = string.Concat(DatabaseEngine.ScCodeEvents.OnlineBaseName, database.Name.ToUpperInvariant());

            try {
                while (!codeHostProcess.HasExited) {
                    if (online == null) {
                        if (!EventWaitHandle.TryOpenExisting(name, out online)) {
                            online = null;
                            Thread.Yield();
                        }
                    }

                    if (online != null) {
                        var ready = online.WaitOne(1000);
                        if (ready) break;
                    }

                    codeHostProcess.Refresh();
                }

            } finally {
                if (online != null) {
                    online.Close();
                }
            }

            if (codeHostProcess.HasExited) {
                throw CreateCodeHostTerminated(codeHostProcess, database);
            }
        }

        internal bool StopCodeHostProcess(Database database) {
            var process = Monitor.GetCodeHostProcess(database);
            if (process == null) {
                ResetToCodeHostNotRunning(database);
                return false;
            }

            // The process is alive; we should tell it to shut down and
            // release the reference.

            var serviceUris = CodeHostAPI.CreateServiceURIs(database.Name);

            var response = Http.DELETE("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                serviceUris.Host, (String)null, null); 

            if (!response.IsSuccessStatusCode) {
                // If the host actively refused to shut down, we never try to
                // kill it by force. Instead, we raise an exception that will later
                // be logged, describing this scenario.
                throw ErrorCode.ToException(
                    Error.SCERRCODEHOSTPROCESSREFUSEDSTOP,
                    FormatDatabaseEngineProcessInfoString(database, process));
            }

            // Wait for the user code process to exit. First wait for a short while,
            // them write out a warning that it takes longer than expected. Then wait
            // a little longer, and finally emit an error in the log, before we
            // finally kill the process.
            if (!process.WaitForExit(1000 * 5)) {
                var log = ServerLogSources.Default;
                var infoString = FormatDatabaseEngineProcessInfoString(database, process);
                log.LogWarning("User code process takes longer than expected to exit. ({0})", infoString);
                if (!process.WaitForExit(1000 * 15)) {
                    // Emit the error and kill it.
                    // Suffix the message with the information that we killed it, and
                    // append the info about the database/process.
                    log.LogError(ErrorCode.ToMessage(
                        Error.SCERRCODEHOSTPROCESSNOTEXITED,
                        string.Format("{0}. ({1})", "Killing it.", infoString)).ToString());
                    process.Kill();
                    process.WaitForExit();
                }
            }

            this.Server.GatewayService.UnregisterCodehost(database.Name);

            ResetToCodeHostNotRunning(database);
            SafeClose(process);
            Monitor.EndMonitoring(database);
            return true;
        }

        internal void ResetToCodeHostNotRunning(Database database) {
            database.CodeHostArguments = null;
            database.CodeHostErrorOutput.Clear();
            database.Apps.Clear();
        }

        internal void SafeClose(Process p) {
            try { p.Close(); } catch { }
        }

        internal void QueueCodeHostRestart(Process terminatingCodeHostProcess, Database database, DatabaseInfo databaseInfo) {
            // Before queuing a restart, we first make sure that we
            // reset the current model to reflect the terminating code host
            // and also cancel any monitoring of this database.

            ResetToCodeHostNotRunning(database);
            Monitor.EndMonitoring(database);

            var restartCommand = new ActionCommand<int, DatabaseInfo>(
                this.Server,
                RestartCodeHost,
                terminatingCodeHostProcess.Id,
                databaseInfo,
                "Attempting to restart code host for {0}",
                databaseInfo.Name
                );
            this.Server.CurrentPublicModel.Execute(restartCommand);
        }

        void RestartCodeHost(ICommandProcessor processor, int terminatingCodeHostProcessId, DatabaseInfo databaseInfo) {
            // The database should reflect what applications we want started.

            // Check the database is either not bound to any code host or that
            // its bound to the one found terminating.

            // Get database by name, not by reference.
            // After fetched, it either has to have no reference to any code host
            // OR be attached to the one now terminating (check PID). We restart
            // the applications we've got copied.

            Database database;
            var databaseExist = Server.Databases.TryGetValue(databaseInfo.Name, out database);
            if (!databaseExist) {
                // Might have been deleted.
                // Take no action.
                log.Debug("Restarting of code host cancelled; the database {0} was not found", databaseInfo.Name);
                return;
            }

            var boundProcess = Monitor.GetCodeHostProcess(database);
            if (boundProcess != null) {
                // The database is bound to some other process. We should
                // let it be. We can't predict what could possibly have
                // happened, since clients can have started/restarted/stopped
                // hosts/applications in between.
                
                // Log a notice about this. We want to keep an eye on it if
                // it provokes some unpredicted behaviour.
                log.LogNotice(
                    "Restarting of code host cancelled; {0} has been started in process {1} already", 
                    databaseInfo.Name,
                    boundProcess.Id
                    );
                return;
            }

            // We have the set of applications that we'll try to restart;
            // reset the internal state prior to doing so.

            ResetToCodeHostNotRunning(database);

            Process restartedHost;
            StartCodeHostProcess(database, out restartedHost);
            WaitUntilCodeHostOnline(restartedHost, database);

            try {
                var apps = databaseInfo.Engine.HostedApps;
                var serviceUris = CodeHostAPI.CreateServiceURIs(database.Name);

                foreach (var app in apps) {

                    var restartedApp = new DatabaseApplication(app);
                    restartedApp.IsStartedWithAsyncEntrypoint = true;
                    restartedApp.IsStartedWithTransactEntrypoint = app.TransactEntrypoint;
                    var exe = restartedApp.ToExecutable();

                    if (exe.RunEntrypointAsynchronous) {
                        Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                            serviceUris.Executables, exe.ToJson(), null, (Response resp) => { });
                    } else {
                        var response = Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                            serviceUris.Executables, exe.ToJson(), null);
                        response.FailIfNotSuccess();
                    }

                    restartedApp.Info.LastRestart = DateTime.Now;
                    database.Apps.Add(restartedApp);
                    log.Debug("Restarted application {0} in {1}", app.Name, database.Name);
                }

            } finally {
                var result = Server.CurrentPublicModel.UpdateDatabase(database);
                processor.SetResult(result);
            }
        }

        Process DoStartEngineProcess(ProcessStartInfo startInfo, Database database, DataReceivedEventHandler errorDataRecevied = null) {
            Process p = new Process();
            p.StartInfo = startInfo;
            if (errorDataRecevied != null) {
                p.ErrorDataReceived += errorDataRecevied;
            }

            try {
                p.Start();
            } catch (Exception e) {
                var postfix = string.Format("Engine executable: \"{0}\"", startInfo.FileName);
                ServerLogSources.Default.LogException(e, postfix);
                throw ErrorCode.ToException(Error.SCERRENGINEPROCFAILEDSTART, e, postfix);
            }

            BeginMonitoringEngineProcess(database, p);
            return p;
        }

        void BeginMonitoringEngineProcess(Database database, Process process)
        {
            // Right now, we monitor the code host process only.

            if (process.ProcessName.Equals(StarcounterConstants.ProgramNames.ScCode, StringComparison.InvariantCultureIgnoreCase))
            {
                this.Monitor.BeginMonitoring(database, process);
            }
        }

        void WaitForDatabaseProcessToExit(string processControlEventName) {
            while (IsEngineProcessRunning(processControlEventName)) Thread.Sleep(1);
        }

        private string JSONEncodePath(string path) {
            const char BACK_SLASH = '\\';
            const char SLASH = '/';
            var output = new StringBuilder(path.Length);
            foreach (char c in path) {
                switch (c) {
                case SLASH:
                    output.AppendFormat("{0}{1}", BACK_SLASH, SLASH);
                    break;
                case BACK_SLASH:
                    output.AppendFormat("{0}{0}", BACK_SLASH);
                    break;
                default:
                    output.Append(c);
                    break;
                }
            }
            return output.ToString();
        }

        ProcessStartInfo GetDatabaseStartInfo(Database database) {
            var arguments = new StringBuilder();

            // The syntax
            // scdata.exe [-instid <installationId>] <json>

            arguments.Append("-instid ");
            arguments.Append(database.InstanceID.ToString());
            arguments.Append(' ');

            arguments.Append("\"{");

            // What "host name" value should we use?
            // TODO:

            arguments.Append("\\\"eventloghost\\\":");
            arguments.Append("\\\"");
            arguments.Append(database.Uri);
            //arguments.Append(database.Name);
            arguments.Append("\\\"");
            arguments.Append(',');

            arguments.Append("\\\"eventlogdir\\\":");
            arguments.Append("\\\"");
            arguments.Append(JSONEncodePath(database.Server.Configuration.LogDirectory.TrimEnd('\\')));
            arguments.Append("\\\"");
            arguments.Append(',');

            arguments.Append("\\\"databasename\\\":");
            arguments.Append("\\\"");
            arguments.Append(database.Name.ToUpperInvariant());
            arguments.Append("\\\"");
            arguments.Append(',');

            var runtimeConfig = database.Configuration.Runtime;
            arguments.Append("\\\"databasedir\\\":");
            arguments.Append("\\\"");
            arguments.Append(JSONEncodePath(runtimeConfig.TransactionLogDirectory.TrimEnd('\\')));
            arguments.Append("\\\"");
            //arguments.Append(',');

            arguments.Append("}\"");

            // Support optional log buffer size in configuration too
            // TODO:

            var psi = new ProcessStartInfo(this.DatabaseExePath, arguments.ToString());
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            return psi;
        }

        ProcessStartInfo GetCodeHostProcessStartInfo(Database database, bool startWithNoDb = false, bool applyLogSteps = false, string commandLineAdditions = null) {
            var args = new List<string>(16);
            
            if (Debugger.IsAttached) {
                args.Add("--attachdebugger ");  // Apply to attach a debugger to the boot sequence.
            }

            args.Add(database.InstanceID.ToString() + " ");
            args.Add(database.Name.ToUpper());
            
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.DatabaseDir + "=\"{0}\"", database.Configuration.Runtime.ImageDirectory);
            var transactionLogDirectory = database.Configuration.Runtime.TransactionLogDirectory;
            if (!string.IsNullOrWhiteSpace(transactionLogDirectory)) {
                args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.TransactionLogDirectory + "=\"{0}\"", transactionLogDirectory);
            }

            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.OutputDir + "=\"{0}\"", database.Server.Configuration.LogDirectory);
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.TempDir + "=\"{0}\"", database.Configuration.Runtime.TempDirectory);
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.DefaultUserHttpPort + "={0}", database.Configuration.Runtime.DefaultUserHttpPort);
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.ChunksNumber + "={0}", database.Configuration.Runtime.ChunksNumber);
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.DefaultSessionTimeoutMinutes + "={0}", database.Configuration.Runtime.DefaultSessionTimeoutMinutes);

            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.LoadEditionLibraries) + "={0}", database.Configuration.Runtime.LoadEditionLibraries);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.WrapJsonInNamespaces) + "={0}", database.Configuration.Runtime.WrapJsonInNamespaces);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.EnforceURINamespaces) + "={0}", database.Configuration.Runtime.EnforceURINamespaces);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.MergeJsonSiblings) + "={0}", database.Configuration.Runtime.MergeJsonSiblings);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.XFilePathHeader) + "={0}", database.Configuration.Runtime.XFilePathHeader);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.RequestFiltersEnabled) + "={0}", database.Configuration.Runtime.RequestFiltersEnabled);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.UriMappingEnabled) + "={0}", database.Configuration.Runtime.UriMappingEnabled);
            args.AddFormat(" --" + StarcounterEnvironment.GetFieldName(() => StarcounterEnvironment.OntologyMappingEnabled) + "={0}", database.Configuration.Runtime.OntologyMappingEnabled);

            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.GatewayWorkersNumber + "={0}", StarcounterEnvironment.Gateway.NumberOfWorkers);
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.DefaultSystemHttpPort + "={0}", database.Server.Configuration.SystemHttpPort);
            args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.SQLProcessPort + "={0}", database.Configuration.Runtime.SQLProcessPort);

            if (startWithNoDb) {
                args.Add(" --" + StarcounterConstants.BootstrapOptionNames.NoDb);
            }
            // args.Add(" --" + ProgramCommandLine.OptionNames.NoNetworkGateway);
            if (applyLogSteps) {
                args.Add(" --" + StarcounterConstants.BootstrapOptionNames.EnableTraceLogging);
            }

            if (database.Configuration.Runtime.SchedulerCount.HasValue) {
                args.AddFormat(" --" + StarcounterConstants.BootstrapOptionNames.SchedulerCount + "={0}", database.Configuration.Runtime.SchedulerCount.Value);
            }

            var arguments = args.ToStringFromValues();
            if (!string.IsNullOrEmpty(commandLineAdditions)) {
                var syntax = ProgramCommandLine.Syntax;
                var additions = CommandLineStringParser.SplitCommandLine(commandLineAdditions).ToArray();
                var parser = new Parser(CommandLineStringParser.SplitCommandLine(arguments).ToArray());
                parser.Apply(additions);
                var parsed = parser.Parse(syntax);
                arguments = parsed.ToString("standard");
            }

            var processStart = new ProcessStartInfo(this.CodeHostExePath, arguments.Trim());
            processStart.CreateNoWindow = true;
            processStart.UseShellExecute = false;
            processStart.RedirectStandardInput = true;
            processStart.RedirectStandardOutput = true;
            processStart.RedirectStandardError = true;

            return processStart;
        }

        string GetDatabaseControlEventName(Database database) {
            string processControlEventName = string.Concat(
                ScDataEvents.SC_S2MM_CONTROL_EVENT_NAME_BASE, database.InstanceID
                );
            return processControlEventName;
        }

        string GetDatabaseIpcPipeName(Database database) {
            const string pipeKeyPrefix = "STAR_P_";
            const int ipcKeyBase = 0x53000000;
            int pipeKey = ipcKeyBase + ((int)database.InstanceID << 16);
            string pipeName = string.Format("\\\\.\\pipe\\{0}{1:X}", pipeKeyPrefix, pipeKey);
            return pipeName;
        }

        internal static string FormatDatabaseEngineProcessInfoString(Database database, Process process, bool checkExited = false) {
            string pid;
            string info;

            try {
                pid = process.Id.ToString();
            } catch {
                pid = "N/A";
            }

            // Example: ScCode.exe, PID=123, Database=Foo
            info = string.Format("{0}, PID={1}, Database={2}", Path.GetFileName(process.StartInfo.FileName), pid, database.Name);
            if (checkExited) {
                try {
                    if (process.HasExited) {
                        info += string.Format(", Exitcode={0}", process.ExitCode);
                    }
                } catch { }
            }

            return info;
        }

        internal static Exception CreateCodeHostTerminated(Process codeHostProcess, Database database, Exception serverException = null) {
            // Check the error output: if we can find an error there that
            // match our exit process, we make the swap.
            var errors = new List<string>();
            ParcelledError.ExtractParcelledErrors(database.CodeHostErrorOutput.ToArray(), CodeHostError.ErrorParcelID, errors, 1);
            if (errors.Count == 1) {
                try {
                    var detail = ErrorMessage.Parse(errors[0]);
                    return detail.ToException();
                } catch {
                    // Let the fallback kick in.
                }
            }

            return CreateEngineProcessTerminated(
                codeHostProcess,
                database,
                Error.SCERRDATABASEENGINETERMINATED,
                serverException
                );
        }

        internal static Exception CreateDatabaseTerminated(Process databaseProcess, Database database, Exception serverException = null) {
            return CreateEngineProcessTerminated(
                databaseProcess,
                database,
                Error.SCERRDBPROCTERMINATED,
                serverException
                );
        }

        static Exception CreateEngineProcessTerminated(
            Process engineProcess,
            Database database,
            uint errorCode,
            Exception serverException = null) {
            var exitCode = (uint)GetSafeExitCode(engineProcess);
            var errorPostfix = FormatDatabaseEngineProcessInfoString(database, engineProcess, true);

            // If the exit code indicates anything greater than 1,
            // we construct an inner exception based on the exit code.
            // Exit code 1 indicates manual kiling of the process.
            var inner = exitCode > 1 ?
                ErrorCode.ToException(exitCode, serverException) :
                serverException;

            return ErrorCode.ToException(errorCode, inner, errorPostfix);
        }

        static int GetSafeExitCode(Process p) {
            int code = (int)Error.SCERRPROCESSEXITCODENOTAVAILABLE;
            try {
                code = p.ExitCode;
            } catch { }
            return code;
        }
    }
}