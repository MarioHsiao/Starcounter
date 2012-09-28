
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;
using System.Runtime.InteropServices;

namespace StarcounterServer {

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

        private const long MIN_DEFAULT_MAX_IMAGE_SIZE = 256;
        private const long MIN_DEFAULT_TRANSACTION_LOG_SIZE = 256;

        private readonly long CalculatedMaxImageSize;
        private readonly long InitialDefaultTransactionLogSize;

        internal long? ConfiguredMaxImageSize { get; private set; }
        internal long? ConfiguredTransactionLogSize { get; private set; }

        internal DatabaseDefaults() {
            CalculatedMaxImageSize = CalculateDefaultMaxImageSize();
            InitialDefaultTransactionLogSize = MIN_DEFAULT_TRANSACTION_LOG_SIZE;
        }

        /// <summary>
        /// Gets the default maximum image size to use.
        /// </summary>
        internal long MaxImageSize {
            get {
                return ConfiguredMaxImageSize.HasValue ?
                    ConfiguredMaxImageSize.Value :
                    CalculatedMaxImageSize;
            }
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
        /// Updates the defaults based on the given <see cref="ServerConfiguration"/>.
        /// </summary>
        /// <param name="configuration"></param>
        internal void Update(ServerConfiguration configuration) {
            this.ConfiguredMaxImageSize = configuration.DatabaseDefaultMaxImageSize;
            if (this.ConfiguredMaxImageSize.HasValue && this.ConfiguredMaxImageSize.Value < MIN_DEFAULT_MAX_IMAGE_SIZE)
                this.ConfiguredMaxImageSize = MIN_DEFAULT_MAX_IMAGE_SIZE;

            this.ConfiguredTransactionLogSize = configuration.DatabaseDefaultTransactionLogSize;
            if (this.ConfiguredTransactionLogSize.HasValue && this.ConfiguredTransactionLogSize.Value < MIN_DEFAULT_TRANSACTION_LOG_SIZE)
                this.ConfiguredMaxImageSize = MIN_DEFAULT_TRANSACTION_LOG_SIZE;
        }

        private long CalculateDefaultMaxImageSize() {
            Int64 v;
            Int32 br;
            Win32.MEMORYSTATUSEX m;

            v = 0;

            m = new Win32.MEMORYSTATUSEX();
            m.dwLength = (UInt32)Marshal.SizeOf(m);
            br = Win32.GlobalMemoryStatusEx(ref m);
            if (br != 0) {
                v = (long)((m.ullTotalPhys / 1048576) * 3);
            } else {
                // Unable to fetch memory information for some reason. Go with
                // default.
            }

            if (v < MIN_DEFAULT_MAX_IMAGE_SIZE) v = MIN_DEFAULT_MAX_IMAGE_SIZE;

            return v;
        }
    }
}
