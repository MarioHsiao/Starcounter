
namespace Starcounter.Server.Commands {
    /// <summary>
    /// Defines the interface of a server command processor.
    /// </summary>
    public interface ICommandProcessor {
        void SetResult(object result, int? exitCode = null);
    }
}