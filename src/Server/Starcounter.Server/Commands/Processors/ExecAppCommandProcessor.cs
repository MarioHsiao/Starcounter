
using Starcounter.Server.PublicModel.Commands;
using System;
using System.IO;
using System.Diagnostics;
using Starcounter.ABCIPC;

namespace Starcounter.Server.Commands {

    /// <summary>
    /// Executes a queued and dispatched <see cref="ExecAppCommand"/>.
    /// </summary>
    [CommandProcessor(typeof(ExecAppCommand))]
    internal sealed class ExecAppCommandProcessor : CommandProcessor {
        
        /// <summary>
        /// Initializes a new <see cref="ExecAppCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="ExecAppCommand"/> the
        /// processor should exeucte.</param>
        public ExecAppCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// </inheritdoc>
        protected override void Execute() {
            ExecAppCommand command;
            WeaverService weaver;
            string appRuntimeDirectory;
            string weavedExecutable;
            Database database;
            DatabaseApp app;
            Process workerProcess;
            bool databaseExist;
            
            // First see if we can find the database and take a look what
            // code is running inside it. We don't want to process the same
            // executable twice.

            command = (ExecAppCommand)this.Command;
            databaseExist = Engine.Databases.TryGetValue(command.DatabaseName, out database);
            if (databaseExist) {
                app = database.Apps.Find(delegate(DatabaseApp candidate) {
                    return candidate.OriginalExecutablePath.Equals(
                        command.AssemblyPath, StringComparison.InvariantCultureIgnoreCase);
                });
                if (app != null) {
                    // Running the same executable more than once is not considered an
                    // error. We just log it as a notice and consider the processing done.
                    Log.LogNotice("Executable {0} is already running in database {1}.", app.OriginalExecutablePath, command.DatabaseName);
                    return;
                }
            }

            // The application doesn't run inside the database, or the database
            // doesn't exist. Process furhter: weaving first.

            // Make sure we respect the (temporary) NoDb switch if applied.

            if (command.NoDb) {
                // TODO PSA:
                // We most likely want to copy all binaries to a temporary runtime
                // directory anyway, since with the current approach, we will lock
                // future builds from succeeding if the host process keeps any of
                // the binaries loaded.
                appRuntimeDirectory = Path.GetDirectoryName(command.AssemblyPath);
                weavedExecutable = command.AssemblyPath;
            } else {
                appRuntimeDirectory = GetAppRuntimeDirectory(this.Engine.Configuration.TempDirectory, command.AssemblyPath);
                weaver = Engine.WeaverService;
                weavedExecutable = weaver.Weave(command.AssemblyPath, appRuntimeDirectory);
            }

            // Create the database if it does not exist and if not told otherwise.
            // Add it to our internal model as well as to the public one.
            if (!databaseExist) {
                var setup = new DatabaseSetup(this.Engine, new DatabaseSetupProperties(this.Engine, command.DatabaseName));
                database = setup.CreateDatabase();
                Engine.Databases.Add(database.Name, database);
                Engine.CurrentPublicModel.AddDatabase(database);
            }

            // Assure the database is started and that there is user code worker
            // process on top of it where we can inject the booting executable.

            Engine.DatabaseEngine.StartDatabaseProcess(database);
            Engine.DatabaseEngine.StartWorkerProcess(database, out workerProcess);
            
            // Get a client handle to the worker process.

            var client = new Client(workerProcess.StandardInput.WriteLine, workerProcess.StandardOutput.ReadLine);

            // The current database worker protocol is "Exec c:\myfile.exe". We use
            // that until the full one is in place.
            //   Grab the response message and utilize it if we fail.

            string responseMessage = string.Empty;
            bool success = client.Send("Exec", string.Format("\"{0}\"", weavedExecutable), delegate(Reply reply) {
                if (reply.IsResponse && !reply.IsSuccess) {
                    reply.TryGetCarry(out responseMessage);
                }
            });
            if (!success) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, responseMessage);
            }

            // The app is successfully loaded in the worker process. We should
            // keep it referenced in the server and consider the execution of this
            // processor a success.
            app = new DatabaseApp() {
                OriginalExecutablePath = command.AssemblyPath,
                WorkingDirectory = command.WorkingDirectory,
                Arguments = command.Arguments,
                ExecutionPath = weavedExecutable
            };
            database.Apps.Add(app);
            Engine.CurrentPublicModel.UpdateDatabase(database);
        }

        string GetAppRuntimeDirectory(string baseDirectory, string assemblyPath) {
            string key;
            string originalDirectory;

            originalDirectory = Path.GetFullPath(Path.GetDirectoryName(assemblyPath));
            key = originalDirectory.Replace(Path.DirectorySeparatorChar, '@').Replace(Path.VolumeSeparatorChar, '@').Replace(" ", "");
            key += ("@" + Path.GetFileNameWithoutExtension(assemblyPath));

            return Path.Combine(baseDirectory, key);
        }
    }
}