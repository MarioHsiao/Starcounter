
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
            Process workerProcess;

            // Weave first.

            command = (ExecAppCommand)this.Command;
            appRuntimeDirectory = GetAppRuntimeDirectory(this.Engine.Configuration.TempDirectory, command.AssemblyPath);
            weaver = Engine.WeaverService;
            weavedExecutable = weaver.Weave(command.AssemblyPath, appRuntimeDirectory);

            // Try getting the database from our internal model

            if (!Engine.Databases.TryGetValue(command.DatabaseName, out database)) {
                // Create the database, if not explicitly told otherwise.
                // Add it to our internal model as well as to the public
                // one.
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