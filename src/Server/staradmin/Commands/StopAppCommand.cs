
using System;

namespace staradmin.Commands {

    internal class StopAppCommand : StopCommand {
        readonly string appReference;

        internal StopAppCommand(string app) {
            appReference = app;
        }

        protected override void Stop() {
            if (string.IsNullOrWhiteSpace(appReference)) {
                var helpOnStop = ShowHelpCommand.CreateAsInternalHelp(FactoryCommand.Info.Name);
                var bad = new ReportBadInputCommand("Specify the application to stop", helpOnStop);
                bad.Execute();
                return;
            }

            Console.WriteLine("Stopping application {0}", appReference);
        }
    }
}