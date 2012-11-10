// ***********************************************************************
// <copyright file="ScTouchTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using PostSharp.Sdk.Extensibility;
using Starcounter;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// PostSharp task that "touches" (changes the last write time) files.
    /// </summary>
    public class ScTouchTask : Task {
        /// <summary>
        /// The files
        /// </summary>
        private string files;
        /// <summary>
        /// The timestamp
        /// </summary>
        private DateTime timestamp;

        /// <summary>
        /// Gets or sets the semicolumn-separated list of files to be touched.
        /// </summary>
        /// <value>The files.</value>
        [ConfigurableProperty(true)]
        public string Files {
            get {
                return files;
            }
            set {
                files = value;
            }
        }

        /// <summary>
        /// Gets or sets the time to which the last write time of the files should be set.
        /// </summary>
        /// <value>The timestamp.</value>
        [ConfigurableProperty(true)]
        public DateTime Timestamp {
            get {
                return timestamp;
            }
            set {
                timestamp = value;
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns><b>true</b> (success).</returns>
        public override bool Execute() {
            foreach (string file in files.Split(';')) {
                if (File.Exists(file)) {
                    ScTransformTrace.Instance.WriteLine("Touching file {{{0}}}.", file);
                    File.SetLastWriteTime(file, this.timestamp);
                } else {
                    ScTransformTrace.Instance.WriteLine("Cannot touch the file {{{0}}} because it does not exist.", file);
                }
            }
            return true;
        }
    }
}