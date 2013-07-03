// ***********************************************************************
// <copyright file="StartDatabaseCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Bootstrap.Management;
using Starcounter.Logging;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace Starcounter.Server.Commands.Processors {

    [CommandProcessor(typeof(StartDatabaseCommand))]
    internal sealed partial class StartDatabaseCommandProcessor : CommandProcessor {
        static LogSource log = ServerLogSources.Default;

        /// <summary>
        /// Initializes a new <see cref="StartDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="StartDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public StartDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            StartDatabaseCommand command = (StartDatabaseCommand)this.Command;
            Database database;
            Process codeHostProcess;
            bool started;

            if (!this.Engine.Databases.TryGetValue(command.Name, out database)) {
                throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND, string.Format("Database: '{0}'.", command.DatabaseUri));
            }

            WithinTask(Task.StartDatabaseProcess, (task) => {
                // Check if it's already started; if so, we return false,
                // with the effect that the task is marked as cancelled.
                if (Engine.DatabaseEngine.IsDatabaseProcessRunning(database))
                    return false;

                // Publish our attempt to start the database process as
                // the current working we are busy doing. If we find that
                // the process was already started (unlikely, but possible)
                // we mark the task/progress cancelled by returning false.
                ProgressTask(task, 1);
                started = Engine.DatabaseEngine.StartDatabaseProcess(database);
                return started;
            });

            codeHostProcess = database.GetRunningCodeHostProcess();
            started = codeHostProcess != null;

            WithinTask(Task.StartCodeHostProcess, (task) => {
                if (codeHostProcess != null)
                    return false;

                ProgressTask(task, 1);
                started = Engine.DatabaseEngine.StartCodeHostProcess(
                    database, command.NoDb, command.LogSteps, out codeHostProcess);
                return started;
            });

            WithinTask(Task.AwaitCodeHostOnline, (task) => {
                // Wait until either the host comes online or until the process
                // terminates, whichever comes first.
                EventWaitHandle online = null;
                var name = string.Concat(DatabaseEngine.ScCodeEvents.OnlineBaseName, database.Name.ToUpperInvariant());

                try {
                    while (!codeHostProcess.HasExited) {
                        if (online == null) {
                            if (!EventWaitHandle.TryOpenExisting(name, out online)) {
                                online = null;
                                Thread.Yield();
                            }
                        }
                        
                        if (online != null) {
                            var ready = online.WaitOne(1000);
                            if (ready) break;
                        }

                        codeHostProcess.Refresh();
                    }

                } finally {
                    if (online != null) {
                        online.Close();
                    }
                }

                Engine.CurrentPublicModel.UpdateDatabase(database);

                if (codeHostProcess.HasExited) {
                    throw DatabaseEngine.CreateCodeHostTerminated(codeHostProcess, database);
                }
            });
        }
    }
}