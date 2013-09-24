using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands {

    /// <summary>
    /// Executes a queued and dispatched <see cref="StopExecutableCommand"/>.
    /// </summary>
    [CommandProcessor(typeof(StopExecutableCommand))]
    internal sealed partial class StopExecutableCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="StopExecutableCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="StopExecutableCommand"/> the
        /// processor should exeucte.</param>
        public StopExecutableCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            var command = this.Command as StopExecutableCommand;
            Log.Debug(command.Executable);
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }
    }
}
