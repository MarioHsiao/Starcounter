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

    [SuppressUnmanagedCodeSecurity]
    public static class filter {

        /// <summary>
        /// </summary>
        [DllImport("filter.dll")]
        public extern static uint init_filter_lib(ulong hmenv, ulong hlogs);
    }

    /// <summary>
    /// Class sccoredb
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class sccoredb
    {

        /// <summary>
        /// The MDBI t_ OBJECTID
        /// </summary>
        public const ulong MDBIT_OBJECTID = 0; // TODO:

        /// <summary>
        /// The INVALI d_ DEFINITIO n_ ADDR
        /// </summary>
        public const ulong INVALID_DEFINITION_ADDR = 0xFFFFFFFFFF;
        /// <summary>
        /// The INVALI d_ RECOR d_ ADDR
        /// </summary>
        public const ulong INVALID_RECORD_ADDR = 0xFFFFFFFFFF;

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
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint star_get_additional_error_info(char** padditional_error_infomation);

        /// <summary>
        /// Delegate ON_NEW_SCHEMA
        /// </summary>
        /// <param name="generation">The generation.</param>
        public delegate void ON_NEW_SCHEMA(ulong generation);

        /// <summary>
        /// </summary>
        public delegate uint ON_NO_TRANSACTION();

        /// <summary>
        /// Struct sccoredb_config
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public unsafe struct STAR_SYSTEM_CALLBACKS
        {
            /// <summary>
            /// </summary>
            public void* yield;

            /// <summary>
            /// </summary>
            public void* ping;

            /// <summary>
            /// </summary>
            public void* query_highmem_cond;
            
            /// <summary>
            /// The on_new_schema
            /// </summary>
            public void* on_new_schema;

            /// <summary>
            /// </summary>
            public void* on_thread_not_attached;

            /// <summary>
            /// </summary>
            public void* on_no_transaction;
        }

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint star_set_system_callbacks(STAR_SYSTEM_CALLBACKS* pcallbacks);

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_LOAD_DATABASE = 0x00100000;

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_USE_BUFFERED_IO = 0x00200000;

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_ENABLE_CHECK_FILE_ON_LOAD = 0x00010000;

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_ENABLE_CHECK_FILE_ON_CHECKP = 0x00020000;

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_ENABLE_CHECK_FILE_ON_BACKUP = 0x00040000;

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_ENABLE_CHECK_MEMORY_ON_CHECKP = 0x00080000;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint sccoredb_connect(uint flags, uint scheduler_count, ulong hmenv, ulong hlogs, void* hperf);

        /// <summary>
        /// </summary>
        public const uint SCCOREDB_UNLOAD_DATABASE = 0x00200000;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint sccoredb_disconnect(uint flags);

#if false
        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint star_attach(ulong thread_id, void* thread_data192, int init);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_detach(uint release);

        /// <summary>
        /// </summary>
        public const uint STAR_RELEASE_NOTHING = 0;

        /// <summary>
        /// </summary>
        public const uint STAR_RELEASE_SNAPHOT = 1;

        /// <summary>
        /// </summary>
        public const uint STAR_RELEASE_ALL = 2;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_reset();
#endif

        /// <summary>
        /// </summary>
        /// <param name="scheduler_index">The scheduler_index.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_advance_clock(uint scheduler_index);

        /// <summary>
        /// </summary>
        /// <param name="pCallAgainIfStillIdle">The p call again if still idle.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint star_idle_task(int* pcall_again_if_still_idle);

#if false
        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_alert_low_memory(uint resource_type);
#endif

        /// <summary>
        /// The MD b_ ATTRFLA g_ NULLABLE
        /// </summary>
        public const ushort MDB_ATTRFLAG_NULLABLE = 0x0040;

        /// <summary>
        /// </summary>
        public const UInt32 SC_UNIQUE_CONSTRAINT = 1;

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public unsafe struct STARI_INDEX_INFO {
            /// <summary>
            /// The handle
            /// </summary>
            public UInt64 handle;
            /// <summary>
            /// The name
            /// </summary>
            public UInt64 name_token;
            /// <summary>
            /// The attribute count
            /// </summary>
            public Int16 attributeCount;
            /// <summary>
            /// The sort mask
            /// </summary>
            public UInt16 sortMask;
            /// <summary>
            /// The attr index arr_0
            /// </summary>
            public Int16 attrIndexArr_0;
            /// <summary>
            /// The attr index arr_1
            /// </summary>
            public Int16 attrIndexArr_1;
            /// <summary>
            /// The attr index arr_2
            /// </summary>
            public Int16 attrIndexArr_2;
            /// <summary>
            /// The attr index arr_3
            /// </summary>
            public Int16 attrIndexArr_3;
            /// <summary>
            /// The attr index arr_4
            /// </summary>
            public Int16 attrIndexArr_4;
            /// <summary>
            /// The attr index arr_5
            /// </summary>
            public Int16 attrIndexArr_5;
            /// <summary>
            /// The attr index arr_6
            /// </summary>
            public Int16 attrIndexArr_6;
            /// <summary>
            /// The attr index arr_7
            /// </summary>
            public Int16 attrIndexArr_7;
            /// <summary>
            /// The attr index arr_8
            /// </summary>
            public Int16 attrIndexArr_8;
            /// <summary>
            /// The attr index arr_9
            /// </summary>
            public Int16 attrIndexArr_9;
            /// <summary>
            /// The attr index arr_10
            /// </summary>
            public Int16 attrIndexArr_10;

            /// <summary>
            /// </summary>
            public UInt16 flags;
        };

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct STAR_SORT_SPEC_ELEM
        {
            /// <summary>
            /// The column_index
            /// </summary>
            public short column_index;
            /// <summary>
            /// The sort
            /// </summary>
            public byte sort; // 0 ascending, 1 descending.
        };

        /// <summary>
        /// </summary>
        /// <param name="table_id"></param>
        /// <param name="sort_spec">The sort_spec.</param>
        /// <param name="pii">The pii.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 stari_get_index_info_by_sort(
            ushort table_id,
            STAR_SORT_SPEC_ELEM* sort_spec,
            STARI_INDEX_INFO* pii
            );

        /// <summary>
        /// </summary>
        /// <param name="table_id"></param>
        /// <param name="pic">The pic.</param>
        /// <param name="piis">The piis.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint stari_get_index_infos(
            ushort table_id,
            uint* pic,
            STARI_INDEX_INFO* piis
            );

        /// <summary>
        /// </summary>
        public const UInt32 SC_INDEXCREATE_UNIQUE_CONSTRAINT = SC_UNIQUE_CONSTRAINT;

        /// <summary>
        /// </summary>
        /// <param name="unlock_tran_from_thread">The unlock_tran_from_thread.</param>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_set_current_transaction(
            int unlock_tran_from_thread,
            ulong handle,
            ulong verify
            );

        /// <summary>
        /// </summary>
        public const uint MDB_TRANSCREATE_MERGING_WRITES = 0x0004;

        /// <summary>
        /// </summary>
        public const uint MDB_TRANSCREATE_READ_ONLY = 0x0008;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_create_transaction(
            uint flags,
            out ulong handle,
            out ulong verify
            );

        internal const uint MDB_TRANSCREATE_SUPPRESS_HOOKS = 0x0040;

        /// <summary>
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="lock_tran_on_thread">The lock_tran_on_thread.</param>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_create_transaction_and_set_current(
            uint flags,
            int lock_tran_on_thread,
            out ulong handle,
            out ulong verify
            );

        /// <summary>
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="verify">The verify.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_free_transaction(
            ulong handle,
            ulong verify
            );

        /// <summary>
        /// </summary>
        /// <param name="tran_locked_on_thread">The tran_locked_on_thread.</param>
        /// <param name="hiter">The hiter.</param>
        /// <param name="viter">The viter.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_begin_commit(
            int tran_locked_on_thread,
            out ulong hiter,
            out ulong viter
            );

        /// <summary>
        /// </summary>
        /// <param name="tran_locked_on_thread">The tran_locked_on_thread.</param>
        /// <param name="detach_and_free">The detach_and_free.</param>
        /// <param name="flags">flags.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_complete_commit(
            int tran_locked_on_thread, int detach_and_free
            );

        /// <summary>
        /// </summary>
        /// <param name="tran_locked_on_thread">The tran_locked_on_thread.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_abort_commit(
            int tran_locked_on_thread
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_rollback();

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_external_abort();

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_insert(
            ushort table_id,
            ulong* pnew_oid,
            ulong* pnew_addr
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        internal unsafe extern static uint star_insert_system(
            ushort table_id,
            ulong* pnew_oid,
            ulong* pnew_addr
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_replace(
            ulong record_id,
            ulong record_addr,
            ushort table_id
            );

        public const uint STAR_HOOKS_ON_COMMIT_DELETE = 0x04;
        public const uint STAR_HOOKS_ON_COMMIT_INSERT = 0x08;
        public const uint STAR_HOOKS_ON_COMMIT_UPDATE = 0x10;

        /// <summary>
        /// Allows the host to configure the kernel runtime to assure any
        /// operation represented in the <paramref name="commit_hook_mask"/>
        /// signals to the host to invoke the corresponding hooks on table
        /// <paramref name="name"/> when a transaction commit. 
        /// </summary>
        /// <param name="name">The name of the table to which the mask should
        /// be applied.</param>
        /// <param name="commit_hook_mask">The mask to apply</param>
        /// <returns>Zero on success; an error code otherwise.</returns>
        /// <seealso cref="STAR_HOOKS_ON_COMMIT_DELETE"/>
        /// <seealso cref="STAR_HOOKS_ON_COMMIT_INSERT"/>
        /// <seealso cref="STAR_HOOKS_ON_COMMIT_UPDATE"/>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_set_commit_hooks(ulong name, uint commit_hook_mask);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint SCObjectFakeWrite(ulong record_id, ulong record_addr);

        /// <summary>
        /// The MDB_ data value flag_ null
        /// </summary>
        public const ushort Mdb_DataValueFlag_Null = 0x0001;

        /// <summary>
        /// The MDB_ data value flag_ error
        /// </summary>
        public const ushort Mdb_DataValueFlag_Error = 0x1000;

        /// <summary>
        /// The MDB_ data value flag_ would block
        /// </summary>
        public const ushort Mdb_DataValueFlag_WouldBlock = 0x2000;

        /// <summary>
        /// The MDB_ data value flag_ exceptional
        /// </summary>
        public const ushort Mdb_DataValueFlag_Exceptional = Mdb_DataValueFlag_Error;

        /// <summary>
        /// Checks if there are any pending changes on given transaction.
        /// </summary>
        /// <param name="hTrans">Transaction handle.</param>
        /// <param name="verify">Transaction handle.</param>
        /// <param name="pValue">True if there are any changes.</param>
        /// <returns>Error code.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 star_transaction_is_dirty(
            UInt64 hTrans,
            UInt64 verify,
            Int32* pValue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_binary(ulong record_id, ulong record_addr, int column_index, /*OUT*/ byte **pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_decimal(ulong record_id, ulong record_addr, int column_index, long *pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_double(ulong record_id, ulong record_addr, int column_index, double* pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_long(ulong record_id, ulong record_addr, int column_index, long *pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_reference(
	        ulong record_id,
            ulong record_addr,
	        int column_index,
            ulong* ptarget_record_id,
            ulong* ptarget_record_addr,
            ushort* ptarget_table_id
	        );            

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_float(ulong record_id, ulong record_addr, int column_index, float* pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_string(ulong record_id, ulong record_addr, int column_index, byte** pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_get_ulong(ulong record_id, ulong record_addr, int column_index, ulong* pvalue);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_binary(ulong record_id, ulong record_addr, int column_index, byte[] value);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static UInt32 star_put_decimal(ulong record_id, ulong record_addr, int column_index, long value);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_double(ulong record_id, ulong record_addr, int column_index, double value);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_long(ulong record_id, ulong record_addr, int column_index, long value);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_reference(ulong record_id, ulong record_addr, int column_index, ulong target_record_id, ulong target_record_addr);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_float(ulong record_id, ulong record_addr, int column_index, float value);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_default(ulong record_id, ulong record_addr, int column_index);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_put_string(ulong record_id, ulong record_addr, int column_index, char *value);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static uint star_put_ulong(ulong record_id, ulong record_addr, int column_index, ulong value);

        /// <summary>
        /// The S c_ ITERATO r_ RANG e_ INCLUD e_ LSKEY
        /// </summary>
        public const UInt32 SC_ITERATOR_RANGE_INCLUDE_LSKEY = 0x00000010;

        /// <summary>
        /// The S c_ ITERATO r_ RANG e_ INCLUD e_ GRKEY
        /// </summary>
        public const UInt32 SC_ITERATOR_RANGE_INCLUDE_GRKEY = 0x00000020;

        /// <summary>
        /// The S c_ ITERATO r_ SORTE d_ DESCENDING
        /// </summary>
        public const UInt32 SC_ITERATOR_SORTED_DESCENDING = 0x00080000;

        /// <summary>
        /// SCs the iterator create.
        /// </summary>
        /// <param name="hIndex">Index of the h.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="lesserKey">The lesser key.</param>
        /// <param name="greaterKey">The greater key.</param>
        /// <param name="ph">The ph.</param>
        /// <param name="pv">The pv.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 star_create_iterator(
            UInt64 hIndex,
            UInt32 flags,
            Byte* lesserKey,
            Byte* greaterKey,
            UInt64* ph,
            UInt64* pv
        );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 create_filter_iterator(
            UInt64 hIndex,
            UInt32 flags,
            Byte* lesserKey,
            Byte* greaterKey,
            UInt64 hfilter,
            IntPtr varstr,
            UInt64* ph,
            UInt64* pv
        );

        /// <summary>
        /// SCs the iterator next.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="v">The v.</param>
        /// <param name="pObjectOID">The p object OID.</param>
        /// <param name="pObjectETI">The p object ETI.</param>
        /// <param name="pClassIndex">Index of the p class.</param>
        /// <param name="pData">The p data.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 star_iterator_next(
            UInt64 h,
            UInt64 v,
            UInt64* pObjectOID,
            UInt64* pObjectETI,
            UInt16* pClassIndex,
            UInt64* pData
        );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 filter_iterator_next(
            UInt64 h,
            UInt64 v,
            UInt64* pObjectOID,
            UInt64* pObjectETI,
            UInt16* pClassIndex,
            UInt64* pData
        );

#if false
        /// <summary>
        /// SCs the iterator fill up.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="v">The v.</param>
        /// <param name="results">The results.</param>
        /// <param name="resultsMaxBytes">The results max bytes.</param>
        /// <param name="resultsNum">The results num.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 SCIteratorFillUp(
            UInt64 h,
            UInt64 v,
            Byte* results,
            UInt32 resultsMaxBytes,
            UInt32* resultsNum,
            UInt32* flags);
#endif

        /// <summary>
        /// </summary>
        /// <param name="index_addr">The index_addr.</param>
        /// <param name="record_id">The record_id.</param>
        /// <param name="record_addr">The record_addr.</param>
        /// <param name="precreate_key">The precreate_key.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 star_get_index_position_key(
            UInt64 index_addr,
            UInt64 record_id,
            UInt64 record_addr,
            Byte** precreate_key
            );

        /// <summary>
        /// </summary>
        /// <param name="hindex">The hindex.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="recreate_key">The recreate_key.</param>
        /// <param name="last_key">The last_key.</param>
        /// <param name="ph">The ph.</param>
        /// <param name="pv">The pv.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 star_recreate_iterator(
            UInt64 hindex,
            UInt32 flags,
            Byte* recreate_key,
            Byte* last_key,
            UInt64* ph,
            UInt64* pv
            );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 recreate_filter_iterator(
            UInt64 hindex,
            UInt32 flags,
            Byte* recreate_key,
            Byte* last_key,
            UInt64 hfilter,
            Byte* varstr,
            UInt64* ph,
            UInt64* pv
            );

#if false
        /// <summary>
        /// Sccoredb_iterator_get_local_times the specified iter_handle.
        /// </summary>
        /// <param name="iter_handle">The iter_handle.</param>
        /// <param name="iter_verify">The iter_verify.</param>
        /// <param name="plocal_time">The plocal_time.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 sccoredb_iterator_get_local_time(
            UInt64 iter_handle,
            UInt64 iter_verify,
            UInt32* plocal_time
            );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 filter_iterator_get_local_time(
            UInt64 iter_handle,
            UInt64 iter_verify,
            UInt32* plocal_time
            );
#endif

        /// <summary>
        /// SCs the iterator free.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="v">The v.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static UInt32 star_iterator_free(
            UInt64 h,
            UInt64 v
        );

        /// <summary>
        /// </summary>
        [DllImport("filter.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static UInt32 filter_iterator_free(
            UInt64 h,
            UInt64 v
        );

        /// <summary>
        /// SCs the convert UT F16 string to native.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="output">The output.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe UInt32 SCConvertUTF16StringToNative(
            String input,
            UInt32 flags,
            Byte** output
        );

        /// <summary>
        /// SCs the convert UT F16 string to native2.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="output">The output.</param>
        /// <param name="outlen">The outlen.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe UInt32 SCConvertUTF16StringToNative2(
            String input,
            UInt32 flags,
            Byte* output,
            UInt32 outlen);

