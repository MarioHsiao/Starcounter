// ***********************************************************************
// <copyright file="ExecAppCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.ABCIPC;
using Starcounter.Internal;
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
    internal sealed class ExecCommandProcessor : CommandProcessor {
        
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
            
            // First see if we can find the database and take a look what
            // code is running inside it. We don't want to process the same
            // executable twice.

            command = (ExecCommand)this.Command;
            databaseExist = Engine.Databases.TryGetValue(command.DatabaseName, out database);
            if (databaseExist) {
                app = database.Apps.Find(delegate(DatabaseApp candidate) {
                    return candidate.OriginalExecutablePath.Equals(
                        command.ExecutablePath, StringComparison.InvariantCultureIgnoreCase);
                });
                if (app != null) {
                    // If the app is running inside the database, we must stop the host,
                    // or validate it's up-to-date.
                    // We currently dont implement checking if the app is up-to-date,
                    // we simply restart the host every time.
                    // Two TODO's here:
                    // 1) Make sure we restart other apps that runs in the same host if
                    // we drop the host.
                    // 2) Check if the app is up-to-date.
                    if (IsUpToDate(app)) {
                        // Running the same executable more than once is not considered an
                        // error. We just log it as a notice and consider the processing done.
                        this.Log.LogNotice("Executable {0} is already running in database {1}.", app.OriginalExecutablePath, command.DatabaseName);
                        return;
                    }

                    Engine.DatabaseEngine.StopCodeHostProcess(database);

                    OnExistingWorkerProcessStopped();
                }
            }

            // Create the database if it does not exist and if not told otherwise.
            // Add it to our internal model as well as to the public one.
            if (!databaseExist) {
                var setup = new DatabaseSetup(this.Engine, new DatabaseSetupProperties(this.Engine, command.DatabaseName));
                database = setup.CreateDatabase();

                OnDatabaseCreated();

                Engine.Databases.Add(database.Name, database);
                Engine.CurrentPublicModel.AddDatabase(database);

                OnDatabaseRegistered();
            }

            // Assure the database is started and that there is user code worker
            // process on top of it where we can inject the booting executable.

            Engine.DatabaseEngine.StartDatabaseProcess(database);

            OnDatabaseProcessStarted();

            Engine.DatabaseEngine.StartCodeHostProcess(database, command.NoDb, command.LogSteps, out codeHostProcess);

            OnWorkerProcessStarted();

            // The application doesn't run inside the database, or the database
            // doesn't exist. Process further: weaving first.
            // (Make sure we respect the (temporary) NoDb switch if applied).

            weaver = Engine.WeaverService;
            appRuntimeDirectory = weaver.CreateFullRuntimePath(
                Path.Combine(database.Configuration.Runtime.TempDirectory, StarcounterEnvironment.Directories.WeaverTempSubDirectory),
                command.ExecutablePath);

            if (command.NoDb)
            {
                weavedExecutable = CopyAllFilesToRunNoDbApplication(command.ExecutablePath, appRuntimeDirectory);

                OnAssembliesCopiedToRuntimeDirectory();
            }
            else
            {
                weavedExecutable = weaver.Weave(command.ExecutablePath, appRuntimeDirectory);

                OnWeavingCompleted();
            }

            // Get a client handle to the hosting process.

            var client = this.Engine.DatabaseHostService.GetHostingInterface(database);

            OnHostingInterfaceConnected();

            try {
                if (command.PrepareOnly) {
                    bool success = client.Send("Ping");
                    if (!success) {
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
                    }

                    OnPingRequestProcessed();
                } else {

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
                        ExecutionPath = weavedExecutable
                    };
                    database.Apps.Add(app);

                    OnDatabaseAppRegistered();
                }

            } catch (TimeoutException timeout) {
                // When we experience a timeout, we can try to check if the
                // process is still alive. If not, it might have crashed.
                // Else, we should indicate that the timeout time can be adjusted
                // by means of config?

                codeHostProcess.Refresh();
                if (codeHostProcess.HasExited) {
                    // The code host has exited, most likely because something
                    // in the bootstrap sequence or in the exec handler has gone
                    // wrong. We count on the code host logging the exact reason,
                    // but just in case it fails to do so, we log this from the
                    // perspective of the server, including the exit code.
                    //   We also don't try to amend this right now, since we don't
                    // have any good strategy figured out. We start with just
                    // logging it and nothing else.

                    Log.LogError(
                        ErrorCode.ToMessage(Error.SCERRDATABASEENGINETERMINATED,
                        DatabaseEngine.FormatCodeHostProcessInfoString(database, codeHostProcess, true))
                        );
                }

                // We always rethrow the timeout exception, since we really can't
                // handle it on this level - we let the server decide how to go
                // further.

                throw timeout;
            }

            Engine.CurrentPublicModel.UpdateDatabase(database);

            OnDatabaseStatusUpdated();
        }

        bool IsUpToDate(DatabaseApp app) {
            return false;
        }

        string GetAppRuntimeDirectory(string baseDirectory, string assemblyPath) {
            string key;
            string originalDirectory;

            originalDirectory = Path.GetFullPath(Path.GetDirectoryName(assemblyPath));
            key = originalDirectory.Replace(Path.DirectorySeparatorChar, '@').Replace(Path.VolumeSeparatorChar, '@').Replace(" ", "");
            key += ("@" + Path.GetFileNameWithoutExtension(assemblyPath));

            return Path.Combine(baseDirectory, key);
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