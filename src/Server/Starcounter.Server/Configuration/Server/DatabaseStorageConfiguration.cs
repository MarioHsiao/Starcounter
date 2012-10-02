﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Starcounter.Configuration {

    /// <summary>
    /// Defines a set of properties describing the database storage
    /// for a given database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The principal difference between this configuration and those
    /// of <see cref="DatabaseConfiguration"/> and <see cref="DatabaseRuntimeConfigurat"/>
    /// is that the server doesn't maintain these values on disk
    /// (except for a single, server-global set that defines the
    /// configured defaults) for every database, simply because they
    /// are properties of the database image- and log files rather
    /// than configuration used for maintenance.
    /// </para>
    /// <para>
    /// The properties here correspond closely to those given to the
    /// database file creation tool (currently scddc.exe).
    /// </para>
    /// </remarks>
    [XmlType("Storage", Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public class DatabaseStorageConfiguration : ConfigurationElement {

        /// <summary>
        /// Gets or sets the database maximum image size.
        /// </summary>
        /// <remarks>
        /// Expressed in megabytes.
        /// </remarks>
        public long? MaxImageSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the database transaction log size.
        /// </summary>
        /// <remarks>
        /// Expressed in megabytes.
        /// </remarks>
        public long? TransactionLogSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collation file used by a certain database
        /// storage setup.
        /// </summary>
        public string CollationFile {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the storage this configuration
        /// represents support replication.
        /// </summary>
        public bool SupportReplication {
            get;
            set;
        }
    }
}
