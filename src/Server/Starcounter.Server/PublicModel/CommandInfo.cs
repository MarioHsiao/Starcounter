
using System;
using System.Xml.Serialization;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Snapshot state of an executing command processor.
    /// </summary>
    public sealed class CommandInfo {
        
        /// <summary>
        /// Initializes a <see cref="CommandInfo"/> message object.
        /// </summary>
        internal CommandInfo() {
        }

        /// <summary>
        /// Command identifier.
        /// </summary>
        public CommandId Id {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets a value indicating the identity of the type of
        /// this command. This identity can be used to query static metadata
        /// about the command, enabling service clients to build a rich
        /// user interface.
        /// </summary>
        public int CommandType {
            get;
            internal set;
        }

        /// <summary>
        /// URI of the server running the command.
        /// </summary>
        public string ServerUri {
            get;
            internal set;
        }

        /// <summary>
        /// URI of the database the current command targets.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not all commands are specific to a database, but the vast majority are.
        /// Hence, we let the database URI be part of the general <see cref="CommandInfo"/>
        /// object rather than specializing it for this scenario.
        /// </para>
        /// <para>
        /// To check if a command targets a database, this property can be queried
        /// for the value of <see langword="null"/>.
        /// </para>
        /// </remarks>
        public string DatabaseUri {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating if the current command represents a
        /// an activity targeting a specific database.
        /// </summary>
        public bool IsDatabaseActivity {
            get {
                return this.DatabaseUri != null;
            }
        }

        /// <summary>
        /// Human-readable description of the command.
        /// </summary>
        public string Description {
            get;
            internal set;
        }

        /// <summary>
        /// Command start time.
        /// </summary>
        public DateTime StartTime {
            get;
            internal set;
        }

        /// <summary>
        /// Command end time.
        /// </summary>
        public DateTime? EndTime {
            get;
            internal set;
        }

        /// <summary>
        /// Command status.
        /// </summary>
        public CommandStatus Status {
            get;
            internal set;
        }

        /// <summary>
        /// Identifier of command to which the current command is correlated,
        /// i.e. typically the command that caused the current command to be queued.
        /// </summary>
        public CommandId CorrelatedCommandId {
            get;
            internal set;
        }

        /// <summary>
        /// Determines whether the command has completed (successfully or not)
        /// or if it has been cancelled. To determine the exact nature of a
        /// completed command, consult the <see cref="Status"/> property.
        /// </summary>
        public bool IsCompleted {
            get {
                return this.EndTime.HasValue;
            }
        }

        /// <summary>
        /// Errors that happened during command execution, or <b>null</b>
        /// if the command executed successfully.
        /// </summary>
        public ErrorInfo[] Errors {
            get;
            internal set;
        }

        /// <summary>
        /// Determines whether the command completed with errors.
        /// </summary>
        public bool HasError {
            get {
                return this.Errors != null && this.Errors.Length > 0;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the progress made by the
        /// command this <see cref="CommandInfo"/> represents.
        /// </summary>
        public ProgressInfo[] Progress {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating if the command comes with any
        /// progress information.
        /// </summary>
        public bool HasProgress {
            get {
                return this.Progress != null && this.Progress.Length > 0;
            }
        }

        /// <summary>
        /// Returns a clone of the current object.
        /// </summary>
        /// <returns></returns>
        public CommandInfo Clone() {
            return (CommandInfo)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Statuses of a command.
    /// </summary>
    public enum CommandStatus {
        Created,

        /// <summary>
        /// Queued for immediate execution.
        /// </summary>
        Queued,

        /// <summary>
        /// Queued for delayed execution.
        /// </summary>
        Delayed,

        /// <summary>
        /// Currently executing.
        /// </summary>
        Executing,

        /// <summary>
        /// Successfully completed.
        /// </summary>
        Completed,

        /// <summary>
        /// Failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Cancelled.
        /// </summary>
        Cancelled
    }
}