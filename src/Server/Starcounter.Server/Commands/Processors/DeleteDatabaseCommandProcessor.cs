
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands {

    [CommandProcessor(typeof(DeleteDatabaseCommand))]
    internal sealed class DeleteDatabaseCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="DeleteDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="DeletedDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public DeleteDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            var command = (DeleteDatabaseCommand)this.Command;
            Log.Debug("Asked to deleted database {0}. This is currently not implemented.", command.DatabaseName);
        }
    }
}