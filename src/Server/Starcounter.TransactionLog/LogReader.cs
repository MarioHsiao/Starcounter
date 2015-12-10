using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    class LogReader : ILogReader
    {
        public LogReader(string db_name, string log_dir, LogPosition position)
        {
            LogReaderImports.TransactionLogOpenAndSeek(db_name, log_dir, position, out _log_handle);
        }

        public LogReader(string db_name, string log_dir)
        {
            LogReaderImports.TransactionLogOpen(db_name, log_dir, out _log_handle);
        }

        private IntPtr _log_handle;

        public Task<LogReadResult> ReadAsync(CancellationToken ct, bool wait_for_live_updates = true)
        {
            return read_next_transaction(ct, wait_for_live_updates);
        }

        private async Task<LogReadResult> read_next_transaction(CancellationToken ct, bool wait_for_live_updates)
        {
            await Task.Yield();



            return new LogReadResult();
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
