
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands {
    
    [CommandProcessor(typeof(CreateDatabaseCommand))]
    internal sealed class CreateDatabaseCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="CreateDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="CreateDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public CreateDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// </inheritdoc>
        protected override void Execute() {
            throw new NotImplementedException();
        }
    }
}