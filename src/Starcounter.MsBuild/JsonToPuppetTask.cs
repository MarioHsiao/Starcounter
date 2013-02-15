
/// <summary>
/// Class JsonToCsMsBuildTask
/// </summary>
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Starcounter.Templates;

namespace Starcounter.Internal.MsBuild {
    public class JsonToPuppetCsMsBuildTask : Task {
        /// <summary>
        /// Gets or sets the input files.
        /// </summary>
        /// <value>The input files.</value>
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        /// <summary>
        /// Gets or sets the output files.
        /// </summary>
        /// <value>The output files.</value>
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute() {
            return BuildCustomObjClass<TPuppet>.ExecuteTask(InputFiles, OutputFiles, Log);
        }
    }

    /// <summary>
    /// Class JsonToCsMsBuildTask without loading into domain (slower).
    /// </summary>
    public class JsonToPuppetCsMsBuildTaskNoLocking : AppDomainIsolatedTask {
        /// <summary>
        /// Gets or sets the input files.
        /// </summary>
        /// <value>The input files.</value>
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        /// <summary>
        /// Gets or sets the output files.
        /// </summary>
        /// <value>The output files.</value>
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute() {
            return BuildCustomObjClass<TPuppet>.ExecuteTask(InputFiles, OutputFiles, Log);
        }
    }
}