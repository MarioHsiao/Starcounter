
namespace Starcounter.CommandLine.Syntax
{
    public interface ICommandSyntax
    {
        string Name { get; }
        string CommandDescription { get; }
        int? MinParameterCount { get; }
        int? MaxParameterCount { get; }
        OptionInfo[] Properties { get; }
        OptionInfo[] Flags { get; }
    }
}
