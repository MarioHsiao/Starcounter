// ***********************************************************************
// <copyright file="CommandTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Server.PublicModel;

namespace Starcounter.Server.Commands {
    /// <summary>
    /// Server representation of a task possibly executing
    /// as part of a command.
    /// </summary>
    internal sealed class CommandTask {
        /// <summary>
        /// Numeric identity of the task, used to send over the wire
        /// when reporting about progress.
        /// </summary>
        /// <remarks>
        /// When the server accumulates progress about an executing command,
        /// each progress record will relate to a given task, identified by
        /// this identity.
        /// </remarks>
        public int ID { get; private set; }

        /// <summary>
        /// A short text, typically a single line, describing what the
        /// task does.
        /// </summary>
        public string ShortText { get; private set; }

        /// <summary>
        /// A possibly long description about what this task does.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// The normal/expected duration of this task.
        /// </summary>
        public TaskDuration Duration { get; private set; }

        /// <summary>
        /// Specifies the units for tasks that reports progress
        /// by numbers, i.e. "Percentage", "Files", "Kilobytes",
        /// etc.
        /// </summary>
        public string ProgressUnits { get; private set; }

        /// <summary>
        /// Initializes a <see cref="CommandTask"/>
        /// </summary>
        /// <param name="id">The identity to use.</param>
        /// <param name="shortText">A short textual description of the task</param>
        /// <param name="duration">The expected <see cref="TaskDuration"> duration
        /// of the task</see></param>
        public CommandTask(int id, string shortText, TaskDuration duration)
            : this(id, shortText, duration, null) {
        }

        /// <summary>
        /// Initializes a <see cref="CommandTask"/>
        /// </summary>
        /// <param name="id">The identity to use.</param>
        /// <param name="shortText">A short textual description of the task</param>
        /// <param name="duration">The expected <see cref="TaskDuration"> duration
        /// of the task</see></param>
        /// <param name="description">A longer descritopn of the task.</param>
        public CommandTask(int id, string shortText, TaskDuration duration, string description) {
            this.ID = id;
            this.ShortText = shortText;
            this.Description = description;
            this.Duration = duration;
        }

        public CommandTask Clone() {
            return (CommandTask)this.MemberwiseClone();
        }

        public TaskInfo ToPublicModel() {
            return new TaskInfo() {
                ID = this.ID,
                ShortText = this.ShortText,
                Description = this.Description,
                Duration = this.Duration
            };
        }
    }
}