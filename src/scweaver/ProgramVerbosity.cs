
namespace Starcounter.Weaver {
    /// <summary>
    /// The supported verbosity levels, normally bound to the
    /// output of the program (i.e. the console).
    /// </summary>
    enum Verbosity {
        /// <summary>
        /// Nothing is written.
        /// </summary>
        Quiet = 0,

        /// <summary>
        /// Errors and warnings are written.
        /// </summary>
        Minimal = 10,
        Default = Minimal,

        /// <summary>
        /// Errors, warnings and other kind of information is written.
        /// </summary>
        Verbose = 20,

        /// <summary>
        /// Everything is written, including debug messages.
        /// </summary>
        Diagnostic = 30
    }
}