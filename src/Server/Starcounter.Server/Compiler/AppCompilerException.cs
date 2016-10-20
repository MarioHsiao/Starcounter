using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Starcounter.Server.Compiler {

    /// <summary>
    /// Errors related to compiling with an <see cref="AppCompiler"/>.
    /// </summary>
    public enum AppCompilerError : uint {
        /// <summary>
        /// The compiler was given no source to compile.
        /// </summary>
        NoSourceSpecified,
        /// <summary>
        /// The underlying compiler failed to compile inputs.
        /// </summary>
        ErrorsCompiling
    }

    /// <summary>
    /// Exception specific to <see cref="AppCompiler"/> errors.
    /// </summary>
    public sealed class AppCompilerException : Exception {
        readonly CompilerResults results;

        /// <summary>
        /// Gets or sets the specific error.
        /// </summary>
        public readonly AppCompilerError Error;

        public IDictionary<string, string> CompilerErrors {
            get {
                if (results == null)
                {
                    return new Dictionary<string, string>();
                }

                var d = new Dictionary<string, string>(results.Errors.Count);
                for (int i = 0; i < results.Errors.Count; i++)
                {
                    var error = results.Errors[i];
                    d.Add(error.ErrorNumber, error.ErrorText);
                }
                return d;
            }
        }

        /// <summary>
        /// Initialize a new exception from an error.
        /// </summary>
        /// <param name="error">The specified error</param>
        internal AppCompilerException(AppCompilerError error) : base(Enum.GetName(typeof(AppCompilerError), error)) {
            Error = error;
        }

        /// <summary>
        /// Initialize a new exception from an error and a compiler
        /// result.
        /// </summary>
        /// <param name="result">Results from underlying compiler.</param>
        internal AppCompilerException(CompilerResults result) : this(AppCompilerError.ErrorsCompiling)
        {
            results = result;
        }
    }
}
