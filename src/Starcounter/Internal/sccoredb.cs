// ***********************************************************************
// <copyright file="sccoredb.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{

    /// <summary>
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class synccommit {

        /// <summary>
        /// </summary>
        [DllImport("synccommit.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_commit_sync(ulong handle, int free);

        [DllImport("synccommit.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void star_process_callback_messages(ulong hlogs);
    }

    /// <summary>
    /// Class sccoredb
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class sccoredb
    {
        /// <summary>
        /// </summary>
        public const ulong MDBIT_OBJECTID = 0; // TODO:

        /// <summary>
        /// </summary>
        public const ulong INVALID_RECORD_REF = 0;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_STRING = 0x01;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_BINARY = 0x02;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_LONG = 0x03;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_ULONG = 0x04;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_DECIMAL = 0x05;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_FLOAT = 0x06;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_DOUBLE = 0x07;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_REFERENCE = 0x08;

        /// <summary>
        /// </summary>
        public const byte STAR_TYPE_KEY = 0x09;

        /// <summary>
        /// </summary>
        public delegate void ON_INDEX_UPDATED(uint context_index, ulong generation);

        /// <summary>
        /// Struct sccoredb_config
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public unsafe struct sccoredb_callbacks
        {
            public void *on_index_updated;
        }

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_set_system_callbacks(
            sccoredb_callbacks* pcallbacks
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint sccoredb_connect(
            uint instance_id, uint context_count, ulong hlogs
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_get_context(
            uint context_index, ulong* pcontext_handle
            );

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct STARI_LAYOUT_INFO {
            /// <summary>
            /// </summary>
            public ulong token;
            
            /// <summary>
            /// </summary>
            public uint column_count;
            
            /// <summary>
            /// </summary>
            public ushort layout_handle;

            /// <summary>
            /// </summary>
            public ushort inherited_layout_handle;
        };

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint stari_context_get_layout_info(
            ulong handle, ushort layout_handle, out STARI_LAYOUT_INFO layout_info
            );

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct STARI_INDEX_INFO {
            /// <summary>
            /// </summary>
            public ulong handle;
            /// <summary>
            /// </summary>
            public ulong token;
            /// <summary>
            /// </summary>
            public ushort column_count;
            /// <summary>
            /// </summary>
            public ushort sort_mask;
            /// <summary>
            /// </summary>
            public ushort flags;
            /// <summary>
            /// </summary>
            //public fixed STARI_COLUMN_DEFINITION column_definitions[8];
            public fixed ulong column_definitions[2 * 8];
        };

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint stari_context_get_layout_infos_by_token(
            ulong handle, ulong token, uint *pcount, STARI_LAYOUT_INFO *playout_infos
            );

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct STARI_COLUMN_INFO
        {
          /// <summary>
          /// </summary>
          public ulong token;
          
          /// <summary>
          /// </summary>
          public ushort index;

          /// <summary>
          /// </summary>
          public byte nullable;

          /// <summary>
          /// </summary>
          public byte type;
        };

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint stari_context_get_column_info(
            ulong handle, ushort layout_handle, ushort index, out STARI_COLUMN_INFO column_info
            );

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal struct STARI_COLUMN_DEFINITION {
            /// <summary>
            /// </summary>
            public byte type;

            /// <summary>
            /// </summary>
            public byte is_nullable;

            /// <summary>
            /// </summary>
            public ulong token;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct STARI_INDEX_INFO_OLD {
            /// <summary>
            /// </summary>
            public ulong handle;
            /// <summary>
            /// </summary>
            public ulong token;
            /// <summary>
            /// </summary>
            public ushort layout_handle;
            /// <summary>
            /// </summary>
            public ushort column_count;
            /// <summary>
            /// </summary>
            public ushort sort_mask;
            /// <summary>
            /// </summary>
            public ushort flags;
            /// <summary>
            /// </summary>
            public fixed ushort column_indexes[8];
        };

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe uint stari_context_get_index_infos_by_setspec_OLD(
            ulong handle, string setspec, ushort layout_handle, uint flags, uint *pic,
            STARI_INDEX_INFO_OLD *piis
            );

        internal const uint STAR_EXCLUDE_INHERITED = 1;

        /// <summary>
        /// </summary>
        public const UInt32 SC_UNIQUE_CONSTRAINT = 1;

        /// <summary>
        /// </summary>
        public const UInt32 SC_INDEXCREATE_UNIQUE_CONSTRAINT = SC_UNIQUE_CONSTRAINT;

        /// <summary>
        /// Gets the context current transaction.
        /// </summary>
        /// <returns>
        /// Always 0. Operation can not fail.
        /// </returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_get_transaction(
            ulong handle, out ulong transaction_handle
            );

        /// <summary>
        /// Sets the context current transaction.
        /// </summary>
        /// <returns>
        /// Always 0. Operation can not fail.
        /// </returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_set_transaction(
            ulong handle, ulong transaction_handle
            );

        /// <summary>
        /// </summary>
        public const uint MDB_TRANSCREATE_LONG_RUNNING = 0x0004;

        /// <summary>
        /// </summary>
        public const uint MDB_TRANSCREATE_READ_ONLY = 0x0008;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_create_transaction(
            uint flags, ulong auto_context_handle, out ulong ptransaction_handle
            );

        /// <summary>
        /// Frees transaction.
        /// </summary>
        /// <returns>
        /// Always 0. Operation can not fail.
        /// </returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        private extern static uint star_transaction_free(ulong handle);

        internal static uint star_transaction_free(ulong handle, ulong verify) {
            var contextHandle = ThreadData.ContextHandle; // Make sure thread is attached.
            if (verify == ThreadData.ObjectVerify)
                return star_transaction_free(handle);
            return Error.SCERRITERATORNOTOWNED;
        }

        /// <summary>
        /// Replaces the transaction with a new one with the same configuration.
        /// </summary>
        /// <returns>
        /// Always 0. Operation can not fail.
        /// </returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_transaction_reset(ulong handle);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint star_context_external_abort(ulong handle);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_insert(
          ulong handle, ushort layout_handle, ulong* pnew_record_id, ulong* pnew_record_ref
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_put_setspec(
            ulong handle, ulong record_id, ulong record_ref, char* value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static uint star_context_insert_system(
            ulong handle, ushort layout_handle, ulong* pnew_record_id,
            ulong* pnew_record_ref
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint stari_transaction_insert_with_id(
          ulong handle, ushort layout_handle, ulong new_record_id, ulong* pnew_record_ref
          );

        /// <summary>
        /// Checks if there are any pending changes on given transaction.
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint star_transaction_is_dirty(ulong handle, int* pvalue);

        internal static unsafe uint star_transaction_is_dirty(
            ulong handle, int* pvalue, ulong verify
            )
        {
            var contextHandle = ThreadData.ContextHandle; // Make sure thread is attached.
            if (verify == ThreadData.ObjectVerify)
                return star_transaction_is_dirty(handle, pvalue);
            return Error.SCERRITERATORNOTOWNED;
        }

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_binary(
          ulong handle, ulong record_id, ulong record_ref, int column_index, byte** pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_decimal(
          ulong handle, ulong record_id, ulong record_ref, int column_index, long* pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_double(
          ulong handle, ulong record_id, ulong record_ref, int column_index, double* pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_long(
          ulong handle, ulong record_id, ulong record_ref, int column_index, long* pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_reference(
          ulong handle, ulong record_id, ulong record_ref, int column_index, ulong* pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_float(
          ulong handle, ulong record_id, ulong record_ref, int column_index, float* pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_string(
          ulong handle, ulong record_id, ulong record_ref, int column_index, byte** pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_ulong(
          ulong handle, ulong record_id, ulong record_ref, int column_index, ulong* pvalue
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_binary(
            ulong handle, ulong record_id, ulong record_ref, int column_index, byte[] value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_decimal(
            ulong handle, ulong record_id, ulong record_ref, int column_index, long value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_double(
            ulong handle, ulong record_id, ulong record_ref, int column_index, double value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_long(
            ulong handle, ulong record_id, ulong record_ref, int column_index, long value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_reference(
          ulong handle, ulong record_id, ulong record_ref, int column_index, ulong value_record_id,
          ulong value_record_ref
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_float(
            ulong handle, ulong record_id, ulong record_ref, int column_index, float value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_default(
            ulong handle, ulong record_id, ulong record_ref, int column_index
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_put_string(
            ulong handle, ulong record_id, ulong record_ref, int column_index, char *value
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_put_ulong(
            ulong handle, ulong record_id, ulong record_ref, int column_index, ulong value
            );

        internal const int STAR_PENDING_INSERT = 1;

        internal const int STAR_DELETED_INSERT = 2;

        internal const int STAR_PENDING_UPDATE = 3;

        internal const int STAR_PENDING_DELETE = 4;

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int star_context_get_trans_state(
          ulong handle, ulong record_id, ulong record_ref
          );

        internal const int DELETE_PENDING = 1;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int star_context_get_trans_flags(
          ulong handle, ulong record_id, ulong record_ref
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int star_context_set_trans_flags(
            ulong handle, ulong record_id, ulong record_ref, int flags
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int star_context_reset_trans_flags(
            ulong handle, ulong record_id, ulong record_ref, int flags
            );

        /// <summary>
        /// </summary>
        public const UInt32 SC_ITERATOR_RANGE_INCLUDE_FIRST_KEY = 0x00000010;

        /// <summary>
        /// </summary>
        public const UInt32 SC_ITERATOR_RANGE_INCLUDE_LAST_KEY = 0x00000020;

        /// <summary>
        /// </summary>
        public const UInt32 SC_ITERATOR_SORTED_DESCENDING = 0x00080000;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_create_iterator(
          ulong handle, ulong index_handle, uint flags, byte* first_key, byte* last_key,
          ulong* piterator_handle
          );

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Calling thread must be the owning thread of the context where the iterator is created
        /// and the current transaction of context must be the transaction the iterator belongs to.
        /// </remarks>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint star_iterator_next(
            ulong handle, ulong* precord_id, ulong* precord_ref
            );

        internal static unsafe uint star_iterator_next(
            ulong handle, ulong* precord_id, ulong* precord_ref, ulong verify
            ) {
            var contextHandle = ThreadData.ContextHandle;
            if (verify == ThreadData.ObjectVerify)
                return star_iterator_next(handle, precord_id, precord_ref);
            return Error.SCERRITERATORNOTOWNED;
        }

        /// <summary>
        /// Frees iterator.
        /// </summary>
        /// <returns>
        /// Always 0. Operation can not fail.
        /// </returns>
        /// <remarks>
        /// Calling thread must be the owning thread of the context where the iterator resides.
        /// </remarks>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint star_iterator_free(ulong handle);

        internal static uint star_iterator_free(ulong handle, ulong verify) {
            var contextHandle = ThreadData.ContextHandle; // Make sure thread is attached.
            if (verify == ThreadData.ObjectVerify)
                return star_iterator_free(handle);
            return Error.SCERRITERATORNOTOWNED;
        }

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_create_filter_iterator(
            ulong handle, ulong index_handle, uint flags, void *first_key,
            void *last_key, ulong filter_handle, void *filter_varstr,
            ulong *piterator_handle
            );

        /// <summary>
        /// Frees filter iterator.
        /// </summary>
        /// <returns>
        /// Always 0. Operation can not fail.
        /// </returns>
        /// <remarks>
        /// Calling thread must be the owning thread of the context where the iterator resides.
        /// </remarks>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint star_filter_iterator_next(
            ulong handle, ulong* precord_id, ulong* precord_ref
            );

        internal static unsafe uint star_filter_iterator_next(
            ulong handle, ulong* precord_id, ulong* precord_ref, ulong verify
            ) {
            var contextHandle = ThreadData.ContextHandle;
            if (verify == ThreadData.ObjectVerify)
                return star_filter_iterator_next(handle, precord_id, precord_ref);
            return Error.SCERRITERATORNOTOWNED;
        }

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_get_index_position_key(
          ulong handle, ulong index_handle, ulong record_id, ulong record_ref, byte** precreate_key
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_recreate_iterator(
          ulong handle, ulong index_handle, uint flags, void* recreate_key, void* last_key,
          ulong* piterator_handle
          );
 
        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_recreate_filter_iterator(
            ulong handle, ulong index_handle, uint flags, void *recreate_key,
            void *last_key, ulong filter_handle, void *filter_varstr,
            ulong *piterator_handle
            );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint star_filter_iterator_free(ulong handle);

        internal static uint star_filter_iterator_free(ulong handle, ulong verify) {
            var contextHandle = ThreadData.ContextHandle; // Make sure thread is attached.
            if (verify == ThreadData.ObjectVerify)
                return star_filter_iterator_free(handle);
            return Error.SCERRITERATORNOTOWNED;
        }

        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe uint star_context_create_update_iterator(
          ulong handle, ulong* piterator_handle
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe uint star_convert_ucs2_to_turbotext(
            string input, uint flags, byte* output, uint outlen
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CharSet = CharSet.Unicode)]
        internal static extern unsafe uint star_convert_ucs2_to_setspectt(
            string input, uint flags, byte* output, uint outlen
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe uint star_context_convert_ucs2_to_turbotext(
          ulong handle, string input, uint flags, byte** pout
          );

        /// <summary>
        /// Compares two UCS-2 strings according to the default collation.
        /// </summary>
        /// <param name="handle">Context handle.</param>
        /// <param name="str1">First string to compare.</param>
        /// <param name="str2">Second string to compare.</param>
        /// <returns>
        /// Comparison result or error. 0 or positive value on success, negative value on failure.
        /// 
        /// To maintain the C runtime convention of comparing strings, the value 1 can be subtracted
        /// from a nonzero return value.Then, the meaning of <0, ==0, and >0 is consistent with the
        /// C runtime.
        ///
        /// On error return value is negative error code.
        /// </returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int star_context_compare_strings(
          ulong handle, string str1, string str2
          );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe uint star_context_lookup(
            ulong handle, ulong record_id, ulong* precord_ref
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern uint star_context_delete(
            ulong handle, ulong record_id, ulong record_ref
            );
    }

    /// <summary>
    /// Class NewCodeGen
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NewCodeGen
    {
        /// <summary>
        /// News the code gen_ load gen code library.
        /// </summary>
        /// <param name="queryID">The query ID.</param>
        /// <param name="pathToGenLibrary">The path to gen library.</param>
        /// <returns>Int32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern Int32 NewCodeGen_LoadGenCodeLibrary(UInt64 queryID, String pathToGenLibrary);

        /// <summary>
        /// News the code gen_ init enumerator.
        /// </summary>
        /// <param name="queryID">The query ID.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>Int32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe Int32 NewCodeGen_InitEnumerator(UInt64 queryID, Byte* queryParameters);

        /// <summary>
        /// News the code gen_ move next.
        /// </summary>
        /// <param name="queryID">The query ID.</param>
        /// <param name="oid">The oid.</param>
        /// <param name="eti">The eti.</param>
        /// <param name="currentCCI">The current CCI.</param>
        /// <returns>Int32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe Int32 NewCodeGen_MoveNext(UInt64 queryID, UInt64* oid, UInt64* eti, UInt16* currentCCI);

        /// <summary>
        /// News the code gen_ reset.
        /// </summary>
        /// <param name="queryID">The query ID.</param>
        /// <returns>Int32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern Int32 NewCodeGen_Reset(UInt64 queryID);
    }

    /// <summary>
    /// Class CodeGenFilterNativeInterface
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class CodeGenFilterNativeInterface
    {

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void star_register_expected_layout(
            ushort layout_handle, ushort expected_layout_handle
            );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe UInt32 star_context_create_filter(
            ulong handle,
            ushort tableId,
            UInt32 stackSize,
            UInt32 varCount,
            UInt32 instrCount,
            UInt32* instrstr,
            UInt64* ph
        );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern UInt32 star_filter_release(UInt64 handle);
    }

    /// <summary>
    /// Delegate SqlConn_GetQueryUniqueId_Type
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="uniqueQueryId">The unique query id.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>UInt32.</returns>
    internal unsafe delegate UInt32 SqlConn_GetQueryUniqueId_Type(
        Char* query,
        UInt64* uniqueQueryId,
        UInt32* flags);

    /// <summary>
    /// Delegate SqlConn_GetResults_Type
    /// </summary>
    /// <param name="uniqueQueryId">The unique query id.</param>
    /// <param name="queryParams">The query params.</param>
    /// <param name="results">The results.</param>
    /// <param name="resultsMaxBytes">The results max bytes.</param>
    /// <param name="resultsNum">The results num.</param>
    /// <param name="recreationKey">The recreation key.</param>
    /// <param name="recreationKeyMaxBytes">The recreation key max bytes.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>UInt32.</returns>
    internal unsafe delegate UInt32 SqlConn_GetResults_Type(
        UInt64 uniqueQueryId,
        Byte* queryParams,
        Byte* results,
        UInt32 resultsMaxBytes,
        UInt32* resultsNum,
        Byte* recreationKey,
        UInt32 recreationKeyMaxBytes,
        UInt32* flags);

    /// <summary>
    /// Delegate SqlConn_GetInfo_Type
    /// </summary>
    /// <param name="infoType">Type of the info.</param>
    /// <param name="param">The param.</param>
    /// <param name="result">The result.</param>
    /// <param name="maxBytes">The max bytes.</param>
    /// <param name="outLenBytes">The out len bytes.</param>
    /// <returns>UInt32.</returns>
    internal unsafe delegate UInt32 SqlConn_GetInfo_Type(
        Byte infoType,
        UInt64 param,
        Byte* result,
        UInt32 maxBytes,
        UInt32* outLenBytes);

#if false
    /// <summary>
    /// Struct SC_SQL_CALLBACKS
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SC_SQL_CALLBACKS
    {
        /// <summary>
        /// The p SQL conn_ get query unique id
        /// </summary>
        internal SqlConn_GetQueryUniqueId_Type pSqlConn_GetQueryUniqueId;
        /// <summary>
        /// The p SQL conn_ get results
        /// </summary>
        internal SqlConn_GetResults_Type pSqlConn_GetResults;
        /// <summary>
        /// The p SQL conn_ get info
        /// </summary>
        internal SqlConn_GetInfo_Type pSqlConn_GetInfo;
    }
#endif

#if true
    /// <summary>
    /// Class SqlConnectivityInterface
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class SqlConnectivityInterface
    {
        // Connectivity constants.
        /// <summary>
        /// The RECREATIO n_ KE y_ MA x_ BYTES
        /// </summary>
        internal const UInt16 RECREATION_KEY_MAX_BYTES = 4096; // Maximum length in bytes of recreation key.
        /// <summary>
        /// The MA x_ HIT s_ PE r_ PAG e_ SORTING
        /// </summary>
        internal const UInt32 MAX_HITS_PER_PAGE_SORTING = 8196; // Maximum amount of hits per page in case of sorting.
        /// <summary>
        /// The MA x_ STATU s_ STRIN g_ LEN
        /// </summary>
        internal const UInt32 MAX_STATUS_STRING_LEN = 8196; // Maximum length of the status string.

        // SQL query flags.
        /// <summary>
        /// The FLA g_ MOR e_ RESULTS
        /// </summary>
        internal const UInt32 FLAG_MORE_RESULTS = 1;
        /// <summary>
        /// The FLA g_ HA s_ PROJECTION
        /// </summary>
        internal const UInt32 FLAG_HAS_PROJECTION = 2;
        /// <summary>
        /// The FLA g_ HA s_ SORTING
        /// </summary>
        internal const UInt32 FLAG_HAS_SORTING = 4;
        /// <summary>
        /// The FLA g_ HA s_ AGGREGATION
        /// </summary>
        internal const UInt32 FLAG_HAS_AGGREGATION = 8;
        /// <summary>
        /// The FLA g_ FETC h_ VARIABLE
        /// </summary>
        internal const UInt32 FLAG_FETCH_VARIABLE = 16;
        /// <summary>
        /// The FLA g_ RECREATIO n_ KE y_ VARIABLE
        /// </summary>
        internal const UInt32 FLAG_RECREATION_KEY_VARIABLE = 32;
        /// <summary>
        /// The FLA g_ POS t_ MANAGE d_ FILTER
        /// </summary>
        internal const UInt32 FLAG_POST_MANAGED_FILTER = 64;
        /// <summary>
        /// The FLA g_ LAS t_ FETCH
        /// </summary>
        internal const UInt32 FLAG_LAST_FETCH = 128;

        // Flags that are used to get SQL status information.
        /// <summary>
        /// The GE t_ LAS t_ ERROR
        /// </summary>
        internal const Byte GET_LAST_ERROR = 0;
        /// <summary>
        /// The GE t_ ENUMERATO r_ EXE c_ PLAN
        /// </summary>
        internal const Byte GET_ENUMERATOR_EXEC_PLAN = 1;
        /// <summary>
        /// The GE t_ QUER y_ CACH e_ STATUS
        /// </summary>
        internal const Byte GET_QUERY_CACHE_STATUS = 2;
        /// <summary>
        /// The GE t_ FETC h_ VARIABLE
        /// </summary>
        internal const Byte GET_FETCH_VARIABLE = 3;
        /// <summary>
        /// The GE t_ RECREATIO n_ KE y_ VARIABLE
        /// </summary>
        internal const Byte GET_RECREATION_KEY_VARIABLE = 4;
        /// <summary>
        /// The PRIN t_ PROFILE r_ RESULTS
        /// </summary>
        internal const Byte PRINT_PROFILER_RESULTS = 5;

#if false
        /// <summary>
        /// SQLs the conn_ init managed functions.
        /// </summary>
        /// <param name="managedSqlFunctions">The managed SQL functions.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern UInt32 SqlConn_InitManagedFunctions(ref SC_SQL_CALLBACKS managedSqlFunctions);

        /// <summary>
        /// SQLs the conn_ get query unique id.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="uniqueQueryId">The unique query id.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe UInt32 SqlConn_GetQueryUniqueId(
            String query, // [IN] SQL query string.
            UInt64* uniqueQueryId, // [OUT] Unique query ID, Fixed-size 8 bytes.
            UInt32* flags // [OUT] Populated query flags, Fixed-size 4 bytes.
            );

        /// <summary>
        /// SQLs the conn_ get results.
        /// </summary>
        /// <param name="uniqueQueryId">The unique query id.</param>
        /// <param name="queryParams">The query params.</param>
        /// <param name="results">The results.</param>
        /// <param name="resultsMaxBytes">The results max bytes.</param>
        /// <param name="resultsNum">The results num.</param>
        /// <param name="recreationKey">The recreation key.</param>
        /// <param name="recreationKeyMaxBytes">The recreation key max bytes.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe UInt32 SqlConn_GetResults(
            UInt64 uniqueQueryId, // [IN] Uniquely identifies the SQL query on server.
            Byte* queryParams, // [IN] Buffer containing SQL query parameters (total length in bytes in first 4 bytes).
            Byte* results, // [OUT] Buffer that should contain query execution results.
            UInt32 resultsMaxBytes, // [IN] Maximum size in bytes of the buffer (needed for allocation in Blast).
            UInt32* resultsNum, // [IN] Number of hits to retrieve. [OUT] Number of fetched hits. Fixed-size 4 bytes data.
            Byte* recreationKey, // [IN] Given key to recreate enumerator. [OUT] Serialized recreation key for further use. Total length in bytes in first 4 bytes.
            UInt32 recreationKeyMaxBytes, // [IN] Maximum size in bytes of the buffer (needed for allocation in Blast).
            UInt32* flags // [IN] SQL flags that are needed to pass. [OUT] Contains returned SQL flags. Fixed-size 4 bytes data.
            );

        /// <summary>
        /// SQLs the conn_ get info.
        /// </summary>
        /// <param name="infoType">Type of the info.</param>
        /// <param name="param">The param.</param>
        /// <param name="results">The results.</param>
        /// <param name="maxBytes">The max bytes.</param>
        /// <param name="outLenBytes">The out len bytes.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe UInt32 SqlConn_GetInfo(
            Byte infoType, // [IN] Needed SQL information type.
            UInt64 param, // [IN] Additional parameter (e.g. SQL unique query ID).
            Byte* results, // [OUT] Obtained information.
            UInt32 maxBytes, // [IN] Maximum size in bytes of the result buffer (needed for allocation in Blast).
            UInt32* outLenBytes // [OUT] Length in bytes of the result data.
            );
#endif

        // Types of variable in query.
        /// <summary>
        /// The QUER y_ VARTYP e_ DEFINED
        /// </summary>
        internal const Byte QUERY_VARTYPE_DEFINED = 1;
        /// <summary>
        /// The QUER y_ VARTYP e_ INT
        /// </summary>
        internal const Byte QUERY_VARTYPE_INT = 2;
        /// <summary>
        /// The QUER y_ VARTYP e_ UINT
        /// </summary>
        internal const Byte QUERY_VARTYPE_UINT = 3;
        /// <summary>
        /// The QUER y_ VARTYP e_ DOUBLE
        /// </summary>
        internal const Byte QUERY_VARTYPE_DOUBLE = 4;
        /// <summary>
        /// The QUER y_ VARTYP e_ DECIMAL
        /// </summary>
        internal const Byte QUERY_VARTYPE_DECIMAL = 5;
        /// <summary>
        /// The QUER y_ VARTYP e_ STRING
        /// </summary>
        internal const Byte QUERY_VARTYPE_STRING = 6;
        /// <summary>
        /// The QUER y_ VARTYP e_ OBJECT
        /// </summary>
        internal const Byte QUERY_VARTYPE_OBJECT = 7;
        /// <summary>
        /// The QUER y_ VARTYP e_ BINARY
        /// </summary>
        internal const Byte QUERY_VARTYPE_BINARY = 8;
        /// <summary>
        /// The QUER y_ VARTYP e_ DATETIME
        /// </summary>
        internal const Byte QUERY_VARTYPE_DATETIME = 9;
        /// <summary>
        /// The QUER y_ VARTYP e_ BOOLEAN
        /// </summary>
        internal const Byte QUERY_VARTYPE_BOOLEAN = 10;
    }
#endif
}
