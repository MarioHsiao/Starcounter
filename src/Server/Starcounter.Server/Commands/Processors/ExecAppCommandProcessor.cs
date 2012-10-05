

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

            if (!Engine.Databases.TryGetValue(command.DatabaseName, out database)) {
                // Create the database, if not explicitly told otherwise.
                // TODO:
            }

            // If no database, create it.
            //  1) Create a database configuration.
            //  2) Create directories for configuration and data.
            //  3) Create the image- and transaction logs.
            //  4) Add it to the internal model, and the public one.
            //
            //  How do we go about to assure we track creation? Use
            //  something simple, like a leading dot or whatever.
            //
            //  Scheme:
            //  1) Create database directory: "."+ config.Databases + name.
            //  2) Create the configuration, in memory.
            //  3) Create/assure all directories (temp, image, log)
            //  4) Create image- and transaction logs (using scdbc.exe)

            /// throw new NotImplementedException();
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