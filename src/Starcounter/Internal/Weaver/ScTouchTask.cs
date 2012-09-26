using System;
using System.IO;
using PostSharp.Sdk.Extensibility;
using Starcounter;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// PostSharp task that "touches" (changes the last write time) files.
    /// </summary>
    public class ScTouchTask : Task {
        private string files;
        private DateTime timestamp;

        /// <summary>
        /// Gets or sets the semicolumn-separated list of files to be touched.
        /// </summary>
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