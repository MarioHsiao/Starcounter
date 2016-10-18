
using Starcounter.Server.PublicModel.Commands;
using System.Diagnostics;

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
            Database database;
            if (!this.Engine.Databases.TryGetValue(command.DatabaseName, out database))
            {
                throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND, $"Database: '{command.DatabaseName}'.");
            }

            var hostProcess = Process.GetProcessById(command.ProcessId);
            if (hostProcess == null)
            {
                throw ErrorCode.ToException(Error.SCERRHOSTPROCESSNOTRUNNING, $"PID={command.ProcessId}, Database: '{command.DatabaseName}'.");
            }

            Engine.DatabaseEngine.Monitor.BeginMonitoring(database, hostProcess);
        }
    }
}
