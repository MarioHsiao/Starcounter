
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands {

    internal sealed partial class StopExecutableCommandProcessor : CommandProcessor {

        public static int ProcessorToken = CreateToken(typeof(StopExecutableCommandProcessor));

        public static CommandDescriptor MakeDescriptor() {
            return new CommandDescriptor() {
                ProcessorToken = ProcessorToken,
                CommandDescription = "Stops a hosted executable",
                Tasks = new TaskInfo[] { 
                    Task.StopCodeHost.ToPublicModel(), 
                    Task.RestartCodeHost.ToPublicModel(),
                    Task.RestartExecutables.ToPublicModel()
                }
            };
        }

        internal static class Task {

            internal static readonly CommandTask StopCodeHost = new CommandTask(
                StopExecutableCommand.DefaultProcessor.Tasks.StopCodeHost,
                "Stopping code host",
                TaskDuration.NormalIndeterminate,
                "Stops the code host processes, effectively unloading all code."
                );

            internal static readonly CommandTask RestartCodeHost = new CommandTask(
                StopExecutableCommand.DefaultProcessor.Tasks.RestartCodeHost,
                "Restarting code host",
                TaskDuration.NormalIndeterminate,
                "Restarts the code host process."
                );

            internal static readonly CommandTask RestartExecutables = new CommandTask(
                StopExecutableCommand.DefaultProcessor.Tasks.RestartExecutables,
                "Restarting executables",
                TaskDuration.NormalIndeterminate,
                "Restart any possibly fellow executables in the restarted code host."
                );
        }
    }
}