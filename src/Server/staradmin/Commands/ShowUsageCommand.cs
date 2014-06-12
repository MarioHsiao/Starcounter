using System;

namespace staradmin.Commands {

    internal class ShowUsageCommand : ICommand {

        public void Execute() {
            CommandLine.WriteUsage(Console.Out);
        }
    }
}