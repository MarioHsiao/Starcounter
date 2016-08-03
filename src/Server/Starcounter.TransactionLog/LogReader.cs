using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Starcounter.TransactionLog
{
    class LogReader : ILogReader
    {
        private const int PollTimeoutMs = 100;

        public LogReader(string db_name, string log_dir, LogPosition position, Func<string, bool> table_predicate)
        {
            CreateDelegate(table_predicate);
            _log_handle = LogReaderImports.TransactionLogOpenAndSeek(db_name, log_dir, position, _table_predicate);
        }

        public LogReader(string db_name, string log_dir, Func<string, bool> table_predicate)
        {
            CreateDelegate(table_predicate);
            _log_handle = LogReaderImports.TransactionLogOpen(db_name, log_dir, _table_predicate);
        }

        private void CreateDelegate(Func<string, bool> table_predicate)
        {
            _table_predicate = table_predicate == null ? null : new LogReaderImports.table_predicate_delegate(table_predicate);
        }

        private IntPtr _log_handle;
        private LogReaderImports.table_predicate_delegate _table_predicate;
        private MetadataCache _meta_cache = new MetadataCache();

        public Task<LogReadResult> ReadAsync(CancellationToken ct, bool wait_for_live_updates = true)
        {
            return read_next_transaction(ct, wait_for_live_updates);
        }

        private async Task<LogReadResult> read_next_transaction(CancellationToken ct, bool wait_for_live_updates)
        {
            while (LogReaderImports.TransactionLogIsEOF(_log_handle))
            {
                if (!wait_for_live_updates)
                    return null;

                await Task.Delay(PollTimeoutMs, ct);
            }

            //read transaction
            LogReadResult res = new LogReadResult();
            res.transaction_data = LogReaderImports.TransactionLogGetCurrentTransactionInfoExtended(_log_handle, _meta_cache);

            //move to next record to get continuatin position. 
            LogReaderImports.TransactionLogMoveNext(_log_handle);

            res.continuation_position = LogReaderImports.TransactionLogGetPosition(_log_handle);

            return res;
        }

        #region IDisposable Support
        protected virtual void Free()
        {
            if (_log_handle != IntPtr.Zero)
            {
                LogReaderImports.TransactionLogClose(_log_handle);
                _log_handle = IntPtr.Zero;
            }
        }

        ~LogReader()
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
