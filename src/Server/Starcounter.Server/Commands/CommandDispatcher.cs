// ***********************************************************************
// <copyright file="CommandDispatcher.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Starcounter.Server.Commands {

    /// <summary>
    /// Dispatches commands to their <see cref="CommandProcessor"/>
    /// and enqueue them for execution.
    /// </summary>
    internal sealed class CommandDispatcher {
        // These fields are initialialized at startup and data is static. No need
        // to protect access to these fields with locks.

        private CommandDescriptor[] _commandDescriptors = new CommandDescriptor[0];
        private Dictionary<Type, ConstructorInfo> _constructors = new Dictionary<Type, ConstructorInfo>();
        private readonly ServerEngine _engine;

        private readonly Object _syncRoot = new Object();

        struct Lists {
            public CommandProcessor CurrentProc;
            public LinkedList<CommandProcessor> PendingProc;

            // Note that only the command information related to the current command is
            // changed while processing commands. The other remains static.

            public CommandInfo CurrentInfo;
            public LinkedList<CommandInfo> PendingInfo;

            public LinkedList<CommandInfo> ProcessedInfo;
        }

        private Lists _lists = new Lists(); // Thread-safety on these fields are maintained by lock (_syncRoot).

        private readonly static TimeSpan TimeUntilDoneTasksAreRemoved = TimeSpan.FromMinutes(2);

        internal CommandDispatcher(ServerEngine engine) {
            _engine = engine;

            _lists.PendingProc = new LinkedList<CommandProcessor>();
            _lists.PendingInfo = new LinkedList<CommandInfo>();
            _lists.ProcessedInfo = new LinkedList<CommandInfo>();
        }

        internal CommandDescriptor[] CommandDescriptors // Obviously the caller must not alter this array.
        {
            get { return _commandDescriptors; }
        }

        internal void Setup() {
            DiscoverAssembly(GetType().Assembly);
        }

        internal CommandInfo GetRecentCommand(CommandId id) {
            Func<CommandInfo, bool> match;

            match = delegate(CommandInfo candidate) {
                return candidate != null && candidate.Id.Equals(id);
            };

            lock (_syncRoot) {

                if (match(_lists.CurrentInfo))
                    return _lists.CurrentInfo;

                if (_lists.PendingInfo.Count != 0) {
                    LinkedListNode<CommandInfo> nextCommand = _lists.PendingInfo.First;
                    do {
                        if (match(nextCommand.Value))
                            return nextCommand.Value;

                        nextCommand = nextCommand.Next;
                    }
                    while (nextCommand != null);
                }

                if (_lists.ProcessedInfo.Count != 0) {
                    DateTime removedAt = DateTime.Now.Subtract(TimeUntilDoneTasksAreRemoved);
                    LinkedListNode<CommandInfo> nextCommand = _lists.ProcessedInfo.First;
                    do {
                        LinkedListNode<CommandInfo> command = nextCommand;
                        nextCommand = command.Next;

                        if (command.Value.EndTime > removedAt) {
                            if (match(command.Value))
                                return command.Value;
                        } else {
                            RemoveProcessedCommand(command);
                        }
                    }
                    while (nextCommand != null);
                }
            }

            return null;
        }

        internal CommandInfo[] GetRecentCommands() {
            List<CommandInfo> commands = new List<CommandInfo>();

            lock (_syncRoot) {
                if (_lists.CurrentInfo != null) commands.Add(_lists.CurrentInfo);

                if (_lists.PendingInfo.Count != 0) {
                    LinkedListNode<CommandInfo> nextCommand = _lists.PendingInfo.First;
                    do {
                        if (nextCommand.Value != null) commands.Add(nextCommand.Value);
                        nextCommand = nextCommand.Next;
                    }
                    while (nextCommand != null);
                }

                if (_lists.ProcessedInfo.Count != 0) {
                    DateTime removedAt = DateTime.Now.Subtract(TimeUntilDoneTasksAreRemoved);
                    LinkedListNode<CommandInfo> nextCommand = _lists.ProcessedInfo.First;
                    do {
                        LinkedListNode<CommandInfo> command = nextCommand;
                        nextCommand = command.Next;

                        if (command.Value.EndTime > removedAt) {
                            commands.Add(command.Value);
                        } else {
                            RemoveProcessedCommand(command);
                        }
                    }
                    while (nextCommand != null);
                }
            }

            return commands.ToArray();
        }

        void RemoveProcessedCommand(LinkedListNode<CommandInfo> commandNode) {
            var list = commandNode.List;
            var command = commandNode.Value;
            if (command.CompletedEvent != null) {
                try {
                    command.CompletedEvent.Dispose();
                } catch (ObjectDisposedException) { }
                command.CompletedEvent = null;
            }
            list.Remove(commandNode);
        }

        /// <summary>
        /// Enqueues a command for immediate execution.
        /// </summary>
        /// <param name="command">The <see cref="ServerCommand"/> to enqueue.</param>
        /// <param name="cancellationPredicate">An optional cancellation predicate in
        /// the host that will periodically be queried to see if a command are to be
        /// processed.
        /// </param>
        /// <param name="correlatingTo">An optional command, represented by a
        /// <see cref="CommandProcessor"/>, the given command correlates to.</param>
        /// <param name="completionCallback">Optional callback that will be invoked
        /// when the executed command completes.
        /// </param>
        /// <returns>The <see cref="CommandProcessor"/> instance that was created to process this <paramref name="command"/>.</returns>
        internal CommandInfo Enqueue(
            ServerCommand command,
            Predicate<CommandId> cancellationPredicate = null,
            CommandProcessor correlatingTo = null,
            Action<CommandId> completionCallback = null) {
            CommandProcessor cp;
            CommandInfo ci;

            cp = GetCommandProcessor(command);
            cp.CancellationPredicate = cancellationPredicate;
            cp.CompletionCallback = completionCallback;
            cp.OnEnqueued(correlatingTo);

            if (cp.IsPublic) ci = cp.ToPublicModel();
            else ci = null;

            Boolean first = false;

            lock (_syncRoot) {
                Trace("Enqueueing {0} from thread {1}", command.Description, Thread.CurrentThread.ManagedThreadId);
                if (_lists.CurrentProc == null) {
                    _lists.CurrentProc = cp;
                    _lists.CurrentInfo = ci;
                    first = true;
                } else {
                    _lists.PendingProc.AddLast(cp);
                    _lists.PendingInfo.AddLast(ci);
                }
            }

            if (first) ThreadPool.QueueUserWorkItem(ProcessCommands);

            return ci;
        }

        private CommandProcessor GetCommandProcessor(ServerCommand command) {
            if (command is InvokableCommand)
                return new InvokableCommandProcessor(_engine, command);

            return (CommandProcessor)_constructors[command.GetType()].Invoke(new object[] { _engine, command });
        }

//#if DEBUG
        private Int32 _commmandProcessorThreadId = -1;
//#endif

        private void ProcessCommands(Object alwaysNull) {
            NotifyCommandStatusChangedCallback notifyCallback;
            CommandProcessor cp;

            notifyCallback = new NotifyCommandStatusChangedCallback(OnNotifyCommandStatusChangedCallback);

//#if DEBUG
            _commmandProcessorThreadId = Thread.CurrentThread.ManagedThreadId;
//#endif
            try {
                cp = _lists.CurrentProc;
                for (; ; ) {
                    cp.ProcessCommand(notifyCallback); // Fatal error on exception here!

                    lock (_syncRoot) {
                        if (_lists.CurrentInfo != null)
                            _lists.ProcessedInfo.AddLast(_lists.CurrentInfo);

                        if (_lists.PendingProc.Count != 0) {
                            cp = _lists.CurrentProc = _lists.PendingProc.First.Value;
                            _lists.PendingProc.RemoveFirst();
                            _lists.CurrentInfo = _lists.PendingInfo.First.Value;
                            _lists.PendingInfo.RemoveFirst();
                        } else {
                            cp = _lists.CurrentProc = null;
                            _lists.CurrentInfo = null;
                        }
                    }

                    if (cp == null) break;
                }
            } catch (Exception e)
            {
                Trace("Exception here: {0}", e.ToString());
                throw;
            }
            finally {
//#if DEBUG
                _commmandProcessorThreadId = -1;
//#endif
            }
        }

        [Conditional("TRACE")]
        static void Trace(string message, params object[] args)
        {
            message = string.Format(message, args);
            Diagnostics.WriteTimeStamp(ServerLogSources.Commands.Source, message);
        }

        private void OnNotifyCommandStatusChangedCallback(CommandInfo commandInfo) {
//#if DEBUG
            if (_commmandProcessorThreadId != Thread.CurrentThread.ManagedThreadId)
                throw new InvalidOperationException("_commmandProcessorThreadId != Thread.CurrentThread.ManagedThreadId");
            if (_lists.CurrentInfo == null)
                throw new InvalidOperationException("_lists.CurrentInfo == null");
            if (_lists.CurrentInfo.Id != commandInfo.Id)
                throw new InvalidOperationException("_lists.CurrentInfo.Id != commandInfo.Id");
//#endif
            _lists.CurrentInfo = commandInfo;
        }

        /// <summary>
        /// Discovers the command processors (<see cref="CommandProcessor"/>) present in an assembly.
        /// </summary>
        /// <param name="assembly">An assembly containing types derived from <see cref="CommandProcessor"/>.</param>
        void DiscoverAssembly(Assembly assembly) // Note that this method should only be called during initialization.
        {
            List<CommandDescriptor> commandDescriptors;
            Dictionary<Type, ConstructorInfo> constructors;
            ConstructorInfo constructor;
            MethodInfo makeDescriptor;
            CommandDescriptor descriptor;

            if (assembly == null) {
                throw new ArgumentNullException("assembly");
            }

            commandDescriptors = new List<CommandDescriptor>();
            constructors = new Dictionary<Type, ConstructorInfo>();

            foreach (Type type in assembly.GetTypes()) {
                if (!type.IsAbstract && typeof(CommandProcessor).IsAssignableFrom(type)) {
                    if (typeof(InvokableCommandProcessor) == type) {
                        continue;
                    }

                    CommandProcessorAttribute[] attributes =
                        (CommandProcessorAttribute[])type.GetCustomAttributes(
                            typeof(CommandProcessorAttribute), false
                        );

                    if (attributes.Length > 0) {
                        // Get a reference to the constructor accepting a single
                        // ServerCommand argument. This constructor will be used to
                        // create processors for commands arriving to the server.

                        constructor = type.GetConstructor(new[] { typeof(ServerEngine), typeof(ServerCommand) });
                        if (constructor == null)
                            throw new InvalidProgramException(
                                string.Format("The command processor {0} does not have the expected constructor.",
                                              type.FullName));

                        // TODO: Backlog
                        //ServerLogSources.CommandProcessor.Debug(
                        //    "Discovered the command processor {0} for {1}.",
                        //    type.Name, attributes[0].CommandType.Name
                        //);

                        constructors.Add(attributes[0].CommandType, constructor);

                        if (!attributes[0].IsInternal) {
                            // Assure a command descriptor entry is created, to allow
                            // clients to ask the server what commands it accepts and
                            // to be able to map sent commands to static descriptors
                            // when building client side components.

                            makeDescriptor = type.GetMethod("MakeDescriptor");
                            descriptor =
                                makeDescriptor != null ?
                                (CommandDescriptor)makeDescriptor.Invoke(null, null) :
                                CommandProcessor.MakeDescriptor(
                                    type,
                                    attributes[0]
                                );

                            // TODO: Backlog
                            //ServerLogSources.CommandProcessor.Debug(
                            //    "Added command descriptor with type {0} for command {1} (\"{2}\").",
                            //    descriptor.CommandType,
                            //    attributes[0].CommandType.Name,
                            //    descriptor.CommandDescription
                            //);

                            commandDescriptors.Add(descriptor);
                        }
                    }
                }
            }

            this._commandDescriptors = commandDescriptors.ToArray();
            this._constructors = constructors;
        }
    }
}