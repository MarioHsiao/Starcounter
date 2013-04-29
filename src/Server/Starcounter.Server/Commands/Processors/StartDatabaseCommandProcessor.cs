// ***********************************************************************
// <copyright file="StartDatabaseCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.PublicModel.Commands;
using System.Diagnostics;
using Starcounter.ABCIPC;

namespace Starcounter.Server.Commands.Processors {

    [CommandProcessor(typeof(StartDatabaseCommand))]
    internal sealed partial class StartDatabaseCommandProcessor : CommandProcessor {
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
            Process workerProcess;
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

            WithinTask(Task.StartCodeHostProcess, (task) => {
                if (Engine.DatabaseEngine.IsCodeHostProcessRunning(database))
                    return false;

                ProgressTask(task, 1);
                started = Engine.DatabaseEngine.StartCodeHostProcess(
                    database, command.NoDb, command.LogSteps, out workerProcess);
                return started;
            });

            WithinTask(Task.AwaitCodeHostOnline, (task) => {
                Engine.CurrentPublicModel.UpdateDatabase(database);

                var client = this.Engine.DatabaseHostService.GetHostingInterface(database);
                bool success = client.Send("Ping");
                if (!success) {
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
                }
            });
        }
    }
}