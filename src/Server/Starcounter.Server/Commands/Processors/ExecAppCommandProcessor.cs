
using Starcounter.Server.PublicModel.Commands;
using System;

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

            throw new NotImplementedException();
        }
    }
}