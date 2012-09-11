
using System.Collections.Specialized;

namespace Starcounter.CommandLine
{
    public interface IApplicationInput
    {
        StringDictionary GlobalOptions { get; }
        StringDictionary CommandOptions { get; }
        StringCollection CommandParameters { get; }
        string Command { get; }
        bool HasCommand { get; }
        string GetProperty(string name);
        string GetProperty(string name, CommandLineSection section);
        bool ContainsProperty(string name);
        bool ContainsProperty(string name, CommandLineSection section);
        bool ContainsFlag(string name);
        bool ContainsFlag(string name, CommandLineSection section);
    }
}