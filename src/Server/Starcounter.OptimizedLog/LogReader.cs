using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Starcounter.TransactionLog;

namespace Starcounter.OptimizedLog
{
    class OptimizedLogReader : IOptimizedLogReader
    {
        public OptimizedLogReader(string db_name, string log_dir, OptimizedLogPosition position)
        {
            _log_handle = OptimizedLogReaderImports.OptimizedLogOpenAndSeek(db_name, log_dir, position);
        }

        public OptimizedLogReader(string db_name, string log_dir)
        {
            _log_handle = OptimizedLogReaderImports.OptimizedLogOpen(db_name, log_dir);
        }

        private IntPtr _log_handle;

        public IEnumerable<OptimizedLogReadResult> Records
        {
            get
            {
                OptimizedLogReadResult record = ReadNext();
                while( record != null )
                {
                    yield return record;
                    record = ReadNext();
                }
            }
        }

        public LogPosition TransactionLogContinuationPosition
        {
            get
            {
                return OptimizedLogReaderImports.OptimizedLogGetTransactionLogContinuationPosition(_log_handle);
            }
        }

        private OptimizedLogReadResult ReadNext()
        {
            if (OptimizedLogReaderImports.OptimizedLogIsEOF(_log_handle))
                return null;

            //read record
            OptimizedLogReadResult res = new OptimizedLogReadResult();
            res.record = read_current_record();

            //move to next record to get continuatin position. 
            OptimizedLogReaderImports.OptimizedLogMoveNext(_log_handle);

            res.continuation_position = OptimizedLogReaderImports.OptimizedLogGetPosition(_log_handle);

            return res;
        }

        private Starcounter.TransactionLog.create_record_entry read_current_record()
        {
            string table;
            ulong object_id;
            uint columns_count;

            OptimizedLogReaderImports.OptimizedLogGetEntryInfo(_log_handle, out table, out object_id, out columns_count);

            return new Starcounter.TransactionLog.create_record_entry
            {
                table = table,
                key = new Starcounter.TransactionLog.reference { object_id = object_id },
                columns = read_columns(columns_count)
            };
        }

        private Starcounter.TransactionLog.column_update[] read_columns(uint columns_count)
        {
            Starcounter.TransactionLog.column_update[] res = new Starcounter.TransactionLog.column_update[columns_count];
            for (uint c = 0; c < columns_count; ++c)
            {
                string name;
                object value;
                OptimizedLogReaderImports.OptimizedLogGetEntryColumnInfo(_log_handle, c, out name, out value);

                res[c] = new Starcounter.TransactionLog.column_update { name = name, value = value };
            }

            return res;
        }

        #region IDisposable Support
        protected virtual void Free()
        {
            if (_log_handle != IntPtr.Zero)
            {
                OptimizedLogReaderImports.OptimizedLogClose(_log_handle);
                _log_handle = IntPtr.Zero;
            }
        }

        ~OptimizedLogReader()
        {
            Free();
        }

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
