using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;


namespace Starcounter.TransactionLog
{
    static class LogReaderImports
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool table_predicate_delegate([MarshalAs(UnmanagedType.LPWStr)] string table);

        [DllImport("logreader.dll", CharSet = CharSet.Ansi)]
        private extern static int TransactionLogOpen(string db_name, string log_dir, table_predicate_delegate table_predicate, out IntPtr log_handle);

        public static IntPtr TransactionLogOpen(string db_name, string log_dir, table_predicate_delegate table_predicate)
        {
            IntPtr log_handle;
            TransactionLogException.Test(TransactionLogOpen(db_name, log_dir, table_predicate, out log_handle));
            return log_handle;
        }

        [DllImport("logreader.dll", CharSet = CharSet.Ansi)]
        private extern static int TransactionLogOpenAndSeek(string db_name, string log_dir, ref LogPosition pos, table_predicate_delegate table_predicate, out IntPtr log_handle);

        public static IntPtr TransactionLogOpenAndSeek(string db_name, string log_dir, LogPosition pos, table_predicate_delegate table_predicate)
        {
            IntPtr log_handle;
            TransactionLogException.Test(TransactionLogOpenAndSeek(db_name, log_dir, ref pos, table_predicate, out log_handle));
            return log_handle;
        }

        [DllImport("logreader.dll")]
        public extern static void TransactionLogClose(IntPtr log_handle);

