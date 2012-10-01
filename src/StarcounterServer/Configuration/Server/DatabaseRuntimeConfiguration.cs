
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Starcounter.Configuration {
    /// <summary>
    /// Configures the runtime properties of a Starcounter database.
    /// </summary>
    [XmlType("DatabaseRuntime", Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public class DatabaseRuntimeConfiguration : ConfigurationElement {
        /// <summary>
        /// Directory in which temporary files are stored.
        /// </summary>
        [DefaultValue(null)]
        public string TempDirectory {
            get {
                return _tempDirectory;
            }
            set {
                _tempDirectory = value;
                OnPropertyChanged("TempDirectory");
            }
        }
        private string _tempDirectory;

        /// <summary>
        /// Full path of the directory containing image files.
        /// </summary>
        /// <remarks>Required. No default value.</remarks>
        [DefaultValue(null)]
        public string ImageDirectory {
            get {
                return _imageDirectory;
            }
            set {
                _imageDirectory = value;
                OnPropertyChanged("ImageDirectory");
            }
        }
        private string _imageDirectory;

        /// <summary>
        /// Full path of the directory containing transaction log files.
        /// </summary>
        /// <remarks>Required. No default value.</remarks>
        [DefaultValue(null)]
        public string TransactionLogDirectory {
            get {
                return _transactionLogDirectory;
            }
            set {
                _transactionLogDirectory = value;
                OnPropertyChanged("TransactionLogDirectory");
            }
        }
        private string _transactionLogDirectory;

        /// <summary>
        /// Full path of the directory containing dump files.
        /// </summary>
        /// <remarks>
        /// By default, dump files are stored in <see cref="ImageDirectory"/>.
        /// </remarks>
        [DefaultValue(null)]
        public string DumpDirectory {
            get;
            set;
        }

        /// <summary>
        /// SQL Prolog process listening port number.
        /// </summary>
        public int SQLProcessPort {
            get {
                return _SQLProcessPort;
            }
            set {
                _SQLProcessPort = value;
                OnPropertyChanged("SQLProcessPort");
            }
        }
        private int _SQLProcessPort;

        /// <summary>
        /// Support of aggregations in SQL queries, for which current implementation is very slow.
        /// </summary>
        [DefaultValue(false)]
        public Boolean SqlAggregationSupport {
            get {
                return _sqlAggregationSupport;
            }
            set {
                _sqlAggregationSupport = value;
                OnPropertyChanged("SqlAggregationSupport");
            }
        }
        private Boolean _sqlAggregationSupport;

        /// <summary>
        /// Size of the shared memory chunk, degree of 2.
        /// </summary>
        public int SharedMemoryChunkSize {
            get {
                return _sharedMemoryChunkSize;
            }
            set {
                _sharedMemoryChunkSize = value;
                OnPropertyChanged("SharedMemoryChunkSize");
            }
        }
        private int _sharedMemoryChunkSize;

        /// <summary>
        /// Number of shared memory chunks, dividable by 256.
        /// </summary>
        public int SharedMemoryChunksNumber {
            get {
                return _sharedMemoryChunksNumber;
            }
            set {
                _sharedMemoryChunksNumber = value;
                OnPropertyChanged("SharedMemoryChunksNumber");
            }
        }
        private int _sharedMemoryChunksNumber;

        /// <summary>
        /// Number of virtual processors.
        /// </summary>
        /// <remarks>
        /// Allowed values are 1, 2, 4 for x64 and 1 on Win32.
        /// Default: The number of cores on the machine up to the maximum
        /// value for x64 or one for win32.
        /// </remarks>
        [DefaultValue(null)]
        public int? VirtualProcessorCount {
            get;
            set;
        }
    }

    /// <summary>
    /// Levels of transaction consistency.
    /// </summary>
    public enum TransactionConsistencyLevel {
        /// <summary>
        /// Fuzzy reads, merging writes.
        /// </summary>
        MergingWrites = 0,

        /// <summary>
        /// Fuzzy reads, ordered writes.
        /// </summary>
        OrderedWrites = 1
    }
}