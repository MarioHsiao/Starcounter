﻿// ***********************************************************************
// <copyright file="StartDatabaseCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Server.Commands.Processors;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Encapsulates a request to start a database.
    /// </summary>
    public sealed class StartDatabaseCommand : DatabaseCommand {
        
        public static class DefaultProcessor {
            public static int Token {
                get { return StartDatabaseCommandProcessor.ProcessorToken; }
            }

            public static class Tasks {
                public const int StartDatabaseProcess = 1;
                public const int StartCodeHostProcess = 2;
                public const int AwaitCodeHostOnline = 3;
            }
        }

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