        [DllImport("logreader.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public extern static bool TransactionLogIsEOF(IntPtr log_handle);

        [DllImport("logreader.dll", EntryPoint = "TransactionLogMoveNext")]
        private extern static int TransactionLogMoveNext_imp(IntPtr log_handle);

        public static void TransactionLogMoveNext(IntPtr log_handle)
        {
            TransactionLogException.Test(TransactionLogMoveNext_imp(log_handle));
        }

        [DllImport("logreader.dll")]
        public extern static LogPosition TransactionLogGetPosition(IntPtr log_handle);

        [DllImport("logreader.dll")]
        private extern static transaction_info TransactionLogGetCurrentTransactionInfo(IntPtr log_handle);

        [DllImport("logreader.dll", CharSet = CharSet.Unicode)]
        internal extern static int TransactionLogDecodeString(IntPtr log_handle, char[] dst, int dst_max, byte[] src, uint src_len, out int dst_len);

        internal delegate int decoder(IntPtr log, char[] dst, int dst_max, byte[] src, uint src_len, out int dst_len);
        internal static string DecodeString(decoder dec, IntPtr log, byte[] encoded_string)
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

        internal static Starcounter.TransactionLog.ColumnUpdate decode_column_update(IntPtr log_handle, LogReaderImports.column_update cu, MetadataCache meta_cache, decoder dec)
        {
            object column_value = null;
            switch (cu.column_type)
            {
                case Starcounter.Internal.sccoredb.STAR_TYPE_STRING:
                    {
                        IntPtr data = cu.value.blob_value.data;
                        uint size = cu.value.blob_value.size;
                        if (data != IntPtr.Zero)
                        {
                            if (size != 0)
                            {
                                byte[] val = new byte[size];
                                Marshal.Copy(data, val, 0, (int)size);

                                column_value = new Lazy<string>(() => { return DecodeString(dec, log_handle, val); });
                            }
                            else
                                column_value = "";
                        }
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_BINARY:
                    {
                        IntPtr data = cu.value.blob_value.data;
                        uint size = cu.value.blob_value.size;
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
                        if (cu.value.long_value.is_initialized != 0)
                            column_value = cu.value.long_value.data;
                        break;
                    }

                case Starcounter.Internal.sccoredb.STAR_TYPE_ULONG:
                    {
                        if (cu.value.long_value.is_initialized != 0)
                            column_value = (ulong)cu.value.long_value.data;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_DECIMAL:
                    {
                        if (cu.value.long_value.is_initialized != 0)
                            column_value = Starcounter.Internal.X6Decimal.FromRaw(cu.value.long_value.data);
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_FLOAT:
                    {
                        if (cu.value.float_value.is_initialized != 0)
                            column_value = cu.value.float_value.data;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_DOUBLE:
                    {
                        if (cu.value.double_value.is_initialized != 0)
                            column_value = cu.value.double_value.data;
                        break;
                    }
                case Starcounter.Internal.sccoredb.STAR_TYPE_REFERENCE:
                    {
                        if (cu.value.long_value.is_initialized != 0)
                            column_value = new Reference { ObjectID = (ulong)cu.value.long_value.data };
                        break;
                    }
            }

            return new Starcounter.TransactionLog.ColumnUpdate
            {
                Name = meta_cache[cu.column_name],
                Value = column_value
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct blob_value_type
        {
            public IntPtr data;
            public uint size;
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct long_value_type
        {
            public long data;
            public byte is_initialized;
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct double_value_type
        {
            public double data;
            public byte is_initialized;
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct float_value_type
        {
            public float data;
            public byte is_initialized;
        };


        [StructLayout(LayoutKind.Explicit)]
        internal struct column_value
        {

            [FieldOffset(0)]
            public blob_value_type blob_value;
            [FieldOffset(0)]
            public long_value_type long_value;
            [FieldOffset(0)]
            public double_value_type double_value;
            [FieldOffset(0)]
            public float_value_type float_value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct column_update
        {
            public IntPtr column_name;
            public column_value value;
            public uint column_index;
            public byte column_type;
        };


        [StructLayout(LayoutKind.Sequential)]
        internal struct insertupdate_entry_info
        {
            public IntPtr table;
            public IntPtr updates;
            public ulong object_id;
            public uint columns_updates_count;
            [MarshalAs(UnmanagedType.I1)]
            public bool is_insert;
        };


        [StructLayout(LayoutKind.Sequential)]
        private struct delete_entry_info
        {
            public IntPtr table;
            public ulong object_id;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct transaction_info
        {
            public IntPtr inserts_updates;
            public IntPtr deletes;
            public ulong metadata_generation;
            public uint insertupdate_entry_count;
            public uint delete_entry_count;
        }

        unsafe public static TransactionData TransactionLogGetCurrentTransactionInfoExtended(IntPtr log_handle, MetadataCache meta_cache)
        {
            LogReaderImports.transaction_info ti = LogReaderImports.TransactionLogGetCurrentTransactionInfo(log_handle);
            meta_cache.notify_on_new_generation(ti.metadata_generation);

            TransactionData res = new TransactionData
            {
                Creates = new List<CreateRecordEntry>((int)ti.insertupdate_entry_count),
                Updates = new List<UpdateRecordEntry>((int)ti.insertupdate_entry_count),
                Deletes = new List<DeleteRecordEntry>((int)ti.delete_entry_count)
            };

            for (int i = 0; i < ti.insertupdate_entry_count; ++i)
            {
                unsafe
                {
                    insertupdate_entry_info* e = (insertupdate_entry_info*)ti.inserts_updates + i;

                    string table = meta_cache[e->table];

                    var updates = new Starcounter.TransactionLog.ColumnUpdate[e->columns_updates_count];

                    for (int j = 0; j < e->columns_updates_count; ++j)
                    {
                        column_update* cu = (column_update*)e->updates + j;
                        updates[j] = decode_column_update(log_handle, *cu, meta_cache, TransactionLogDecodeString);
                    }


                    if (e->is_insert)
                        res.Creates.Add(new CreateRecordEntry
                        {
                            Table = table,
                            Key = new Reference { ObjectID = e->object_id },
                            Columns = updates
                        });
                    else
                        res.Updates.Add(new UpdateRecordEntry
                        {
                            Table = table,
                            Key = new Reference { ObjectID = e->object_id },
                            Columns = updates
                        });
                }

            }

            for (int i = 0; i < ti.delete_entry_count; ++i)
            {
                unsafe
                {
                    delete_entry_info* d = (delete_entry_info*)ti.deletes + i;

                    string table = meta_cache[d->table];

                    res.Deletes.Add(new DeleteRecordEntry { Table = table, Key = new Reference { ObjectID = d->object_id } });
                }
            }

            return res;

        }

    }
}
