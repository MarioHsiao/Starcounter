
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System.IO;

namespace staradmin.Commands {

    internal interface IUserCommand {
        CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax);
        CommandLine.Command Info { get; }
        ICommand CreateCommand(ApplicationArguments args);
        void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer); 
    }
}