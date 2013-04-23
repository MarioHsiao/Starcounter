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
    internal sealed class StartDatabaseCommandProcessor : CommandProcessor {
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

            if (!this.Engine.Databases.TryGetValue(command.Name, out database)) {
                throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND, string.Format("Database: '{0}'.", command.DatabaseUri));
            }

            Engine.DatabaseEngine.StartDatabaseProcess(database);
            Engine.DatabaseEngine.StartCodeHostProcess(database, out workerProcess);
            Engine.CurrentPublicModel.UpdateDatabase(database);

            // Get a client handle to the worker process.

            var client = this.Engine.DatabaseHostService.GetHostingInterface(database);

            // Send a ping, the means by which we check the "status" of the
            // worker process - if it answers, we consider it online.

            bool success = client.Send("Ping");
            if (!success) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
            }
        }
    }
}