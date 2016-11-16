
using Starcounter.Advanced;
using Starcounter.Bootstrap.Management;
using Starcounter.Internal;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Starcounter.Server.Commands {

    /// <summary>
    /// Executes a queued and dispatched <see cref="StartExecutableCommand"/>.
    /// </summary>
    [CommandProcessor(typeof(StartExecutableCommand))]
    internal sealed partial class StartExecutableCommandProcessor : CommandProcessor {

        /// <summary>
        /// Initializes a new <see cref="StartExecutableCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="StartExecutableCommand"/> the
        /// processor should exeucte.</param>
        public StartExecutableCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            StartExecutableCommand command;
            WeaverService weaver;
            string appRuntimeDirectory;
            string weavedExecutable;
            Database database;
            DatabaseApplication app;
            Process codeHostProcess;
            bool databaseExist;

            command = (StartExecutableCommand)this.Command;
            databaseExist = false;
            weavedExecutable = null;
            database = null;
            codeHostProcess = null;

            if (!File.Exists(command.Application.BinaryFilePath)) {
                throw ErrorCode.ToException(
                    Error.SCERREXECUTABLENOTFOUND, string.Format("File: {0}", command.Application.BinaryFilePath));
            }

            if (string.IsNullOrWhiteSpace(command.Application.Name)) {
                throw ErrorCode.ToException(Error.SCERRMISSINGAPPLICATIONNAME);
            }

            databaseExist = Engine.Databases.TryGetValue(command.DatabaseName, out database);
            if (!databaseExist) {
                throw ErrorCode.ToException(
                    Error.SCERRDATABASENOTFOUND, string.Format("Database: {0}", command.DatabaseName)
                    );
            }

            app = database.Apps.Find((candidate) => {
                return candidate.Info.EqualApplicationFile(command.Application);
            });
            if (app != null) {
                throw ErrorCode.ToException(
                    Error.SCERREXECUTABLEALREADYRUNNING,
                    string.Format("Application {0} is already running in database {1}.", command.Application.FilePath, command.DatabaseName)
                    );
            }

            app = database.Apps.Find((candidate) => {
                return candidate.Info.Name.Equals(command.Application.Name, StringComparison.InvariantCultureIgnoreCase);
            });
            if (app != null) {
                throw ErrorCode.ToException(
                    Error.SCERRAPPLICATIONALREADYRUNNING, string.Format("Name \"{0}\".", command.Application.Name));
            }

            codeHostProcess = database.GetRunningCodeHostProcess();
            if (codeHostProcess == null) {
                throw ErrorCode.ToException(
                    Error.SCERRDATABASEENGINENOTRUNNING,
                    string.Format("Database {0}.", command.DatabaseName)
                    );
            }

            var exeKey = Engine.ExecutableService.CreateKey(command.Application.FilePath);
            WithinTask(Task.PrepareExecutable, (task) => {
                weaver = Engine.WeaverService;
                appRuntimeDirectory = Path.Combine(database.ExecutableBasePath, exeKey);

                if (command.NoDb) {
                    weavedExecutable = CopyAllFilesToRunNoDbApplication(command.Application.BinaryFilePath, appRuntimeDirectory);
                    OnAssembliesCopiedToRuntimeDirectory();
                } else {
                    weavedExecutable = weaver.Weave(
                        command.Application.BinaryFilePath,
                        appRuntimeDirectory);

                    OnWeavingCompleted();
                }
            });

            WithinTask(Task.Run, (task) => {
                var serviceUris = CodeHostAPI.CreateServiceURIs(database.Name);
                
                var databaseStateSnapshot = database.ToPublicModel();
                command.Application.Key = exeKey;
                command.Application.HostedFilePath = weavedExecutable;
                
                app = new DatabaseApplication(command.Application);
                app.IsStartedWithAsyncEntrypoint = command.RunEntrypointAsynchronous;
                app.IsStartedWithTransactEntrypoint = command.TransactEntrypoint;
                var exe = app.ToExecutable();

                // It's within the below scope the database model might
                // be affected - either by the successfull starting of a
                // new application, or a terminating, faulty code host.
                // Make sure to updat the public model no matter what
                // (see the finally-clause)

                Exception codeHostExited = null;
                try {

                    if (exe.RunEntrypointAsynchronous) {

                        // Just make the asynchronous call and be done with it
                        // We never check anything more.
                        Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                            serviceUris.Executables, exe.ToJson(), null, (Response resp) => { });

                    } else {
                        // Make a asynchronous call, where we let the callback
                        // set the event whenever the code host is done. Until
                        // then, we wait for this, and check that the code host
                        // is running periodically.
                        var confirmed = new ManualResetEvent(false);
                        Response codeHostResponse = null;

                        Http.POST("http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + 
                            serviceUris.Executables, exe.ToJson(), null, (Response resp) => {

                            codeHostResponse = resp;
                            confirmed.Set();
                        });

                        var timeout = 500;
                        while (!confirmed.WaitOne(timeout)) {
                            codeHostExited = CreateExceptionIfCodeHostTerminated(codeHostProcess, database);
                            if (codeHostExited != null) {
                                throw codeHostExited;
                            }

                            timeout = 20;
                        }

                        confirmed.Dispose();
                        codeHostExited = CreateExceptionIfCodeHostTerminated(codeHostProcess, database);
                        if (codeHostExited != null) {
                            throw codeHostExited;
                        }
                        codeHostResponse.FailIfNotSuccess();
                    }
                    OnCodeHostExecRequestProcessed();

                    app.Info.Started = DateTime.Now;
                    database.Apps.Add(app);
                    OnDatabaseAppRegistered();

                } catch (Exception ex) {
                    if (codeHostExited != null && ex.Equals(codeHostExited)) {
                        Engine.DatabaseEngine.QueueCodeHostRestart(codeHostProcess, database, databaseStateSnapshot);
                        throw;
                    }

                    codeHostExited = CreateExceptionIfCodeHostTerminated(codeHostProcess, database, ex);
                    if (codeHostExited != null) {
                        Engine.DatabaseEngine.QueueCodeHostRestart(codeHostProcess, database, databaseStateSnapshot);
                        throw codeHostExited;
                    }

                    throw;

                } finally {
                    var result = Engine.CurrentPublicModel.UpdateDatabase(database);
                    SetResult(result);
                    OnDatabaseStatusUpdated();
                }
            });
        }



        Exception CreateExceptionIfCodeHostTerminated(Process codeHostProcess, Database database, Exception ex = null) {
            Exception result = null;
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

                result = DatabaseEngine.CreateCodeHostTerminated(codeHostProcess, database, ex);
            }

            return result;
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
        void OnCodeHostExecRequestProcessed() { Trace("Code host Exec request processed."); }
        void OnDatabaseAppRegistered() { Trace("Database app registered."); }
        void OnDatabaseStatusUpdated() { Trace("Database status updated."); }
    }
}