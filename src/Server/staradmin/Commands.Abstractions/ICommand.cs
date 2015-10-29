
namespace staradmin.Commands {
    /// <summary>
    /// Defines the interface of a command that is possible to
    /// execute within the scope of the staradmin CLI.
    /// </summary>
    internal interface ICommand {
        void Execute();
    }
}