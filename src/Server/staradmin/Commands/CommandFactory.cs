
using Starcounter.CLI;
using Starcounter.CommandLine;
using System;
using System.Collections.Generic;

namespace staradmin.Commands {
    /// <summary>
    /// Govers the creation of executable <see cref="ICommand"/> instances
    /// based on given input.
    /// </summary>
    internal class CommandFactory {
        readonly List<IUserCommand> userCommands = new List<IUserCommand>();

        public CommandFactory() {
            userCommands.Add(new KillCommand.UserCommand());
            userCommands.Add(new ShowHelpCommand.UserCommand());
            userCommands.Add(new UnloadCommand.UserCommand());
        }

        /// <summary>
        /// Creates a command based on given arguments.
        /// </summary>
        /// <param name="args">The arguments to use to decied what command
        /// to create.</param>
        /// <returns>A <see cref="ICommand"/> instance.</returns>
        public ICommand CreateCommand(ApplicationArguments args) {
            if (args == ApplicationArguments.Empty || args.ContainsFlag(CommandLine.Options.Help.Name)) {
                return new ShowUsageCommand();
            }

            ICommand command = null;

            var userCommand = userCommands.Find((candidate) => {
                var arg = args.Command;
                var comparison = StringComparison.InvariantCultureIgnoreCase;
                return arg.Equals(candidate.Info.Name, comparison);
            });

            if (userCommand != null) {
                command = userCommand.CreateCommand(args);
            } else {
                var usage = new ShowUsageCommand();
                command = new ReportBadInputCommand(string.Format("Invalid input: '{0}'.", args.ToString("given")), usage);
            }

            var contextCommand = command as ContextAwareCommand;
            if (contextCommand != null) {
                string database;
                SharedCLI.ResolveDatabase(args, out database);

                var context = new Context(args);
                context.Database = database;

                contextCommand.Context = context;
            } 

            return command;
        }
    }
}