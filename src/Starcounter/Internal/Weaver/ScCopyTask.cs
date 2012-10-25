// ***********************************************************************
// <copyright file="ScCopyTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PostSharp.Sdk.Extensibility;
using Starcounter;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// PostSharp task that just copy files.
    /// </summary>
    public sealed class ScCopyTask : Task {
        /// <summary>
        /// The input files
        /// </summary>
        private string inputFiles;
        /// <summary>
        /// The output files
        /// </summary>
        private string outputFiles;

        /// <summary>
        /// Gets or sets a semi-column separated list of input files.
        /// </summary>
        /// <value>The input files.</value>
        /// <remarks>This property is configured from the PostSharp project file.</remarks>
        [ConfigurableProperty(true)]
        public string InputFiles {
            get {
                return inputFiles;
            }
            set {
                inputFiles = value;
            }
        }

        /// <summary>
        /// Gets or sets a semi-column separated list of output files. This list should have
        /// the same number of items as <see cref="InputFiles" />.
        /// </summary>
        /// <value>The output files.</value>
        /// <remarks>This property is configured from the PostSharp project file.</remarks>
        [ConfigurableProperty(true)]
        public string OutputFiles {
            get {
                return outputFiles;
            }
            set {
                outputFiles = value;
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns><b>true</b> (success).</returns>
        /// <exception cref="System.InvalidOperationException">The number of input files should be the same as the number of output files.</exception>
        public override bool Execute() {
            string[] parsedInputFiles = this.inputFiles.Split(';');
            string[] parsedOutputFiles = this.outputFiles.Split(';');
            if (parsedInputFiles.Length != parsedOutputFiles.Length)
                throw new InvalidOperationException(
                    "The number of input files should be the same as the number of output files.");
            for (int i = 0; i < parsedInputFiles.Length; i++) {
                string inputFile = parsedInputFiles[i].Trim();
                string outputFile = parsedOutputFiles[i].Trim();
                if (inputFile.Length > 0) {
                    if (File.Exists(inputFile)) {
                        ScTransformTrace.Instance.WriteLine("Copying {{{0}}} to {{{1}}}.",
                                                            inputFile, outputFile);
                        File.Copy(inputFile, outputFile, true);
                    } else {
                        ScTransformTrace.Instance.WriteLine("Skipping the file {{{0}}} because it does not exist.",
                                                            inputFile);
                    }
                }
            }
            return true;
        }

    }
}
