
using System;

namespace Starcounter {

    /// <summary>
    /// Represents a Starcounter application, executing in or configured
    /// to run in, a Starcounter code host.
    /// </summary>
    public sealed class Application {
        /// <summary>
        /// Gets the name, including the full path, of the applications
        /// primary file.
        /// </summary>
        /// <remarks>
        /// An application can be launched by several types of files,
        /// including executables (.exe), source code (e.g. .cs) and
        /// libraries (.dll) and future, yet not known files, on a variety
        /// of host systems. This property is designed to return the full
        /// file name, including the path, of the file responsible for
        /// starting the current application.
        /// </remarks>
        public string FileName { get; internal set; }
        
        /// <summary>
        /// Gets the full path of the file actually loaded in the code
        /// host.
        /// </summary>
        public string LoadPath { get; internal set; }

        /// <summary>
        /// Gets the logical working directory of the current <see cref="Application"/>.
        /// </summary>
        public string WorkingDirectory { get; internal set; }
        
        /// <summary>
        /// Gets the arguments with which the current <see cref="Application"/>
        /// was started. These are the arguments passed to a possible entrypoint,
        /// semantically comparable to <see cref="Environment.CommandLine"/>.
        /// </summary>
        public string[] Arguments { get; internal set; }
    }
}
