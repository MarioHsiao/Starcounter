
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands
{
    [CommandProcessor(typeof(RegisterHostCommand))]
    internal sealed class RegisterHostCommandProcessor : CommandProcessor
    {
        public RegisterHostCommandProcessor(ServerEngine server, ServerCommand command) : base(server, command) {
        }

        protected override void Execute()
        {
            Execute((RegisterHostCommand)Command);
        }

        void Execute(RegisterHostCommand command)
        {
            // Register the specified host.
            // TODO:
        }
    }
}
