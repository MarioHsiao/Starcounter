
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace Starcounter.Advanced {
    
    /// <summary>
    /// </summary>
    public class DbEnvironment {

        /// <summary>
        /// </summary>
        public DbEnvironment(string databaseName, bool hasDatabase) { // TODO: Internal

            if (string.IsNullOrEmpty(databaseName)) throw new ArgumentException("databaseName");

            DatabaseNameLower = databaseName.ToLower();
            StarcounterEnvironment.DatabaseNameLower = DatabaseNameLower;

            HasDatabase = hasDatabase;
        }

        /// <summary>
        /// Name of the database.
        /// </summary>
        public string DatabaseNameLower { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there is a database attached to the current applet
        /// </summary>
        public bool HasDatabase { get; private set; }

        /// <summary>
        /// Gets the number of schedulers.
        /// </summary>
        [DllImport("coalmine.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        unsafe extern static UInt32 cm3_get_cpuc(void* h_opt, Byte* pcpuc);

        Byte schedulerCount_ = 0;

        /// <summary>
        /// Gets the number of schedulers.
        /// </summary>
        public Byte SchedulerCount
        {
            get
            {
                if (0 == schedulerCount_)
                {
                    unsafe
                    {
                        Byte cpuc = 0;
                        cm3_get_cpuc(null, &cpuc);
                        schedulerCount_ = cpuc;
                    }
                }

                return schedulerCount_;
            }
        }

        [DllImport("sccoredb.dll")]
        private extern static void sccoredb_get_db_info(byte[] uuid, out IntPtr db_name, out IntPtr log_path);

        private class DbInfo
        {
            public Guid db_uuid;
            public string db_name;
            public string log_dir;
        }

        private Lazy<DbInfo> _db_info = new Lazy<DbInfo>(() =>
        {
            byte[] uuid = new byte[16];
            IntPtr db_name;
            IntPtr log_dir;

            sccoredb_get_db_info(uuid, out db_name, out log_dir);

            return new DbInfo {
                    db_uuid = new Guid(uuid),
                    db_name = Marshal.PtrToStringAnsi(db_name),
                    log_dir = Marshal.PtrToStringAnsi(log_dir) };
        });

        public Guid DatabaseGuid
        {
            get
            {
                return _db_info.Value.db_uuid;
            }
        }

        public string DatabaseName
        {
            get
            {
                return _db_info.Value.db_name;
            }
        }

        public string DatabaseLogDir
        {
            get
            {
                return _db_info.Value.log_dir;
            }
        }

    }
}
