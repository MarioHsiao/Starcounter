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
        private const int PollTimeoutMs = 100;

        public LogReader(string db_name, string log_dir, LogPosition position)
        {
            _log_handle = LogReaderImports.TransactionLogOpenAndSeek(db_name, log_dir, position);
        }

        public LogReader(string db_name, string log_dir)
        {
            _log_handle = LogReaderImports.TransactionLogOpen(db_name, log_dir);
        }

        private IntPtr _log_handle;

        public Task<LogReadResult> ReadAsync(CancellationToken ct, bool wait_for_live_updates = true)
        {
            return read_next_transaction(ct, wait_for_live_updates);
        }

        private async Task<LogReadResult> read_next_transaction(CancellationToken ct, bool wait_for_live_updates)
        {
            await Task.Yield();

            while (LogReaderImports.TransactionLogIsEOF(_log_handle))
            {
                if (!wait_for_live_updates)
                    return null;

                await Task.Delay(PollTimeoutMs, ct);
            }

            //read transaction
            LogReadResult res = new LogReadResult();
            res.transaction_data = read_current_transaction();

            //move to next record to get continuatin position. 
            LogReaderImports.TransactionLogMoveNext(_log_handle);

            res.continuation_position = LogReaderImports.TransactionLogGetPosition(_log_handle);

            return res;
        }

        private TransactionData read_current_transaction()
        {
            uint insertupdate_entry_count;
            uint delete_entry_count;
            LogReaderImports.TransactionLogGetCurrentTransactionInfo(_log_handle, out insertupdate_entry_count, out delete_entry_count);

            TransactionData res = new TransactionData
            {
                creates = new List<create_record_entry>((int)insertupdate_entry_count),
                updates = new List<update_record_entry>((int)insertupdate_entry_count),
                deletes = new List<delete_record_entry>((int)delete_entry_count)
            };

            for (uint i = 0; i < insertupdate_entry_count; ++i)
            {
                bool is_insert;
                string table;
                ulong object_id;
                uint columns_count;

                LogReaderImports.TransactionLogGetInsertUpdateEntryInfo(_log_handle, i, out is_insert, out table, out object_id, out columns_count);

                if (is_insert)
                    res.creates.Add(new create_record_entry
                    {
                        table = table,
                        key = new reference { object_id = object_id },
                        columns = read_columns(i, columns_count)
                    });
                else
                    res.updates.Add(new update_record_entry
                    {
                        table = table,
                        key = new reference { object_id = object_id },
                        columns = read_columns(i, columns_count)
                    });
            }

            for (uint i = 0; i < delete_entry_count; ++i)
            {
                string table;
                ulong object_id;

                LogReaderImports.TransactionLogGetDeleteEntryInfo(_log_handle, i, out table, out object_id);

                res.deletes.Add(new delete_record_entry { table = table, key = new reference { object_id = object_id } });
            }

            return res;
        }

        private column_update[] read_columns(uint insertupdate_entry_index, uint columns_count)
        {
            column_update[] res = new column_update[columns_count];
            for (uint c = 0; c < columns_count; ++c)
            {
                string name;
                object value;
                LogReaderImports.TransactionLogGetInsertUpdateEntryColumnInfo(_log_handle, insertupdate_entry_index, c, out name, out value);

                res[c] = new column_update { name = name, value = value };
            }

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
