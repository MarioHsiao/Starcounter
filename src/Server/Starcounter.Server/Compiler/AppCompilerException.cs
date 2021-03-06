﻿using System;
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
        /// Gets the specific error.
        /// </summary>
        public readonly AppCompilerError Error;

        /// <summary>
        /// Gets errors from the underlying compiler, or an empty enumerator
        /// if there are none.
        /// </summary>
        public IEnumerable<IAppCompilerSourceError> CompilerErrors {
            get {
                var errors = new List<IAppCompilerSourceError>();
                if (results != null && results.Errors.HasErrors)
                {
                    foreach (CompilerError error in results.Errors)
                    {
                        errors.Add(new CodeDomAppCompilerError(error));
                    }
                }
                return errors;
            }
        }

        /// <summary>
        /// Gets a value indicating if the current exception carries any
        /// compiler errors.
        /// </summary>
        public bool HasCompilerErrors {
            get {
                return results != null && results.Errors.HasErrors;
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
