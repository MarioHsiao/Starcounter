// ***********************************************************************
// <copyright file="DatabaseInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Xml.Serialization;
using Starcounter.Configuration;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Represents a snapshot of the public state of a database.
    /// </summary>
    public sealed class DatabaseInfo {
        
        /// <summary>
        /// Initializes a <see cref="DatabaseInfo"/>.
        /// </summary>
        internal DatabaseInfo() {
        }

        /// <summary>
        /// Gets or sets the URI of the database.
        /// </summary>
        public string Uri {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Name {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the database max image file size
        /// configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Notice that Starcounter currently maintains two copies of the
        /// image file (one for checkpointing). By doubling this value, the
        /// approximate size of the database on disk is obtained.
        /// </para>
        /// </remarks>
        public long MaxImageSize {
            get;
            set;
        }

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
        public long TransactionLogSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the collation file used by the
        /// database referenced by the current instance.
        /// </summary>
        public string CollationFile {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the database referenced
        /// by the current instance supports replication.
        /// </summary>
        public bool SupportReplication {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the database configuration.
        /// </summary>
        public DatabaseConfiguration Configuration {
            get;
            set;
        }

        /// <summary>
        /// Gets the set of "Apps" currently hosted in the database
        /// represented by this snapshot.
        /// </summary>
        /// <remarks>
        /// This property should be moved out to a smaller, runtime-specific
        /// database information entity, along with all other runtime related
        /// state, like process identity, to allow information that change
        /// significantly less frequently to be updated only when needed (for
        /// example, configuration).
        /// <seealso cref="HostedApps"/>
        /// </remarks>
        public AppInfo[] HostedApps {
            get;
            set;
        }

        /// <summary>
        /// Gets the process ID of the database host process, if running.
        /// </summary>
        /// <remarks>
        /// This property should be moved out to a smaller, runtime-specific
        /// database information entity, along with all other runtime related
        /// state, like info hosted apps.
        /// <seealso cref="HostedApps"/>
        /// </remarks>
        public int HostProcessId {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the database process is running.
        /// </summary>
        /// <remarks>
        /// The server intentionally don't reveal the PID or any other sensitive
        /// information about the database process, just letting server hosts
        /// know if it's running or not.
        /// <seealso cref="HostProcessId"/>
        /// </remarks>
        public bool DatabaseProcessRunning {
            get;
            set;
        }
    }
}