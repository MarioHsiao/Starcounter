// ***********************************************************************
// <copyright file="DatabaseInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Xml.Serialization;
using Starcounter.Advanced.Configuration;
using System;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Represents a snapshot of the public state of a database.
    /// </summary>
    public sealed class DatabaseInfo {
        /// <summary>
        /// Gets or sets the URI of the database.
        /// </summary>
        public readonly string Uri;

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public readonly string Name;


        /// <summary>
        /// Gets or sets the size of the database log file(s). The
        /// value is the size in bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is approximate and might be influenced by alignments.
        /// However, it should be accurate enough to indicate the size of
        /// the transaction log as usually referenced in day to day
        /// discussions.
        /// </para>
        /// <para>
        /// Notice that Starcounter supports databases with multiple log
        /// files. In such an installation, this value is the combined size
        /// of all transaction logs.
        /// </para>
        /// </remarks>
        public readonly long TransactionLogSize;

        /// <summary>
        /// Gets or sets the name of the collation file used by the
        /// database referenced by the current instance.
        /// </summary>
        public readonly string CollationFile;

        /// <summary>
        /// Gets or sets the first object ID used by the database.
        /// </summary>
        public readonly ulong FirstObjectID;

        /// <summary>
        /// Gets or sets the last object ID used by the database.
        /// </summary>
        public readonly ulong LastObjectID;

        /// <summary>
        /// Gets or sets a value indicating if the database referenced
        /// by the current instance supports replication.
        /// </summary>
        public readonly bool SupportReplication;

        /// <summary>
        /// Gets or sets the database configuration.
        /// </summary>
        public readonly DatabaseConfiguration Configuration;

        /// <summary>
        /// Gets the base directory where this database stores and runs
        /// executables from.
        /// </summary>
        public readonly string ExecutableBasePath;

        /// <summary>
        /// Gets or sets the <see cref="EngineInfo"/> of the
        /// current database. Null indicates the engine is shut
        /// down (including the host and the database).
        /// </summary>
        public readonly EngineInfo Engine;

        /// <summary>
        /// Gets or sets the UUID used to uniquely identify a database.
        /// </summary>
        public Guid DbUUID { get; set; }
        
        /// <summary>
        /// Initializes a <see cref="DatabaseInfo"/>.
        /// </summary>
        internal DatabaseInfo(
            string uri, string name, long logSize, string exeBasePath, EngineInfo engine, DatabaseConfiguration config, string collation) {
            this.Uri = uri;
            this.Name = name;
            this.TransactionLogSize = logSize;
            this.ExecutableBasePath = exeBasePath;
            this.Engine = engine;
            this.Configuration = config;
            this.CollationFile = collation;
            this.SupportReplication = false;
        }
    }
}