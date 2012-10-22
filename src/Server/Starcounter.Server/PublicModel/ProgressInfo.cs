
namespace Starcounter.Server.PublicModel {
    
    /// <summary>
    /// Represents the progress of a task, executing to fullfill the
    /// execution of a <see cref="ServerCommand"/>.
    /// </summary>
    public sealed class ProgressInfo {
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        internal ProgressInfo() {
        }

        /// <summary>
        /// Initializes a new <see cref="ProgressInfo"/>.
        /// </summary>
        /// <param name="data">To be defined.</param>
        internal ProgressInfo(int taskID, int value, int maximum, string text) {
            this.TaskIdentity = taskID;
            this.Value = value;
            this.Maximum = maximum;
            this.Text = text;
        }

        /// <summary>
        /// Identity of the well-known command sub-task that has made progress,
        /// or a pseudo number according to protocol.
        /// </summary>
        public int TaskIdentity { 
            get; 
            set; 
        }

        /// <summary>
        /// Progress value.
        /// </summary>
        public int Value {
            get;
            set;
        }

        /// <summary>
        /// Progress maximum, if known.
        /// </summary>
        public int Maximum {
            get;
            set;
        }

        /// <summary>
        /// Progress Text.
        /// </summary>
        public string Text {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating if the current progess record indicates
        /// the task which it representes is completed.
        /// </summary>
        public bool IsCompleted {
            get { return this.Value == this.Maximum; }
        }
    }
}