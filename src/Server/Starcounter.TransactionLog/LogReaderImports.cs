using System;
using System.Runtime.InteropServices;

namespace Starcounter.TransactionLog
{
    static class LogReaderImports
    {
        [DllImport("logreader.dll", CharSet = CharSet.Ansi)]
        public extern static int TransactionLogOpen(string db_name, string log_dir, out IntPtr log_handle);

        [DllImport("logreader.dll", CharSet = CharSet.Ansi)]
        public extern static int TransactionLogOpenAndSeek(string db_name, string log_dir, [In,Out] LogPosition pos, out IntPtr log_handle);

        [DllImport("logreader.dll")]
        public extern static void TransactionLogClose(IntPtr log_handle);

/*
    //log traversion
    LOGREADER_API int TransactionLogIsEOF(log_handle_t log, bool* eof);
    LOGREADER_API int TransactionLogMoveNext(log_handle_t log);
    LOGREADER_API star_log_reader::log_position TransactionLogGetPosition(log_handle_t log);

    //transaction entry content
    LOGREADER_API int TransactionLogGetCurrentTransactionInfo(log_handle_t log, uint32_t* insertupdate_entry_count, uint32_t* delete_entry_count);

    //log entry content
    LOGREADER_API int TransactionLogGetDeleteEntryInfo(log_handle_t log, uint32_t delete_entry_index, const ucs2_char** table, uint64_t* object_id);
LOGREADER_API int TransactionLogGetInsertUpdateEntryInfo(log_handle_t log, uint32_t insertupdate_entry_index, bool* is_insert, const ucs2_char** table, uint64_t* object_id, uint32_t* columns_count);
    LOGREADER_API int TransactionLogGetInsertUpdateEntryColumnInfo(log_handle_t log, uint32_t insertupdate_entry_index, uint32_t column_index, const ucs2_char** column_name, uint8_t* column_type);
LOGREADER_API int TransactionLogGetColumnStringValue(log_handle_t log, uint32_t insertupdate_entry_index, uint32_t column_index, const ucs2_char** val);
    LOGREADER_API int TransactionLogGetColumnBinaryValue(log_handle_t log, uint32_t insertupdate_entry_index, uint32_t column_index, const uint8_t** data, uint32_t* size);
LOGREADER_API int TransactionLogGetColumnIntValue(log_handle_t log, uint32_t insertupdate_entry_index, uint32_t column_index, int64_t* val);
    LOGREADER_API int TransactionLogGetColumnDoubleValue(log_handle_t log, uint32_t insertupdate_entry_index, uint32_t column_index, double* val);
    LOGREADER_API int TransactionLogGetColumnFloatValue(log_handle_t log, uint32_t insertupdate_entry_index, uint32_t column_index, float* val);
    */

    }
}
