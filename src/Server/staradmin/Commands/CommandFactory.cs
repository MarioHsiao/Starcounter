
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

            if (command != null) {
                return command;
            }

            // Internal commmand that displays an error that the command-line
            // is currently not supported.
            // TODO:

            // Also, introduce a command that can be used when a command detects
            // a parameter set it can't interpret, like 'staradmin show foo'. In
            // such case, we should display that 'staradmin show' does not understand
            // parameter 'foo' and instead display the help on 'staradmin show'.
            // TODO:

            throw new NotImplementedException();
        }

        bool CommandIs(ApplicationArguments args, CommandLine.Command command) {
            var arg = args.Command;
            var comparison = StringComparison.InvariantCultureIgnoreCase;
            return arg.Equals(CommandLine.Commands.Help.Name, comparison);
        }
    }
}