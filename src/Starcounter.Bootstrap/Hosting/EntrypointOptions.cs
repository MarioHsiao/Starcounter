namespace Starcounter.Hosting
{
    /// <summary>
    /// Define how the runtime host should execute an entrypoint
    /// method.
    /// </summary>
    public enum EntrypointOptions
    {
        /// <summary>
        /// Assure an entrypoint can be found and run it synchronously.
        /// </summary>
        RunSynchronous,

        /// <summary>
        /// Assure an entrypoint can be found and run it asynchronously.
        /// </summary>
        RunAsynchronous,

        /// <summary>
        /// Don't look for an entrypoint at all.
        /// </summary>
        /// <remarks>
        /// Used in self-hosting scenarios where the OS has already
        /// invoked the entrypoint, and to support running libraries,
        /// not only executables. Also used internally when bootstrapping
        /// packages with no application.
        /// </remarks>
        DontRun
    }
}