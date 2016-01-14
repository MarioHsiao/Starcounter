// ***********************************************************************
// <copyright file="DatabaseDefaults.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Advanced.Configuration;
using System.Runtime.InteropServices;

namespace Starcounter.Server {

    /// <summary>
    /// Represents database default values for a given server.
    /// </summary>
    internal sealed class DatabaseDefaults {
        private static class Win32 {
            [StructLayout(LayoutKind.Sequential)]
            internal struct MEMORYSTATUSEX {
                internal uint dwLength;
                internal uint dwMemoryLoad;
                internal ulong ullTotalPhys;
                internal ulong ullAvailPhys;
                internal ulong ullTotalPageFile;
                internal ulong ullAvailPageFile;
                internal ulong ullTotalVirtual;
                internal ulong ullAvailVirtual;
                internal ulong ullAvailExtendedVirtual;
            }

            [DllImport("Kernel32.dll")]
            internal static extern Int32 GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
        }

        private const long MIN_DEFAULT_TRANSACTION_LOG_SIZE = 256;

        /// <summary>
        /// The static default collation file, used when either no configuration
        /// not a platform-dependent value can be retreived.
        /// </summary>
        private const string StaticDefaultCollationFile = "TurboText_en-GB_4.dll";

        private readonly long InitialDefaultTransactionLogSize;
        private readonly string InitialDefaultCollationFile;
        private readonly ulong InitialDefaultFirstObjectID;
        private readonly ulong InitialDefaultLastObjectID;

        internal long? ConfiguredTransactionLogSize { get; private set; }
        internal string ConfiguredCollationFile { get; private set; }
        internal ulong ConfiguredFirstObjectID { get; private set; }
        internal ulong ConfiguredLastObjectID { get; private set; }

        internal DatabaseDefaults() {
            InitialDefaultTransactionLogSize = MIN_DEFAULT_TRANSACTION_LOG_SIZE;
            InitialDefaultCollationFile = StaticDefaultCollationFile;
            InitialDefaultFirstObjectID = 1;
            InitialDefaultLastObjectID = 4611686018427387903L;
        }


        /// <summary>
        /// Gets the default transaction log size to use.
        /// </summary>
        internal long TransactionLogSize {
            get {
                return ConfiguredTransactionLogSize.HasValue ?
                    ConfiguredTransactionLogSize.Value :
                    InitialDefaultTransactionLogSize;
            }
        }

        /// <summary>
        /// Gets the default collation file to use.
        /// </summary>
        internal string CollationFile {
            get {
                return ConfiguredCollationFile ?? InitialDefaultCollationFile;
            }
        }

        internal ulong FirstObjectID
        {
            get { return ConfiguredFirstObjectID < 1 ? InitialDefaultFirstObjectID : ConfiguredFirstObjectID; }
        }

        internal ulong LastObjectID
        {
            get { return ConfiguredLastObjectID < 1 ? InitialDefaultLastObjectID : ConfiguredLastObjectID; }
        }

        /// <summary>
        /// Updates the defaults based on the given <see cref="ServerConfiguration"/>.
        /// </summary>
        /// <param name="configuration"></param>
        internal void Update(ServerConfiguration configuration) {
            DatabaseStorageConfiguration storageConfiguration = configuration.DefaultDatabaseStorageConfiguration; 
            if (storageConfiguration != null) {

                this.ConfiguredTransactionLogSize = storageConfiguration.TransactionLogSize;
                if (this.ConfiguredTransactionLogSize.HasValue && this.ConfiguredTransactionLogSize.Value < MIN_DEFAULT_TRANSACTION_LOG_SIZE)
                    this.ConfiguredTransactionLogSize = MIN_DEFAULT_TRANSACTION_LOG_SIZE;

                // NOTE:
                // Check if the file is present in the installation directory and
                // refuse it (with a log message) if not?
                this.ConfiguredCollationFile = storageConfiguration.CollationFile;
                this.ConfiguredFirstObjectID = (ulong) storageConfiguration.FirstObjectID;
                this.ConfiguredLastObjectID = (ulong) storageConfiguration.LastObjectID;
            }
        }


    }
}
