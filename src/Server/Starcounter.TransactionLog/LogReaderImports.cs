using System;
using System.Runtime.InteropServices;

namespace Starcounter.TransactionLog
{
    static class LogReaderImports
    {
        [DllImport("logreader.dll", CharSet = CharSet.Ansi)]
        private extern static int TransactionLogOpen(string db_name, string log_dir, out IntPtr log_handle);

        public static IntPtr TransactionLogOpen(string db_name, string log_dir)
        {
            IntPtr log_handle;
            TransactionLogException.Test(TransactionLogOpen(db_name, log_dir, out log_handle));
            return log_handle;
        }

        [DllImport("logreader.dll", CharSet = CharSet.Ansi)]
        private extern static int TransactionLogOpenAndSeek(string db_name, string log_dir, ref LogPosition pos, out IntPtr log_handle);

        public static IntPtr TransactionLogOpenAndSeek(string db_name, string log_dir, LogPosition pos)
        {
            IntPtr log_handle;
            TransactionLogException.Test(TransactionLogOpenAndSeek(db_name, log_dir, ref pos, out log_handle));
            return log_handle;
        }

        [DllImport("logreader.dll")]
        public extern static void TransactionLogClose(IntPtr log_handle);

        [DllImport("logreader.dll")]
        private extern static int TransactionLogIsEOF(IntPtr log_handle, [MarshalAs(UnmanagedType.I1)] out bool eof);

        public static bool TransactionLogIsEOF(IntPtr log_handle)
        {
            bool eof;
            TransactionLogException.Test(TransactionLogIsEOF(log_handle, out eof));
            return eof;
        }

        [DllImport("logreader.dll", EntryPoint = "TransactionLogMoveNext")]
        private extern static int TransactionLogMoveNext_imp(IntPtr log_handle);

        public static void TransactionLogMoveNext(IntPtr log_handle)
        {
            TransactionLogException.Test(TransactionLogMoveNext_imp(log_handle));
        }

        [DllImport("logreader.dll")]
        public extern static LogPosition TransactionLogGetPosition(IntPtr log_handle);

        [DllImport("logreader.dll", EntryPoint = "TransactionLogGetCurrentTransactionInfo")]
        private extern static int TransactionLogGetCurrentTransactionInfo_imp(IntPtr log_handle, out uint insertupdate_entry_count, out uint delete_entry_count);

        public static void TransactionLogGetCurrentTransactionInfo(IntPtr log_handle, out uint insertupdate_entry_count, out uint delete_entry_count)
        {
            TransactionLogException.Test(
                TransactionLogGetCurrentTransactionInfo_imp(log_handle, out insertupdate_entry_count, out delete_entry_count));
        }

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetDeleteEntryInfo(IntPtr log_handle, uint delete_entry_index, out IntPtr table, out ulong object_id);

        public static void TransactionLogGetDeleteEntryInfo(IntPtr log_handle, uint delete_entry_index, out string table, out ulong object_id)
        {
            IntPtr table_ptr;
            TransactionLogException.Test(TransactionLogGetDeleteEntryInfo(log_handle, delete_entry_index, out table_ptr, out object_id));
            table = Marshal.PtrToStringUni(table_ptr);
        }

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetInsertUpdateEntryInfo(
            IntPtr log_handle, uint insertupdate_entry_index, [MarshalAs(UnmanagedType.I1)] out bool is_insert,
            out IntPtr table, out ulong object_id, out uint columns_count);

        public static void TransactionLogGetInsertUpdateEntryInfo(
            IntPtr log_handle, uint insertupdate_entry_index, out bool is_insert,
            out string table, out ulong object_id, out uint columns_count)
        {
            IntPtr table_ptr;
            TransactionLogException.Test(
                TransactionLogGetInsertUpdateEntryInfo(
                    log_handle, insertupdate_entry_index, out is_insert,
                    out table_ptr, out object_id, out columns_count));

            table = Marshal.PtrToStringUni(table_ptr);
        }

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetInsertUpdateEntryColumnInfo(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out IntPtr column_name, out byte column_type);

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetColumnEncodedStringValue(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out IntPtr data, out uint size);

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetColumnBinaryValue(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out IntPtr data, out uint size);

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetColumnIntValue(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out long val, [MarshalAs(UnmanagedType.I1)] out bool is_initialized);

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetColumnDoubleValue(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out double val, [MarshalAs(UnmanagedType.I1)] out bool is_initialized);

