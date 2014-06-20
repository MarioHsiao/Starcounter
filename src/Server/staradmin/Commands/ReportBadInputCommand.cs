using Starcounter.Internal;
using System;

namespace staradmin.Commands {

    internal class ReportBadInputCommand : ICommand {
        readonly string message;
        readonly ICommand afterMessage;

        public ReportBadInputCommand(string msg, ICommand then = null) {
            message = msg;
            afterMessage = then;
        }

        public void Execute() {
            Console.WriteLine(message);

            if (afterMessage != null) {
                Console.WriteLine();
                afterMessage.Execute();
            }

            Environment.ExitCode = (int)Error.SCERRBADCOMMANDLINEFORMAT;
        }
    }
}
