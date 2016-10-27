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
        public OptimizedLogReader(string db_name, string log_dir, OptimizedLogPosition position, Func<string, bool> table_predicate)
        {
            CreateDelegate(table_predicate);
            _log_handle = OptimizedLogReaderImports.OptimizedLogOpenAndSeek(db_name, log_dir, position, _table_predicate);
        }

        public OptimizedLogReader(string db_name, string log_dir, Func<string, bool> table_predicate)
        {
            CreateDelegate(table_predicate);
            _log_handle = OptimizedLogReaderImports.OptimizedLogOpen(db_name, log_dir, _table_predicate);
        }

        private void CreateDelegate(Func<string, bool> table_predicate)
        {
            _table_predicate = table_predicate == null ? null : new LogReaderImports.table_predicate_delegate(table_predicate);
        }

        private IntPtr _log_handle;
        private LogReaderImports.table_predicate_delegate _table_predicate;
        private MetadataCache _meta_cache = new MetadataCache();

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

        private CreateRecordEntry read_current_record()
        {
            Starcounter.TransactionLog.LogReaderImports.insertupdate_entry_info e;
            OptimizedLogReaderImports.OptimizedLogGetEntryInfo(_log_handle, out e);

            return new CreateRecordEntry
            {
                Table = _meta_cache[e.table],
                Key = new Reference { ObjectID = e.object_id},
                Columns = read_columns(e)
            };
        }

        private ColumnUpdate[] read_columns(LogReaderImports.insertupdate_entry_info e)
        {
            Starcounter.TransactionLog.ColumnUpdate[] res = new Starcounter.TransactionLog.ColumnUpdate[e.columns_updates_count];
            for (uint c = 0; c < e.columns_updates_count; ++c)
            {
                unsafe
                {
                    LogReaderImports.column_update* cu = (LogReaderImports.column_update*)e.updates + c;
                    res[c] = LogReaderImports.decode_column_update(_log_handle, *cu, _meta_cache, OptimizedLogReaderImports.OptimizedLogDecodeString);
                }
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
