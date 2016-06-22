// ***********************************************************************
// <copyright file="CreateDatabaseCommandProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Server.PublicModel.Commands;
using System.Text.RegularExpressions;

namespace Starcounter.Server.Commands {
    
    [CommandProcessor(typeof(CreateDatabaseCommand))]
    internal sealed class CreateDatabaseCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="CreateDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="CreateDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public CreateDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            CreateDatabaseCommand command = (CreateDatabaseCommand)this.Command;
            AssureAllowedName(command);
            AssureUniqueName(command);
            var instanceId = this.Engine.AllocateNextDatabaseInstanceID();

            var setup = new DatabaseSetup(this.Engine, command.SetupProperties);
            var database = setup.CreateDatabase();
            database.InstanceID = instanceId;

            Engine.Databases.Add(database.Name, database);
            Engine.CurrentPublicModel.AddDatabase(database);
        }

        void AssureUniqueName(CreateDatabaseCommand command) {
            string candidate = command.SetupProperties.Name;
            if (Engine.Databases.ContainsKey(candidate)) {
                throw ErrorCode.ToException(Error.SCERRDATABASEALREADYEXISTS, string.Format("Database '{0}'.", candidate));
            }
        }

        void AssureAllowedName(CreateDatabaseCommand command) {
            string candidate = command.SetupProperties.Name;
            if (!new Regex("^[a-zA-Z0-9]*$").IsMatch(candidate)) {
                throw ErrorCode.ToException(Error.SCERRFORBIDDENDATABASENAME, string.Format("Name '{0}'.", candidate));
            }
        }
    }
}