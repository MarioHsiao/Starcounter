using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands.Processors {
    
    [CommandProcessor(typeof(StopDatabaseCommand))]
    internal sealed class StopDatabaseCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="StopDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="StopDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public StopDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// </inheritdoc>
        protected override void Execute() {
            throw new NotImplementedException();
        }
    }
}