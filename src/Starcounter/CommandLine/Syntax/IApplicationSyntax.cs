
namespace Starcounter.CommandLine.Syntax
{
    public interface IApplicationSyntax
    {
        bool RequiresCommand { get; }
        string DefaultCommand { get; }
        string ProgramDescription { get; }
        OptionInfo[] Properties { get; }
        OptionInfo[] Flags { get; }
        ICommandSyntax[] Commands { get; }
    }
}
