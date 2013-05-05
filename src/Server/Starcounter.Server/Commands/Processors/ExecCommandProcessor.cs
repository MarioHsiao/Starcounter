// ***********************************************************************
// <copyright file="ExecAppCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.ABCIPC;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Starcounter.Server.Commands {

    /// <summary>
    /// Executes a queued and dispatched <see cref="ExecCommand"/>.
    /// </summary>
    [CommandProcessor(typeof(ExecCommand))]
    internal sealed partial class ExecCommandProcessor : CommandProcessor {

        /// <summary>
        /// Initializes a new <see cref="ExecCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="ExecCommand"/> the
        /// processor should exeucte.</param>
        public ExecCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            ExecCommand command;
            WeaverService weaver;
            string appRuntimeDirectory;
            string weavedExecutable;
            Database database;
            DatabaseApp app;
            Process codeHostProcess;
            bool databaseExist;

            Diagnostics.WriteTimeStamp("SERVER", "Execute()");

            command = (ExecCommand)this.Command;
            databaseExist = false;
            weavedExecutable = null;
            database = null;
            codeHostProcess = null;

            if (!File.Exists(command.ExecutablePath)) {
                throw ErrorCode.ToException(
                    Error.SCERREXECUTABLENOTFOUND, string.Format("File: {0}", command.ExecutablePath));
            }

            databaseExist = Engine.Databases.TryGetValue(command.DatabaseName, out database);
            if (!databaseExist) {
                throw ErrorCode.ToException(
                    Error.SCERRDATABASENOTFOUND, string.Format("Database: {0}", command.DatabaseName)
                    );
            }

            app = database.Apps.Find(delegate(DatabaseApp candidate) {
                return candidate.OriginalExecutablePath.Equals(command.ExecutablePath, StringComparison.InvariantCultureIgnoreCase);
            });
            if (app != null) {
                throw ErrorCode.ToException(
                    Error.SCERREXECUTABLEALREADYRUNNING,
                    string.Format("Executable {0} is already running in engine {1}.", command.ExecutablePath, command.DatabaseName)
                    );
            }

            codeHostProcess = database.GetRunningCodeHostProcess();
            if (codeHostProcess == null) {
                throw ErrorCode.ToException(
                    Error.SCERRDATABASEENGINENOTRUNNING,
                    string.Format("Database {0}.", command.DatabaseName)
                    );
            }

            var exeKey = Engine.ExecutableService.CreateKey(command.ExecutablePath);
            WithinTask(Task.PrepareExecutable, (task) => {
                weaver = Engine.WeaverService;
                appRuntimeDirectory = Path.Combine(database.ExecutableBasePath, exeKey);

                if (command.NoDb) {
                    weavedExecutable = CopyAllFilesToRunNoDbApplication(command.ExecutablePath, appRuntimeDirectory);
                    OnAssembliesCopiedToRuntimeDirectory();
                } else {
                    weavedExecutable = weaver.Weave(command.ExecutablePath, appRuntimeDirectory);
                    OnWeavingCompleted();
                }
            });

            var client = this.Engine.DatabaseHostService.GetHostingInterface(database);
            OnHostingInterfaceConnected();

            WithinTask(Task.Run, (task) => {
                try {

                    // The current database worker protocol is "Exec c:\myfile.exe". We use
                    // that until the full one is in place.
                    //   Grab the response message and utilize it if we fail.

                    var properties = new Dictionary<string, string>();
                    properties.Add("AssemblyPath", weavedExecutable);
                    properties.Add("WorkingDir", command.WorkingDirectory);
                    properties.Add("Args", KeyValueBinary.FromArray(command.ArgumentsToApplication).Value);

                    string responseMessage = string.Empty;
                    bool success = client.Send("Exec2", properties, delegate(Reply reply) {
                        if (reply.IsResponse && !reply.IsSuccess) {
                            reply.TryGetCarry(out responseMessage);
                        }
                    });
                    if (!success) {
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, responseMessage);
                    }

                    OnExec2RequestProcessed();

                    // The app is successfully loaded in the worker process. We should
                    // keep it referenced in the server and consider the execution of this
                    // processor a success.
                    app = new DatabaseApp() {
                        OriginalExecutablePath = command.ExecutablePath,
                        WorkingDirectory = command.WorkingDirectory,
                        Arguments = command.Arguments,
                        ExecutionPath = weavedExecutable,
                        Key = exeKey
                    };
                    database.Apps.Add(app);

                    OnDatabaseAppRegistered();

                } catch (Exception ex) {
                    // When we experience a timeout, we can try to check if the
                    // process is still alive. If not, it might have crashed.
                    // Else, we should indicate that the timeout time can be adjusted
                    // by means of config?
                    codeHostProcess.Refresh();
                    if (codeHostProcess.HasExited) {
                        // The code host has exited, most likely because something
                        // in the bootstrap sequence or in the exec handler has gone
                        // wrong. 
                        // We count on the code host logging the exact reason,
                        // but just in case it fails to do so, we log this from the
                        // perspective of the server, including the exit code which 
                        // should be a proper starcounter errorcode.
                        // We also don't try to amend this right now, since we don't
                        // have any good strategy figured out. We start with just
                        // logging it and nothing else.

                        Exception inner = null;
                        uint processExitCode = (uint)codeHostProcess.ExitCode;
                        string errorPostPrefix = DatabaseEngine.FormatCodeHostProcessInfoString(database, codeHostProcess, true);

                        if (processExitCode > 1) { // 1 is the default return value if process is killed manually.
                            inner = ErrorCode.ToException(processExitCode);
                        }
                        throw ErrorCode.ToException(Error.SCERRDATABASEENGINETERMINATED, inner, errorPostPrefix);
                    }
                    throw ex;
                }
            });

            var result = Engine.CurrentPublicModel.UpdateDatabase(database);
            SetResult(result);

            OnDatabaseStatusUpdated();
        }

        /// <summary>
        /// Adapts to the (temporary) NoDb switch by copying all binary
        /// files possibly referenced by the starting assembly, as given
        /// by <paramref name="assemblyPath"/>, including the starting
        /// assembly itself.
        /// </summary>
        /// <param name="assemblyPath">Full path to the original assembly,
        /// i.e. the assembly we are told to execute.</param>
        /// <param name="runtimeDirectory">The runtime directory where the
        /// assembly will actually run from, when hosted in Starcounter.</param>
        /// <returns>Full path to the assembly that is about to be executed.
        /// </returns>
        string CopyAllFilesToRunNoDbApplication(string assemblyPath, string runtimeDirectory) {
            #region Copying of a single binary + it's symbol file (i.e. pdb)
            Action<string, string> copyBinary = (string sourceFile, string targetDirectory) => {
                string sourceDirectory;
                string fileNameNoExtension;
                string symbolFileName;
                string sourceSymbolFile;

                sourceDirectory = Path.GetDirectoryName(sourceFile);
                fileNameNoExtension = Path.GetFileNameWithoutExtension(sourceFile);
                symbolFileName = string.Concat(fileNameNoExtension, ".pdb");
                sourceSymbolFile = Path.Combine(sourceDirectory, symbolFileName);

                File.Copy(sourceFile, Path.Combine(targetDirectory, Path.GetFileName(sourceFile)), true);
                if (File.Exists(sourceSymbolFile)) {
                    File.Copy(sourceSymbolFile, Path.Combine(targetDirectory, symbolFileName), true);
                }
            };
            #endregion

            Directory.CreateDirectory(runtimeDirectory);

            var extensions = new string[] { ".dll", ".exe" };
            foreach (var extension in extensions) {
                foreach (var item in Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*" + extension, SearchOption.TopDirectoryOnly)) {
                    if (item.EndsWith(".vshost.exe"))
                        continue;

                    copyBinary(item, runtimeDirectory);
                }
            }

            return Path.Combine(runtimeDirectory, Path.GetFileName(assemblyPath));
        }

        void OnExistingWorkerProcessStopped() { Trace("Existing worker process stopped."); }
        void OnAssembliesCopiedToRuntimeDirectory() { Trace("Assemblies copied to runtime directory."); }
        void OnWeavingCompleted() { Trace("Weaving completed."); }
        void OnDatabaseCreated() { Trace("Database created."); }
        void OnDatabaseRegistered() { Trace("Database registered."); }
        void OnDatabaseProcessStarted() { Trace("Database process started."); }
        void OnWorkerProcessStarted() { Trace("Worker process started."); }
        void OnHostingInterfaceConnected() { Trace("Hosting interface connected."); }
        void OnPingRequestProcessed() { Trace("Ping request processed."); }
        void OnExec2RequestProcessed() { Trace("Exec2 request processed."); }
        void OnDatabaseAppRegistered() { Trace("Database app registered."); }
        void OnDatabaseStatusUpdated() { Trace("Database status updated."); }
    }
}