        [DllImport("logreader.dll")]
        private extern static int TransactionLogGetColumnFloatValue(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out float val, [MarshalAs(UnmanagedType.I1)] out bool is_initialized);

        [DllImport("logreader.dll", CharSet = CharSet.Unicode)]
        private extern static int TransactionLogDecodeString(IntPtr log_handle, char[] dst, int dst_max, byte[] src, uint src_len, out int dst_len);

        public delegate int decoder<T>(T log, char[] dst, int dst_max, byte[] src, uint src_len, out int dst_len);
        public static string DecodeString<T>( decoder<T> dec, T log, byte[] encoded_string)
        {
            int guess_size = encoded_string.Length * 2;
            char[] dst = new char[guess_size];
            int dst_len;

            dec(log, dst, dst.Length, encoded_string, (uint)encoded_string.Length, out dst_len);

            if (dst_len == 0)
                throw new System.ArgumentException();

            if (dst_len > guess_size)
            {
                dst = new char[dst_len];
                dec(log, dst, dst.Length, encoded_string, (uint)encoded_string.Length, out dst_len);
            }

            return new string(dst, 0, dst_len);
        }

    public static void TransactionLogGetInsertUpdateEntryColumnInfo(IntPtr log_handle, uint insertupdate_entry_index, uint column_index, out string column_name, out object column_value)
        {
            IntPtr column_name_ptr;
            byte column_type;
            TransactionLogException.Test(TransactionLogGetInsertUpdateEntryColumnInfo(log_handle, insertupdate_entry_index, column_index, out column_name_ptr, out column_type));
            column_name = Marshal.PtrToStringUni(column_name_ptr);
            column_value = null;

            switch (column_type)
            {
                case Starcounter.Internal.sccoredb.STAR_TYPE_STRING:
                    {
                        IntPtr data;
                        uint size;
                        TransactionLogException.Test(TransactionLogGetColumnEncodedStringValue(log_handle, insertupdate_entry_index, column_index, out data, out size));
                        if (data != IntPtr.Zero)
                        {
                            if (size != 0)
                            {
                                byte[] val = new byte[size];
                                Marshal.Copy(data, val, 0, (int)size);

                                column_value = new Lazy<string>(() => { return DecodeString(TransactionLogDecodeString, log_handle, val); });
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
                        TransactionLogException.Test(TransactionLogGetColumnBinaryValue(log_handle, insertupdate_entry_index, column_index, out data, out size));
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
                        TransactionLogException.Test(TransactionLogGetColumnIntValue(log_handle, insertupdate_entry_index, column_index, out val, out is_initialized));
                        if ( is_initialized )
                            column_value = val;
                        break;
                    }

                case Starcounter.Internal.sccoredb.STAR_TYPE_ULONG:
                    {
                        long val;
                        bool is_initialized;
                        TransactionLogException.Test(TransactionLogGetColumnIntValue(log_handle, insertupdate_entry_index, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = (ulong)val;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_DECIMAL:
                    {
                        long val;
                        bool is_initialized;
                        TransactionLogException.Test(TransactionLogGetColumnIntValue(log_handle, insertupdate_entry_index, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = Starcounter.Internal.X6Decimal.FromRaw(val);
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_FLOAT:
                    {
                        float val;
                        bool is_initialized;
                        TransactionLogException.Test(TransactionLogGetColumnFloatValue(log_handle, insertupdate_entry_index, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = val;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_DOUBLE:
                    {
                        double val;
                        bool is_initialized;
                        TransactionLogException.Test(TransactionLogGetColumnDoubleValue(log_handle, insertupdate_entry_index, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = val;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_REFERENCE:
                    {
                        long val;
                        bool is_initialized;
                        TransactionLogException.Test(TransactionLogGetColumnIntValue(log_handle, insertupdate_entry_index, column_index, out val, out is_initialized));
                        if (is_initialized)
                            column_value = new reference { object_id = (ulong)val };
                        break;
                    }
            }
        }

    }
}
