
namespace staradmin.Commands {

    /// <summary>
    /// Base class for commands that are aware of the context.
    /// Command classes extending this class will have the context
    /// prior to being exectued.
    /// </summary>
    internal abstract class ContextAwareCommand : ICommand {
        /// <summary>
        /// Gets or sets the context of the current
        /// command.
        /// </summary>
        public Context Context { get; set; }

        /// <inheritdoc />
        public abstract void Execute();
    }
}
