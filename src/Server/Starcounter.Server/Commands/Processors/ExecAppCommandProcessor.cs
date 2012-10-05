
using Starcounter.Server.PublicModel.Commands;
using System;
using System.IO;

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
            : base(server, command)
        {
        }

        /// </inheritdoc>
        protected override void Execute() {
            ExecAppCommand command;
            WeaverService weaver;
            string appRuntimeDirectory;
            string weavedExecutable;
            Database database;

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
            // TODO:

            bool wasStarted = Engine.DatabaseEngine.StartDatabaseProcess(database);
            Console.WriteLine(wasStarted);
            if (wasStarted) {
                wasStarted = Engine.DatabaseEngine.StopDatabaseProcess(database);
                Console.WriteLine(wasStarted);
            }

            // throw new NotImplementedException();
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