
namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Describes a task, possibly executed as part of executing
    /// a server command.
    /// </summary>
    public sealed class TaskInfo {
        
        /// <summary>
        /// Numeric identity of the task, used to send over the wire
        /// when reporting about progress.
        /// </summary>
        public int ID {
            get;
            internal set;
        }

        /// <summary>
        /// A short text, typically a single line, describing what the
        /// task does.
        /// </summary>
        public string ShortText {
            get;
            internal set;
        }

        /// <summary>
        /// A possibly long description about what this task does.
        /// </summary>
        public string Description {
            get;
            internal set;
        }

        /// <summary>
        /// The normal/expected duration of this task.
        /// </summary>
        public TaskDuration Duration {
            get;
            internal set;
        }

        /// <summary>
        /// Specifies the units for tasks that reports progress
        /// by numbers, i.e. "Percentage", "Files", "Kilobytes",
        /// etc.
        /// </summary>
        public string ProgressUnits {
            get;
            internal set;
        }
    }
}