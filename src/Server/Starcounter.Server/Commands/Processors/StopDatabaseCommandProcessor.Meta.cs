
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands.Processors {

    internal sealed partial class StopDatabaseCommandProcessor : CommandProcessor {

        public static int ProcessorToken = CreateToken(typeof(StopDatabaseCommandProcessor));

        public static CommandDescriptor MakeDescriptor() {
            return new CommandDescriptor() {
                ProcessorToken = ProcessorToken,
                CommandDescription = "Stops a database engine.",
                Tasks = new TaskInfo[] { 
                    Task.StopDatabaseProcess.ToPublicModel(), 
                    Task.StopCodeHostProcess.ToPublicModel()
                }
            };
        }

        private static class Task {

            public static readonly CommandTask StopDatabaseProcess = new CommandTask(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopDatabaseProcess,
                "Stopping database process",
                TaskDuration.UnknownWithProgress,
                "Stops the database process. The task is marked as cancelled if the process is found not running."
                );

            internal static readonly CommandTask StopCodeHostProcess = new CommandTask(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopCodeHostProcess,
                "Stopping code host process",
                TaskDuration.UnknownWithProgress,
                "Stops the code host process. The task is marked as cancelled if the process is not running."
                );
        }
    }
}
