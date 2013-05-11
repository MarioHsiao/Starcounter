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
                Engine.CurrentPublicModel.UpdateDatabase(database);

                var node = new Node("127.0.0.1", NewConfig.Default.SystemHttpPort);
                node.InternalSetLocalNodeForUnitTests(false);
                var serviceUris = CodeHostAPI.CreateServiceURIs(database.Name.ToUpperInvariant());
                var keepTrying = true;
                var hostJustStarted = started;

                if (hostJustStarted) {
                    // If we in fact just started the engine, we should
                    // probably give it some time to run the bootstrap
                    // sequence.
                    Thread.Sleep(500);
                }

                while (keepTrying) {
                    try {
                        var response = node.GET(serviceUris.Host, null, null);
                        response.FailIfNotSuccessOr(503, 404);
                        if (response.StatusCode == 404) {
                            Thread.Sleep(100);
                            continue;
                        }
                        else if (response.StatusCode == 503) {
                            // It's just not available yet. Most likely in
                            // between the registering of management handlers and
                            // when the host is considered fully functional.
                            // Try again after a yield.
                            Thread.Yield();
                            continue;
                        }

                        // Success. We consider the host started.
                        keepTrying = false;

                    } catch (SocketException se) {

                        codeHostProcess.Refresh();
                        if (codeHostProcess.HasExited) {
                            throw DatabaseEngine.CreateCodeHostTerminated(codeHostProcess, database, se);
                        }

                        // The socket exception tells us that the likely cause
                        // of the problem is that the host is just starting and
                        // hasn't reach the point where it accepts requests just
                        // yet. We respond with a 2/10 of a second sleeping and
                        // the trying again.
                        Thread.Sleep(200);
                    }
                }
            });
        }
    }
}