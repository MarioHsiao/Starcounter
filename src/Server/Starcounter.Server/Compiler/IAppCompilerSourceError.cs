namespace Starcounter.Server.Compiler
{
    /// <summary>
    /// Defines an error raised by the underlying compiler when
    /// trying to compile an application.
    /// </summary>
    public interface IAppCompilerSourceError
    {
        /// <summary>
        /// Gets the error identity.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the error description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the file the error stem from, if applicable.
        /// </summary>
        string File { get; }

        /// <summary>
        /// Gets the file line the error stem from, if applicable.
        /// </summary>
        int Line { get; }

        /// <summary>
        /// Gets the file column the error stem from, if applicable.
        /// </summary>
        int Column { get; }
    }
}
