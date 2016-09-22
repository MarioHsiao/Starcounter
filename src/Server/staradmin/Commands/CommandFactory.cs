
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
        public static readonly List<IUserCommand> UserCommands = new List<IUserCommand>();

        static CommandFactory() {
            UserCommands.Add(new KillCommand.UserCommand());
            UserCommands.Add(new ShowHelpCommand.UserCommand());
            UserCommands.Add(new UnloadCommand.UserCommand());
            UserCommands.Add(new ReloadCommand.UserCommand());
            UserCommands.Add(new ListCommand.UserCommand());
            UserCommands.Add(new ConsoleCommand.UserCommand());
            UserCommands.Add(new NewCommand.UserCommand());
            UserCommands.Add(new StartCommand.UserCommand());
            UserCommands.Add(new StopCommand.UserCommand());
            UserCommands.Add(new DeleteCommand.UserCommand());
            UserCommands.Add(new TestCommand.UserCommand());
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

            var userCommand = UserCommands.Find((candidate) => {
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
                string serverHost;
                int serverPort;
                string ignored;

                var explicitDatabase = SharedCLI.ResolveDatabase(args, out database);
                SharedCLI.ResolveAdminServer(args, out serverHost, out serverPort, out ignored);
 
                var context = new Context(args);
                context.Database = database;
                context.IsExcplicitDatabase = explicitDatabase;
                context.ServerHost = serverHost;
                context.ServerPort = serverPort;

                contextCommand.Context = context;
            }

            return command;
        }
    }
}