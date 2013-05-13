// ***********************************************************************
// <copyright file="CommandInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Snapshot state of an executing command processor.
    /// </summary>
    public sealed class CommandInfo {
        
        /// <summary>
        /// Initializes a <see cref="CommandInfo"/> message object.
        /// </summary>
        public CommandInfo() {
        }

        /// <summary>
        /// Command identifier.
        /// </summary>
        public CommandId Id {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating the token of the processor of
        /// this command. This token can be used to query static metadata
        /// about the command, enabling service clients to build a rich
        /// user interface.
        /// </summary>
        public int ProcessorToken {
            get;
            set;
        }

        /// <summary>
        /// URI of the server running the command.
        /// </summary>
        public string ServerUri {
            get;
            set;
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
            set;
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
            set;
        }

        /// <summary>
        /// Command start time.
        /// </summary>
        public DateTime StartTime {
            get;
            set;
        }

        /// <summary>
        /// Command end time.
        /// </summary>
        public DateTime? EndTime {
            get;
            set;
        }

        /// <summary>
        /// Command status.
        /// </summary>
        public CommandStatus Status {
            get;
            set;
        }

        /// <summary>
        /// Identifier of command to which the current command is correlated,
        /// i.e. typically the command that caused the current command to be queued.
        /// </summary>
        public CommandId CorrelatedCommandId {
            get;
            set;
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
        /// Gets the exit code of the command, or null if no
        /// exit code was provided by the command.
        /// </summary>
        public int? ExitCode {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a possible outcome from the processor. There
        /// should never be a result unless the command is considered
        /// completed (successfully or erred).
        /// </summary>
        public object Result {
            get;
            internal set;
        }

        /// <summary>
        /// Errors that happened during command execution, or <b>null</b>
        /// if the command executed successfully.
        /// </summary>
        public ErrorInfo[] Errors {
            get;
            set;
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
            set;
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
        /// Returns the progress of a task identified by the given
        /// task identity, or null if there was no progress info found
        /// for that task.
        /// </summary>
        /// <param name="task">Identity of the task.</param>
        /// <returns>The progress info of the given task, or null if
        /// no such progress was found.</returns>
        public ProgressInfo GetProgressOf(int task) {
            var p = this.Progress;
            return p == null ? null : p.FirstOrDefault<ProgressInfo>((candidate) => {
                return candidate.TaskIdentity == task;
            });
        }

        /// <summary>
        /// Gets or sets a <see cref="ManualResetEventSlim"/> that
        /// the server can use to wait for the command to complete.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This reference will only be assigned for commands that
        /// explicitly state that they need to support waiting, see
        /// <see cref="ServerCommand.EnableWaiting"/>.
        /// </para>
        /// <para>
        /// The server core will only signal this once the underlying
        /// command has fully completed (successfully or failed) and
        /// when the latest publich snapshot (in the form of a new
        /// <see cref="CommandInfo"/> has been made available to clients
        /// consuming the public state model. Hence, when this event
        /// is signaled, the waiting thread should go and grab the
        /// latest snapshot before returning to the caller.
        /// </para>
        /// </remarks>
        internal ManualResetEventSlim CompletedEvent {
            get;
            set;
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
        /// <summary>
        /// The created
        /// </summary>
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