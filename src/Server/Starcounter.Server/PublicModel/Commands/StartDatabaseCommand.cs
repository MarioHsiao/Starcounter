// ***********************************************************************
// <copyright file="StartDatabaseCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Encapsulates a request to start a database.
    /// </summary>
    public sealed class StartDatabaseCommand : DatabaseCommand {
        /// <summary>
        /// Initializes a new <see cref="StartDatabaseCommand"/>.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="databaseName"></param>
        public StartDatabaseCommand(ServerEngine engine, string databaseName)
            : base(engine, CreateDatabaseUri(engine, databaseName), string.Format("Starting database {0}.", databaseName)) {
        }

        /// <summary>
        /// Gets or sets a value indicating if the code host that are to be started
        /// should connect to the database or not.
        /// </summary>
        public bool NoDb {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the code host that are to be started
        /// should log it's boot sequence steps or not.
        /// </summary>
        public bool LogSteps {
            get;
            set;
        }
    }
}