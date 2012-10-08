﻿
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
            internal set;
        }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Name {
            get;
            internal set;
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
            internal set;
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
            internal set;
        }

        /// <summary>
        /// Gets or sets the name of the collation file used by the
        /// database referenced by the current instance.
        /// </summary>
        public string CollationFile {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the database referenced
        /// by the current instance supports replication.
        /// </summary>
        public bool SupportReplication {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the database configuration.
        /// </summary>
        public DatabaseConfiguration Configuration {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the set of "Apps" currently hosted in the database
        /// represented by this snapshot.
        /// </summary>
        public AppInfo[] HostedApps {
            get;
            internal set;
        }
    }
}