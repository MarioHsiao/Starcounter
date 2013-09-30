﻿// ***********************************************************************
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

            WithinTask(Task.StartLogWriterProcess, (task) => {
                if (Engine.DatabaseEngine.IsLogWriterProcessRunning(database))
                    return false;

                ProgressTask(task, 1);
                started = Engine.DatabaseEngine.StartLogWriterProcess(database);
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
                try {
                    Engine.DatabaseEngine.WaitUntilCodeHostOnline(codeHostProcess, database);
                } finally {
                    Engine.CurrentPublicModel.UpdateDatabase(database);
                }
            });
        }
    }
}