#if false
        /// <summary>
        /// SCs the convert native string to UT F16.
        /// </summary>
        /// <param name="inBuf">The in buf.</param>
        /// <param name="inlen">The inlen.</param>
        /// <param name="output">The output.</param>
        /// <param name="poutlen">The poutlen.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe UInt32 SCConvertNativeStringToUTF16(
            Byte* inBuf,
            UInt32 inlen,
            Char* output,
            UInt32* poutlen);

        /// <summary>
        /// SCs the compare native strings.
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <returns>Int32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe Int32 SCCompareNativeStrings(
            /* const */ Byte* str1,
            /* const */ Byte* str2
            );
#endif

        /// <summary>
        /// SCs the compare UT F16 strings.
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <param name="result">The result.</param>
        /// <returns>UInt32.</returns>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public extern static UInt32 SCCompareUTF16Strings(
            String str1,
            String str2,
            out Int32 result
            );

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static uint star_lookup(ulong record_id, /*OUT*/ ulong *precord_addr, /*OUT*/ ushort *ptable_id);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_begin_delete(ulong record_id, ulong record_addr);
        
        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_complete_delete(ulong record_id, ulong record_addr);

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_abort_delete(ulong record_id, ulong record_addr);

        public const ushort STAR_KEY_COLUMN_NAME_TOKEN = 1;
        public const ushort STAR_DEFAULT_INDEX_NAME_TOKEN = 2;

        /// <summary>
        /// </summary>
        [DllImport("sccoredb.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint star_configure(
            ulong key_column_name, ulong default_index_name, ulong installation_id, string database_name, string temp_dir_path
            );
    }

    /// <summary>
    /// Contains API from systables.dll
    /// </summary>
        [SuppressUnmanagedCodeSecurity]
    public static class systables {

        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint star_get_token(string name, out ulong token);

        public static ulong star_get_token(string name) {
            ulong token;
            var r = star_get_token(name, out token);
            if (r != 0) throw ErrorCode.ToException(r);
            return token;
        }

        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_get_label(ulong token, char** pvalue);

        public static unsafe string star_get_label(ulong token) {
            char* value;
            uint err = star_get_label(token, &value);
            if (err == 0)
                return new String(value);
            else throw ErrorCode.ToException(err);
        }

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct STAR_TABLE_INFO {
            /// <summary>
            /// </summary>
            public ulong name_token;

            /// <summary>
            /// </summary>
            public uint column_count;

            /// <summary>
            /// </summary>
            public uint inheriting_table_count;

            /// <summary>
            /// </summary>
            public ushort* inheriting_table_ids;

            /// <summary>
            /// </summary>
            public ushort table_id;

            /// <summary>
            /// </summary>
            public ushort inherited_table_id;

            /// <summary>
            /// </summary>
            public ushort flags;

            public char* name;
        };

        /// <summary>
        /// </summary>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal extern static uint star_get_table_info_by_name(
            string name, out STAR_TABLE_INFO table_info);

        /// <summary>
        /// </summary>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint star_get_table_info(
            ushort table_id,
            out STAR_TABLE_INFO table_info
            );

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        internal unsafe struct STAR_COLUMN_INFO {
            /// <summary>
            /// </summary>
            public ulong name_token;

            /// <summary>
            /// </summary>
            public ushort index;

            /// <summary>
            /// </summary>
            public ushort flags;

            /// <summary>
            /// </summary>
            public byte type;

            /// <summary>
            /// </summary>
            public char* name;
        };

        /// <summary>
        /// </summary>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint star_get_column_info(
            ushort table_id,
            ushort index,
            out STAR_COLUMN_INFO column_info
            );
        
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall)]
        internal extern static uint star_prepare_system_tables();

        /// <summary>
        /// Struct SC_COLUMN_DEFINITION
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public unsafe struct STAR_COLUMN_DEFINITION {
            /// <summary>
            /// The type
            /// </summary>
            public byte type;
            /// <summary>
            /// The is_nullable
            /// </summary>
            public byte is_nullable;

            /// <summary>
            /// The name
            /// </summary>
            public char* name;
        }

        /// <summary>
        /// </summary>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint star_create_table(
            char* name,
            ushort base_table_id,
            STAR_COLUMN_DEFINITION* column_definitions,
            uint flags
            );

        /// <summary>
        /// Sc_rename_tables the specified table_id.
        /// </summary>
        /// <param name="table_id">The table_id.</param>
        /// <param name="new_name">The new_name.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint star_rename_table(
            ushort table_id,
            string new_name
            );

        /// <summary>
        /// Sccoredb_drop_tables the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint star_drop_table(string name);

        /// <summary>
        /// </summary>
        /// <param name="table_id"></param>
        /// <param name="name"></param>
        /// <param name="pii"></param>
        /// <returns></returns>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public unsafe extern static uint star_get_index_info_by_name(
            ushort table_id,
            string name,
            sccoredb.STARI_INDEX_INFO* pii
            );

        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public extern unsafe static uint star_create_index(
            ushort table_id,
            string name,
            ushort sort_mask,
            short* column_indexes,
            uint flags
            );

        /// <summary>
        /// </summary>
        /// <param name="table_name"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [DllImport("systables.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public extern unsafe static UInt32 star_drop_index(
            string table_name,
            string name
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
        internal static extern unsafe UInt32 create_filter(
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
        internal static extern UInt32 release_filter(UInt64 h);
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
