using System;

namespace Starcounter.Server.Compiler {

    /// <summary>
    /// Errors related to compiling with an <see cref="AppCompiler"/>.
    /// </summary>
    public enum AppCompilerError : uint {
        /// <summary>
        /// The compiler was given no source to compile.
        /// </summary>
        NoSourceSpecified
    }

    /// <summary>
    /// Exception specific to <see cref="AppCompiler"/> errors.
    /// </summary>
    public sealed class AppCompilerException : Exception {
        /// <summary>
        /// Gets or sets the specific error.
        /// </summary>
        public readonly AppCompilerError Error;

        /// <summary>
        /// Initialize a new exception from an error.
        /// </summary>
        /// <param name="error">The specifici error</param>
        public AppCompilerException(AppCompilerError error) : base(Enum.GetName(typeof(AppCompilerError), error)) {
            Error = error;
        }
    }
}
