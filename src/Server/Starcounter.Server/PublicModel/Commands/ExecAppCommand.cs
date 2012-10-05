
using System;
using System.IO;

namespace Starcounter.Server.PublicModel.Commands {

    /// <summary>
    /// A command representing the request to start an executable.
    /// </summary>
    internal sealed class ExecAppCommand : ServerCommand {
        /// <summary>
        /// Gets the path to the assembly file requesting to start.
        /// </summary>
        internal readonly string AssemblyPath;

        /// <summary>
        /// Gets the path to the directory the requesting executable
        /// has given as it's working directory;
        /// </summary>
        internal readonly string WorkingDirectory;

        /// <summary>
        /// Gets the arguments with which the requesting executable
        /// was started with.
        /// </summary>
        internal readonly string[] Arguments;

        /// <summary>
        /// Initializes an instance of <see cref="ExecAppCommand"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> where this command
        /// are to execute.</param>
        /// <param name="assemblyPath">Path to the assembly requesting to start.</param>
        /// <param name="workingDirectory">Working directory the executable has requested to run in.</param>
        /// <param name="arguments">Arguments as passed to the requesting executable.</param>
        internal ExecAppCommand(ServerEngine engine, string assemblyPath, string workingDirectory, string[] arguments)
            : base(engine, "Starting {0}", Path.GetFileName(assemblyPath)) {
            if (string.IsNullOrEmpty(assemblyPath)) {
                throw new ArgumentNullException("assemblyPath");
            }
            this.AssemblyPath = assemblyPath;
            if (string.IsNullOrEmpty(workingDirectory)) {
                workingDirectory = Path.GetDirectoryName(this.AssemblyPath);
            }
            this.WorkingDirectory = workingDirectory;
            this.Arguments = arguments;
        }
    }
}