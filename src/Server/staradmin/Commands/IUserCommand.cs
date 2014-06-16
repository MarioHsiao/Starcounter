
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;

namespace staradmin.Commands {

    internal interface IUserCommand {
        CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax);
        CommandLine.Command Info { get; }
        ICommand CreateCommand(ApplicationArguments args);
    }
}