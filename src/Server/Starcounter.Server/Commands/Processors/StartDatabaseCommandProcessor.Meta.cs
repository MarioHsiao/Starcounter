
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Server.Commands.Processors {

    internal sealed partial class StartDatabaseCommandProcessor : CommandProcessor {

        public static int ProcessorToken = CreateToken(typeof(StartDatabaseCommandProcessor));

        public static CommandDescriptor MakeDescriptor() {
            return new CommandDescriptor() {
                ProcessorToken = ProcessorToken,
                CommandDescription = "Starts a database engine.",
                Tasks = new TaskInfo[] { 
                    Task.StartDatabaseProcess.ToPublicModel(), 
                    Task.StartCodeHostProcess.ToPublicModel(),
                    Task.AwaitCodeHostOnline.ToPublicModel(),
                }
            };
        }

        private static class Task {

            public static readonly CommandTask StartDatabaseProcess = new CommandTask(
                StartDatabaseCommand.DefaultProcessor.Tasks.StartDatabaseProcess,
                "Starting database process",
                TaskDuration.UnknownWithProgress,
                "Starts the database process. The task is marked as cancelled if the process is found already running."
                );

            internal static readonly CommandTask StartCodeHostProcess = new CommandTask(
                StartDatabaseCommand.DefaultProcessor.Tasks.StartCodeHostProcess,
                "Starting code host process",
                TaskDuration.UnknownWithProgress,
                "Starts the code host process. The task is marked as cancelled if the process is found already running."
                );

            internal static readonly CommandTask AwaitCodeHostOnline = new CommandTask(
                StartDatabaseCommand.DefaultProcessor.Tasks.AwaitCodeHostOnline,
                "Assuring code host is ready for service",
                TaskDuration.NormalIndeterminate,
                "Awaits the code host boot sequence to get to the point where it accepts requests."
                );
        }
    }
}
