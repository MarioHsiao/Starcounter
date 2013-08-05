// ***********************************************************************
// <copyright file="CreateDatabaseCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Encapsulates the parameters used when creating a new database.
    /// </summary>
    public sealed class CreateDatabaseCommand : ServerCommand {
        /// <summary>
        /// Gets the property structure describing the properties that
        /// should be used when creating the database.
        /// </summary>
        public readonly DatabaseSetupProperties SetupProperties;

        /// <summary>
        /// Initializes a new <see cref="CreateDatabaseCommand"/>.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="databaseName"></param>
        public CreateDatabaseCommand(ServerEngine engine, string databaseName) : base(engine, "Creating database {0}", databaseName) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("name");
            }

            this.SetupProperties = new DatabaseSetupProperties(engine, databaseName);
        }

        internal override void GetReadyToEnqueue() {
            base.GetReadyToEnqueue();
            this.SetupProperties.MakeFinal();
        }
    }
}