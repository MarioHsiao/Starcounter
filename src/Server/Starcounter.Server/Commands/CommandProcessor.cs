// ***********************************************************************
// <copyright file="CommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Starcounter.Server.Commands {

    internal delegate void NotifyCommandStatusChangedCallback(CommandInfo info);

    /// <summary>
    /// Command processors execute commands specified in <see cref="ServerCommand"/> objects. This class
    /// is the base of all command processors.
    /// </summary>
    /// <remarks>
    /// Command processors should be annotated with the <see cref="CommandProcessorAttribute"/> custom
    /// attribute.
    /// </remarks>
    internal abstract class CommandProcessor : ICommandProcessor {
        private readonly ServerCommand command;

        private readonly DateTime startTime = DateTime.Now;

        private Dictionary<int, ProgressInfo> progress;
        private int? exitCode;
        private object result;
        private readonly int typeIdentity;
        private bool isCancelledByHost;

        private NotifyCommandStatusChangedCallback _notifyStatusChangedCallback;

        private Stopwatch stopwatch;

        /// <summary>
        /// Event reference we use for processors that are instructed to support
        /// waiting by means of signaling (instead of polling).
        /// </summary>
        private ManualResetEventSlim completedEvent;

        /// <summary>
        /// Initializes a new <see cref="CommandProcessor"/>.
        /// </summary>
        protected CommandProcessor(ServerEngine server, ServerCommand command) : this(server, command, false) { }

        /// <summary>
        /// Initializes a new <see cref="CommandProcessor"/>.
        /// </summary>
        protected CommandProcessor(ServerEngine server, ServerCommand command, Boolean isInternal)
            : base() {
            this.Engine = server;
            this.command = command;
            this.Id = CommandId.MakeNew();
            this.typeIdentity = CreateToken(GetType());
            this.IsPublic = !isInternal;
            this.completedEvent = command.EnableWaiting ? new ManualResetEventSlim(false) : null;
            this.result = null;
            this.exitCode = null;
            stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets a value indicating if this processor is public, i.e. it
        /// does expose state, progress and result to the public model.
        /// </summary>
        /// <remarks>
        /// <see cref="CommandProcessorAttribute.IsInternal"/>
        /// </remarks>
        internal Boolean IsPublic
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the server on which the current processor executes.
        /// </summary>
        public ServerEngine Engine {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command processed by the current processor.
        /// </summary>
        public ServerCommand Command {
            get {
                return this.command;
            }
        }

        /// <summary>
        /// Gets a unique identifier of this command in the exeucting
        /// <see cref="ServerEngine"/>.
        /// </summary>
        public CommandId Id {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command to which the current command is correlated,
        /// i.e. typically the command that caused the current command to be queued.
        /// </summary>
        public CommandProcessor CorrelatedCommand {
            get;
            set;
        }

        /// <summary>
        /// Time at which the command ended, or <b>null</b> if the command did not end yet.
        /// </summary>
        public DateTime? EndTime {
            get;
            private set;
        }

        /// <summary>
        /// Errors that occured during command execution, or <b>null</b> if the command did not complete
        /// or did not complete with errors.
        /// </summary>
        public ErrorInfo[] Errors {
            get;
            private set;
        }

        /// <summary>
        /// Command status.
        /// </summary>
        public CommandStatus Status {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets an optional predicate that are to be
        /// queried periodically by the current processor to see if
        /// command processing should be cancelled.
        /// </summary>
        public Predicate<CommandId> CancellationPredicate {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an optional callback that are to be invoked
        /// when the current processor completes.
        /// </summary>
        public Action<CommandId> CompletionCallback {
            get;
            set;
        }

        /// <summary>
        /// Provides a way for processors to attach an optional exit
        /// code and a result to the ending of their processing.
        /// The code/result will be published with the latest/final
        /// snapshot of the command when the processor has completed
        /// (either sucessfully or erred).
        /// </summary>
        /// <param name="result">The result, an opaque object.</param>
        /// <param name="exitCode">The exit code.</param>
        protected void SetResult(object result, int? exitCode = null) {
            // Lets begin to only allow this to be set during the
            // execution of commands, and loosen up a little if we find
            // it's needed.
            if (this.Status != CommandStatus.Executing)
                throw new InvalidOperationException();

            this.exitCode = exitCode;
            this.result = result;
        }

        /// <summary>
        /// Updates the status of the current command to <see cref="CommandStatus.Failed"/>.
        /// </summary>
        /// <param name="errors">Description of the error that has happened.</param>
        private void SetFailed(params ErrorInfo[] errors) {
            this.EndTime = DateTime.Now;
            this.Errors = errors;
            this.Status = CommandStatus.Failed;

            EndAllProgress(true);
            NotifyStatusChanged();
            SignalCompletion();
        }

        /// <summary>
        /// Updates the status of the current command to <see cref="CommandStatus.Completed"/>,
        /// or <see cref="CommandStatus.Cancelled"/> if the command was cancelled.
        /// </summary>
        private void SetCompleted() {
            var wasCancelled = isCancelledByHost;

            this.EndTime = DateTime.Now;
            this.Status = wasCancelled ? CommandStatus.Cancelled : CommandStatus.Completed;

            EndAllProgress(wasCancelled);
            NotifyStatusChanged();

            SignalCompletion();
        }

        /// <inheritdoc />
        public CommandInfo ToPublicModel() {
            CommandInfo info;

            DatabaseCommand databaseCommand;

            databaseCommand = this.Command as DatabaseCommand;

            info = new CommandInfo();
            info.Id = this.Id;
            info.ProcessorToken = this.typeIdentity;
            info.ServerUri = this.Engine.Uri;
            info.Description = this.command.Description;
            info.StartTime = this.startTime;
            info.Status = this.Status;
            info.EndTime = this.EndTime;
            info.Errors = this.Errors;
            info.CompletedEvent = this.completedEvent;
            info.CorrelatedCommandId = this.CorrelatedCommand != null
                ? this.CorrelatedCommand.Id
                : CommandId.Null;
            info.DatabaseUri = databaseCommand != null ? databaseCommand.DatabaseUri : null;

            if (this.progress != null) {
                info.Progress = new ProgressInfo[progress.Values.Count];
                progress.Values.CopyTo(info.Progress, 0);
            }

            if (this.EndTime.HasValue) {
                // Only assign the exit code and the result to the public model
                // representation if the processor in fact have completd.
                info.ExitCode = exitCode;
                info.Result = this.result;
            }

            return info;
        }

        /// <summary>
        /// Enqueues the current command for execution, speciyfing the
        /// <see cref="CommandProcessor"/> it correlates to.
        /// </summary>
        /// <param name="correlatingTo">A command, represented by a <see cref="CommandProcessor"/>,
        /// this command correlates to.</param>
        /// <returns>The current instance.</returns>
        internal void OnEnqueued(CommandProcessor correlatingTo) {
            if (this.Status != CommandStatus.Created) {
                throw new InvalidOperationException();
            }

            this.CorrelatedCommand = correlatingTo;
            this.Status = CommandStatus.Queued;
        }

        internal bool ShouldCancel() {
            var cancel = (this.Status == CommandStatus.Cancelled) || isCancelledByHost;
            if (!cancel && CancellationPredicate != null) {
                cancel = CancellationPredicate(this.Id);
                if (cancel) {
                    isCancelledByHost = true;
                }
            }
            return cancel;
        }

        internal void ProcessCommand(NotifyCommandStatusChangedCallback notifyStatusChangedCallback) // Must not throw exception!
        {
            // Check we are in a valid state.
            if (this.Status != CommandStatus.Queued && this.Status != CommandStatus.Delayed) {
                throw new InvalidOperationException();
            }

            if (IsPublic) _notifyStatusChangedCallback = notifyStatusChangedCallback;
            try {

                if (ShouldCancel()) {
                    SetCompleted();
                    return;
                }

                this.Status = CommandStatus.Executing;

                NotifyStatusChanged();

                OnBeginExecute();

                this.Execute();

                OnEndExecute();

                this.SetCompleted();

            } catch (Exception e) {
                ErrorInfo commandError;
                ErrorInfoException errorInfoException;
                List<ErrorInfo> errorsWithCommandError;
                bool suppressLogging;

                // Make sure we have a command-level error, representing
                // this command processor.

                suppressLogging = false;
                commandError = this.ToErrorInfo() ?? this.ToDefaultErrorInfo();
                errorsWithCommandError = new List<ErrorInfo>();
                errorsWithCommandError.Add(commandError);

                // Take action according to the type of exception being thrown

                errorInfoException = e as ErrorInfoException;
                if (errorInfoException != null) {
                    // Gather all original, raised error and create a new
                    // array including the general one as the "main" error

                    // Log each original, raised error and stuff it in the
                    // array as a prrimary error

                    foreach (var error in errorInfoException.GetErrorInfo()) {
                        Log.LogError(error.ToErrorMessage().ToString());
                        errorsWithCommandError.Add(error);
                    }
                } else if (ErrorCode.IsFromErrorCode(e)) {
                    // Exceptions with a code we log as errors, as opposed
                    // to exceptions we don't expect, which we log as real
                    // exception strings.

                    uint code;
                    ErrorCode.TryGetCode(e, out code);
                    suppressLogging = this.SuppressLoggingForError(code);
                    if (!suppressLogging) {
                        Log.LogException(e);
                    }
                    errorsWithCommandError.Add(ErrorInfo.FromException(e));
                } else {
                    // An exception not based on a Starcounter exception.
                    // Log it as an exception, and make sure we create a
                    // corresponding ErrorInfo.FromException. Logging it
                    // as an exception makes sure all inner exceptions
                    // are logged too.

                    // We wrap the exception in an error that pinpoints
                    // "an exception without code": ScErrUnexpectedCommandException.
                    // This error is internal and if reported, it tells us
                    // that there are failures we don't currently handle
                    // well in terms of pinpointing them as well-known
                    // errors (tagging them with a code).

                    string postfix;
                    string message;

                    postfix = string.Format("(\"{0}\").", e.Message);
                    message = ErrorCode.ToMessageWithArguments(
                        Error.SCERRUNEXPECTEDCOMMANDEXCEPTION,
                        string.Empty,
                        this.Command.Description
                        );

                    Log.LogException(e, message + Environment.NewLine);
                    errorsWithCommandError.Add(ErrorInfo.FromErrorCode(Error.SCERRUNEXPECTEDCOMMANDEXCEPTION, postfix, this.Command.Description));
                }

                // Log the command error (if logging is not suppressed) and
                // set the result.

                if (!suppressLogging) {
                    Log.LogError(commandError.ToErrorMessage().ToString());
                }

                OnEndExecuteFailed();
                this.SetFailed(errorsWithCommandError.ToArray());

            } finally {
                _notifyStatusChangedCallback = null;
            }
        }

        /// <summary>
        /// Executes the current command.
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// Returns the <see cref="ErrorInfo"/> used to represent failure to
        /// execute this command processor.
        /// </summary>
        /// <remarks>
        /// Specialized command processors can choose to override this method
        /// to provide general errors other than the default.
        /// </remarks>
        /// <returns>An <see cref="ErrorInfo"/> describing the general, failed
        /// operation of an unsuccessful command processor.</returns>
        protected virtual ErrorInfo ToErrorInfo() {
            return ToDefaultErrorInfo();
        }

        /// <summary>
        /// Gets a value indicating of logging of a given error should be
        /// suppressed.
        /// </summary>
        /// <param name="errorCode">The error code to evaluate.</param>
        /// <returns>True if the error should not be logged.</returns>
        protected virtual bool SuppressLoggingForError(uint errorCode) {
            // As a standard, we suppress no logging.
            return false;
        }

        /// <summary>
        /// Gets the default <see cref="ErrorInfo"/>, utilized by the server
        /// when a command processor specific <see cref="ErrorInfo"/> could
        /// not be successfully retreived.
        /// </summary>
        /// <returns></returns>
        private ErrorInfo ToDefaultErrorInfo() {
            return ErrorInfo.FromErrorCode(Error.SCERRSERVERCOMMANDFAILED, string.Empty, this.Command.Description);
        }

        /// <summary>
        /// Gets the default <see cref="LogSource"/> this processor should use
        /// when logging.
        /// </summary>
        protected virtual LogSource Log {
            get {
                return ServerLogSources.Commands;
            }
        }

        private void OnBeginExecute() {
            Trace("Executing '{0}' ({1})", true, this.command.Description, this.Id.Value);
        }

        private void OnEndExecute() {
            Trace("Executing completed ({0})", false, this.Id.Value);
        }

        private void OnEndExecuteFailed() {
            Trace("Executing failed ({0})", false, this.Id.Value);
        }

        [Conditional("TRACE")]
        protected void Trace(string message, bool restartWatch = false, params object[] args)
        {
            message = string.Format(message, args);
            if (restartWatch) stopwatch.Restart();
            Diagnostics.WriteTrace(Log.Source, stopwatch.ElapsedTicks, message);
            Diagnostics.WriteTimeStamp(Log.Source, message);
        }

        #region Progress tracking

        public static CommandDescriptor MakeDescriptor(
            Type commandProcessorType,
            CommandProcessorAttribute attribute) {
            CommandDescriptor info;
            info = new CommandDescriptor();
            info.ProcessorToken = CreateToken(commandProcessorType);
            info.CommandDescription = string.Format("Executes the command {0}.", attribute.CommandType.Name);
            return info;
        }

        /// <summary>
        /// Executes <paramref name="action"/> in between a begin and
        /// end of the <see cref="CommandTask"/> <paramref name="task"/>.
        /// </summary>
        /// <remarks>
        /// If an exception is raised from the given action, this method
        /// does invoke the end method for the task.
        /// </remarks>
        /// <param name="task">The <see cref="CommandTask"/> that is
        /// progressing while the given action executes.</param>
        /// <param name="action">The code to execute.</param>
        protected void WithinTask(CommandTask task, Action<CommandTask> action) {
            WithinTask(task, (t) => {
                action(t);
                return true;
            });
        }

        /// <summary>
        /// Executes <paramref name="func"/> in between a begin and
        /// end of the <see cref="CommandTask"/> <paramref name="task"/>.
        /// </summary>
        /// <remarks>
        /// If an exception is raised from the given function, this method
        /// does invoke the end method for the task.
        /// </remarks>
        /// <param name="task">The <see cref="CommandTask"/> that is
        /// progressing while the given action executes.</param>
        /// <param name="func">The code to execute. The ending of the task
        /// can be marked as cancelled by returning false from the func.</param>
        protected void WithinTask(CommandTask task, Func<CommandTask, bool> func) {
            bool cancel = ShouldCancel();
            
            BeginTask(task);
            try {
                if (!cancel) {
                    cancel = !func(task);
                }
            } catch {
                cancel = true;
                throw;
            } finally {
                EndTask(task, cancel);
            }
        }

        /// <summary>
        /// Executes <paramref name="action"/> in between a begin and
        /// end of the <see cref="CommandTask"/> <paramref name="task"/>,
        /// based on a given condition.
        /// </summary>
        /// <remarks>
        /// If an exception is raised from the given action, this method
        /// does invoke the end method for the task.
        /// </remarks>
        /// <param name="condition">If <c>true</c>, the
        /// action is executed; otherwise, this method instantly return.
        /// </param>
        /// <param name="task">The <see cref="CommandTask"/> that is
        /// progressing while the given action executes.</param>
        /// <param name="action">The code to execute.</param>
        protected void WithinTaskIf(bool condition, CommandTask task, Action<CommandTask> action) {
            if (condition) {
                WithinTask(task, action);
            }
        }

        /// <summary>
        /// Progresses a task by value.
        /// </summary>
        /// <param name="task">The <see cref="CommandTask"/> to progress.
        /// </param>
        /// <param name="newValue">The new value.</param>
        protected void ProgressTask(CommandTask task, int newValue) {
            ProgressInfo info;

            if (task.Duration.IsProgressing() == false)
                throw
                    new InvalidOperationException();

            info = this.progress[task.ID];

            if (info.IsCompleted)
                throw new InvalidOperationException();
            if (task.Duration.IsDeterminate() && newValue > info.Maximum)
                throw new ArgumentOutOfRangeException();


            info.Value = newValue;
            if (info.Value == info.Maximum)
                info.Text = null;

            NotifyStatusChanged();
        }

        /// <summary>
        /// Progresses a task by updating the text of the current
        /// activity.
        /// </summary>
        /// <param name="task">The <see cref="CommandTask"/> to progress.
        /// </param>
        /// <param name="newActivity">The new activity.</param>
        protected void ProgressTask(CommandTask task, string newActivity) {
            ProgressInfo info;

            info = this.progress[task.ID];

            if (info.IsCompleted)
                throw new InvalidOperationException();

            info.Text = newActivity;

            NotifyStatusChanged();
        }

        /// <summary>
        /// Progresses a task by value and updates the text of the current
        /// activity.
        /// </summary>
        /// <param name="task">The <see cref="CommandTask"/> to progress.
        /// </param>
        /// <param name="newValue">The new value.</param>
        /// <param name="newActivity">The new activity.</param>
        protected void ProgressTask(CommandTask task, int newValue, string newActivity) {
            ProgressInfo info;

            if (task.Duration.IsProgressing() == false)
                throw
                    new InvalidOperationException();

            info = this.progress[task.ID];

            if (info.IsCompleted) throw new InvalidOperationException();
            if (task.Duration.IsDeterminate() && newValue > info.Maximum)
                throw new ArgumentOutOfRangeException();

            info.Value = newValue;
            info.Text = info.Value == info.Maximum
                ? null
                : newActivity;

            NotifyStatusChanged();
        }

        /// <summary>
        /// Begins a task with no defined max value, i.e. used by 
        /// all tasks that are indeterminate.
        /// </summary>
        /// <param name="task">The <see cref="CommandTask"/> that is
        /// about to begin.</param>
        void BeginTask(CommandTask task) {
            ProgressInfo progressInfo;

            if (task.Duration.IsDeterminate())
                throw new InvalidOperationException();

            Trace("Begin task '{0}'", false, task.ShortText);

            progressInfo = new ProgressInfo(task.ID, 0, -1, null);

            if (this.progress == null) {
                this.progress = new Dictionary<int, ProgressInfo>();
            }

            this.progress.Add(task.ID, progressInfo);

            NotifyStatusChanged();
        }

        /// <summary>
        /// Cancel a task.
        /// </summary>
        /// <param name="task">The <see cref="CommandTask"/> to cancel.
        void CancelTask(CommandTask task) {
            EndTask(task, true);
        }

        /// <summary>
        /// Ends a single task.
        /// </summary>
        /// <param name="task">The task to end.</param>
        /// <param name="cancel">Indicates if the task should
        /// be marked as cancelled or fulfilled.</param>
        void EndTask(CommandTask task, bool cancel = false) {
            ProgressInfo info;

            Trace("End task (cancelled={0})", false, cancel);

            info = this.progress[task.ID];
            EndSingleProgress(info, cancel);

            NotifyStatusChanged();
        }

        /// <summary>
        /// Ends a set of tasks.
        /// </summary>
        /// <param name="tasks">The tasks to end.</param>
        void EndTasks(params CommandTask[] tasks) {
            ProgressInfo info;

            foreach (var task in tasks) {
                if (this.progress.TryGetValue(task.ID, out info)) {
                    EndSingleProgress(info);
                }
            }

            NotifyStatusChanged();
        }

        /// <summary>
        /// Ends a single progress info.
        /// </summary>
        /// <param name="info">The progress to end.</param>
        /// <param name="cancel">Indicates if the progress should be considered
        /// cancelled or not.</param>
        private void EndSingleProgress(ProgressInfo info, bool cancel = false) {
            if (cancel) {
                info.Cancel();
            } else {
                info.Value = info.Maximum;
                info.Text = null;
            }
        }

        /// <summary>
        /// Assures every task with possibly registered progress is
        /// marked ended.
        /// </summary>
        /// <remarks>
        /// Used when a processor ends successfully and prior to its
        /// state/status is published; therefore, don't invoke
        /// notification of any change here.
        /// </remarks>
        /// <param name="cancel">Indicates if any outstanding progress
        /// should be considered cancelled or not.</param>
        private void EndAllProgress(bool cancel = false) {
            if (progress != null) {
                foreach (var p in progress.Values) {
                    if (!p.IsCompleted) {
                        EndSingleProgress(p, cancel);
                    }
                }
            }
        }

        private void SignalCompletion() {
            // NOTE:
            // Don't dispose this event here; clients might still access it
            // through the reference copy in CommandInfo. The disposal of
            // this events will be governed by the server, when dropping
            // commands from the recent command list.
            // (See CommandDispatcher.RemoveProcessedCommand)
            if (this.completedEvent != null) {
                this.completedEvent.Set();
            }

            if (CompletionCallback != null) {
                Trace("CommandProcessor: Invoking completion callback");
                CompletionCallback(this.Id);
                Trace("CommandProcessor: Completion callback finished");
            }
        }

        private void NotifyStatusChanged() {
            if (_notifyStatusChangedCallback != null) {
                Trace("CommandProcessor: Notifying status changed");
                _notifyStatusChangedCallback(ToPublicModel());
                Trace("CommandProcessor: Status changed notified");
            }
        }

        #endregion

        /// <summary>
        /// Gets a value representing a token that identifies the command
        /// type of the <see cref="Type"/> supplied.
        /// </summary>
        /// <remarks>
        /// The command type token is used when publishing metadata about
        /// commands, such as a command type level description.
        /// </remarks>
        public static int CreateToken(Type commandProcessorType) {
            return commandProcessorType.FullName.GetHashCode();
        }

        void ICommandProcessor.SetResult(object result, int? exitCode) {
            this.SetResult(result, exitCode);
        }
    }
}