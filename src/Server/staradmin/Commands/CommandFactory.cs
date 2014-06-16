
using Starcounter.CLI;
using Starcounter.CommandLine;
using System;

namespace staradmin.Commands {
    /// <summary>
    /// Govers the creation of executable <see cref="ICommand"/> instances
    /// based on given input.
    /// </summary>
    internal class CommandFactory {
        
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

            if (CommandIs(args, CommandLine.Commands.Help)) {
                var topic = args.CommandParameters.Count == 0 ? CommandLine.Commands.Help.Name : args.CommandParameters[0];
                command = new ShowHelpCommand(topic);
            }

            if (CommandIs(args, CommandLine.Commands.Kill)) {
                var target = args.CommandParameters[0];
                command = new KillCommand(target);
            }

            if (CommandIs(args, CommandLine.Commands.Unload)) {
                var source = args.CommandParameters.Count == 0 ? string.Empty : args.CommandParameters[0];
                command = new UnloadCommand(source);
            }

            if (command == null) {
                var usage = new ShowUsageCommand();
                command = new ReportBadInputCommand(string.Format("Invalid input: '{0}'.", args.ToString("given")), usage);
            }

            var contextCommand = command as ContextAwareCommand;
            if (contextCommand != null) {
                string database;
                SharedCLI.ResolveDatabase(args, out database);

                var context = new Context();
                context.Database = database;

                contextCommand.Context = context;
            } 

            return command;
        }

        bool CommandIs(ApplicationArguments args, CommandLine.Command command) {
            var arg = args.Command;
            var comparison = StringComparison.InvariantCultureIgnoreCase;
            return arg.Equals(command.Name, comparison);
        }
    }
}