using System;
using System.Runtime.InteropServices;

namespace Starcounter.OptimizedLog
{
    static class OptimizedLogReaderImports
    {
        [DllImport("optlogreader.dll", CharSet = CharSet.Ansi)]
        private extern static int OptimizedLogOpen(string db_name, string log_dir, TransactionLog.LogReaderImports.table_predicate_delegate table_predicate, out IntPtr log_handle);

        public static IntPtr OptimizedLogOpen(string db_name, string log_dir, TransactionLog.LogReaderImports.table_predicate_delegate table_predicate)
        {
            IntPtr log_handle;
            OptimizedLogException.Test(OptimizedLogOpen(db_name, log_dir, table_predicate, out log_handle));
            return log_handle;
        }

        [DllImport("optlogreader.dll", CharSet = CharSet.Ansi)]
        private extern static int OptimizedLogOpenAndSeek(string db_name, string log_dir, ref OptimizedLogPosition pos, TransactionLog.LogReaderImports.table_predicate_delegate table_predicate, out IntPtr log_handle);

        public static IntPtr OptimizedLogOpenAndSeek(string db_name, string log_dir, OptimizedLogPosition pos, TransactionLog.LogReaderImports.table_predicate_delegate table_predicate)
        {
            IntPtr log_handle;
            OptimizedLogException.Test(OptimizedLogOpenAndSeek(db_name, log_dir, ref pos, table_predicate, out log_handle));
            return log_handle;
        }

        [DllImport("optlogreader.dll")]
        public extern static void OptimizedLogClose(IntPtr log_handle);

        [DllImport("optlogreader.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public extern static bool OptimizedLogIsEOF(IntPtr log_handle);

        [DllImport("optlogreader.dll", EntryPoint = "OptimizedLogMoveNext")]
        private extern static int OptimizedLogMoveNext_imp(IntPtr log_handle);

        public static void OptimizedLogMoveNext(IntPtr log_handle)
        {
            OptimizedLogException.Test(OptimizedLogMoveNext_imp(log_handle));
        }

        [DllImport("optlogreader.dll")]
        public extern static OptimizedLogPosition OptimizedLogGetPosition(IntPtr log_handle);

        [DllImport("optlogreader.dll")]
        public extern static Starcounter.TransactionLog.LogPosition OptimizedLogGetTransactionLogContinuationPosition(IntPtr log_handle);

        [DllImport("optlogreader.dll")]
        public extern static void OptimizedLogGetEntryInfo(IntPtr log_handle, out Starcounter.TransactionLog.LogReaderImports.insertupdate_entry_info e);

        [DllImport("optlogreader.dll", CharSet = CharSet.Unicode)]
        internal extern static int OptimizedLogDecodeString(IntPtr log_handle, char[] dst, int dst_max, byte[] src, uint src_len, out int dst_len);
    }
}
