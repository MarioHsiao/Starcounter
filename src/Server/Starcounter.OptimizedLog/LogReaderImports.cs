using System;
using System.Runtime.InteropServices;

namespace Starcounter.OptimizedLog
{
    static class OptimizedLogReaderImports
    {
        [DllImport("optlogreader.dll", CharSet = CharSet.Ansi)]
        private extern static int OptimizedLogOpen(string db_name, string log_dir, out IntPtr log_handle);

        public static IntPtr OptimizedLogOpen(string db_name, string log_dir)
        {
            IntPtr log_handle;
            OptimizedLogException.Test(OptimizedLogOpen(db_name, log_dir, out log_handle));
            return log_handle;
        }

        [DllImport("optlogreader.dll", CharSet = CharSet.Ansi)]
        private extern static int OptimizedLogOpenAndSeek(string db_name, string log_dir, ref OptimizedLogPosition pos, out IntPtr log_handle);

        public static IntPtr OptimizedLogOpenAndSeek(string db_name, string log_dir, OptimizedLogPosition pos)
        {
            IntPtr log_handle;
            OptimizedLogException.Test(OptimizedLogOpenAndSeek(db_name, log_dir, ref pos, out log_handle));
            return log_handle;
        }

        [DllImport("optlogreader.dll")]
        public extern static void OptimizedLogClose(IntPtr log_handle);

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogIsEOF(IntPtr log_handle, [MarshalAs(UnmanagedType.I1)] out bool eof);

        public static bool OptimizedLogIsEOF(IntPtr log_handle)
        {
            bool eof;
            OptimizedLogException.Test(OptimizedLogIsEOF(log_handle, out eof));
            return eof;
        }

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
        private extern static int OptimizedLogGetEntryInfo(
            IntPtr log_handle, out IntPtr table, out ulong object_id, out uint columns_count);

        public static void OptimizedLogGetEntryInfo(
            IntPtr log_handle, out string table, out ulong object_id, out uint columns_count)
        {
            IntPtr table_ptr;
            OptimizedLogException.Test(
                OptimizedLogGetEntryInfo(
                    log_handle, out table_ptr, out object_id, out columns_count));

            table = Marshal.PtrToStringUni(table_ptr);
        }

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogGetEntryColumnInfo(IntPtr log_handle, uint column_index, out IntPtr column_name, out byte column_type);

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogGetColumnEncodedStringValue(IntPtr log_handle, uint column_index, out IntPtr data, out uint size);

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogGetColumnBinaryValue(IntPtr log_handle, uint column_index, out IntPtr data, out uint size);

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogGetColumnIntValue(IntPtr log_handle, uint column_index, out long val, [MarshalAs(UnmanagedType.I1)] out bool is_initialized);

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogGetColumnDoubleValue(IntPtr log_handle, uint column_index, out double val, [MarshalAs(UnmanagedType.I1)] out bool is_initialized);

        [DllImport("optlogreader.dll")]
        private extern static int OptimizedLogGetColumnFloatValue(IntPtr log_handle, uint column_index, out float val, [MarshalAs(UnmanagedType.I1)] out bool is_initialized);

        [DllImport("optlogreader.dll", CharSet = CharSet.Unicode)]
        private extern static int OptimizedLogDecodeString(IntPtr log_handle, char[] dst, int dst_max, byte[] src, uint src_len, out int dst_len);

        public static void OptimizedLogGetEntryColumnInfo(IntPtr log_handle, uint column_index, out string column_name, out object column_value)
        {
            IntPtr column_name_ptr;
            byte column_type;
            OptimizedLogException.Test(OptimizedLogGetEntryColumnInfo(log_handle, column_index, out column_name_ptr, out column_type));
            column_name = Marshal.PtrToStringUni(column_name_ptr);
            column_value = null;

            switch (column_type)
            {
                case Starcounter.Internal.sccoredb.STAR_TYPE_STRING:
                    {
                        IntPtr data;
                        uint size;
                        OptimizedLogException.Test(OptimizedLogGetColumnEncodedStringValue(log_handle, column_index, out data, out size));
                        if (data != IntPtr.Zero)
                        {
                            if (size != 0)
                            {
                                byte[] val = new byte[size];
                                Marshal.Copy(data, val, 0, (int)size);

                                column_value = new Lazy<string>(() => { return Starcounter.TransactionLog.LogReaderImports.DecodeString(OptimizedLogDecodeString, log_handle, val); });
                            }
                            else
                                column_value = "";
                        }
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_BINARY:
                    {
                        IntPtr data;
                        uint size;
                        OptimizedLogException.Test(OptimizedLogGetColumnBinaryValue(log_handle, column_index, out data, out size));
                        if (data != IntPtr.Zero)
                        {
                            byte[] val = new byte[size];
                            Marshal.Copy(data, val, 0, (int)size);
                            column_value = val;
                        }
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_LONG:
                    {
                        long val;
                        bool is_initialized;
                        OptimizedLogException.Test(OptimizedLogGetColumnIntValue(log_handle, column_index, out val, out is_initialized));
                        if ( is_initialized )
                            column_value = val;
                        break;
                    }

                case Starcounter.Internal.sccoredb.STAR_TYPE_ULONG:
                    {
                        long val;
                        bool is_initialized;
                        OptimizedLogException.Test(OptimizedLogGetColumnIntValue(log_handle, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = (ulong)val;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_DECIMAL:
                    {
                        long val;
                        bool is_initialized;
                        OptimizedLogException.Test(OptimizedLogGetColumnIntValue(log_handle, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = Starcounter.Internal.X6Decimal.FromRaw(val);
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_FLOAT:
                    {
                        float val;
                        bool is_initialized;
                        OptimizedLogException.Test(OptimizedLogGetColumnFloatValue(log_handle, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = val;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_DOUBLE:
                    {
                        double val;
                        bool is_initialized;
                        OptimizedLogException.Test(OptimizedLogGetColumnDoubleValue(log_handle, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = val;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_REFERENCE:
                    {
                        long val;
                        bool is_initialized;
                        OptimizedLogException.Test(OptimizedLogGetColumnIntValue(log_handle, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = new Starcounter.TransactionLog.reference { object_id = (ulong)val };
                        break;
                    }
            }
        }

    }
}
