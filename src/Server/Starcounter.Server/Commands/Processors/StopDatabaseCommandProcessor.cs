// ***********************************************************************
// <copyright file="StopDatabaseCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.PublicModel;

namespace Starcounter.Server.Commands.Processors {
    
    [CommandProcessor(typeof(StopDatabaseCommand))]
    internal sealed partial class StopDatabaseCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="StopDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="StopDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public StopDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            DatabaseInfo result;
            StopDatabaseCommand command = (StopDatabaseCommand)this.Command;
            Database database;
            bool stopped;
            
            if (!this.Engine.Databases.TryGetValue(command.Name, out database)) {
                throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND, string.Format("Database: '{0}'.", command.DatabaseUri));
            }

            // Implement conditional execution by using fingerprinting. We map
            // fingerprints to a running engine, meaning that the presence of a
            // given fingerprint on the command has the implicit semantics of
            // a running engine with that identity. The match performed is a
            // equality match.
            if (!string.IsNullOrWhiteSpace(command.Fingerprint)) {
                result = Engine.CurrentPublicModel.GetDatabase(command.DatabaseUri);
                if (result.Engine == null || !result.Engine.Fingerprint.Equals(command.Fingerprint)) {
                    this.SetResult(result, (int)Error.SCERRCOMMANDPRECONDITIONFAILED);
                    return;
                }
            }

            // Verify any fingerprint, and if it's a not match, don't do anything,
            // including not touching the model.

            WithinTask(Task.StopCodeHostProcess, (task) => {
                stopped = Engine.DatabaseEngine.StopCodeHostProcess(database);
                return !stopped;
            });

            if (command.StopDatabaseProcess) {
                WithinTask(Task.StopDatabaseProcess, (task) => {
                    stopped = Engine.DatabaseEngine.StopDatabaseProcess(database);
                    return !stopped;
                });
            }
            
            result = Engine.CurrentPublicModel.UpdateDatabase(database);
            SetResult(result);
        }
    }
}