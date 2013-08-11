﻿
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Modules;
using System;

namespace Starcounter.Internal.MsBuild {

    /// <summary>
    /// Class JsonToCsMsBuildTask without loading into domain (slower).
    /// </summary>
    public class JsonToTypedJsonCsMsBuildTaskNoLocking : AppDomainIsolatedTask {

        static JsonToTypedJsonCsMsBuildTaskNoLocking() {
            Bootstrapper.Bootstrap();
        }

        
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
            return JsonToCsMsBuildTask.ExecuteTask(InputFiles, OutputFiles, Log);
        }
    }
}
