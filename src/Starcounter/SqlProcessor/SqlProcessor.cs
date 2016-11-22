using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    internal class SqlProcessor {
        internal const ulong MaxErrorCode = 1000000;
        internal const ulong STAR_MOM_OF_ALL_LAYOUTS_NAME_TOKEN = 11;
        internal const ulong STAR_GLOBAL_SETSPEC_INDEX_NAME_TOKEN = 12;

        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        public static unsafe extern uint scsql_process_query(ulong context,
            string query, out byte query_type, out ulong iter, ScError* error);
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        public static unsafe extern uint scsql_process_select_query(ulong context_handle,
            string query, out ulong iter, ScError* error);
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        internal static unsafe extern uint scsql_process_modifyquery(ulong context,
            string query, out int nrObjectsUpdated, ScError* error);
        [DllImport("scsqlprocessor.dll")]
        public static extern uint scsql_free_memory();
        [DllImport("scsqlprocessor.dll")]
        public static extern uint scsql_dump_memory_leaks();
        [DllImport("scdbmetalayer.dll")]
        private static extern uint star_prepare_system_tables(ulong context, out IntPtr continuation, [MarshalAs(UnmanagedType.I1)] out bool commit_required);

        [DllImport("scdbmetalayer.dll")]
        private static extern uint run_continuation(IntPtr current_continuation, out IntPtr next_continuation, [MarshalAs(UnmanagedType.I1)] out bool commit_required);

        [DllImport("scdbmetalayer.dll")]
        private static extern void free_continuation_handle(IntPtr continuation);

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_get_token(ulong context_handle, string spelling, ulong* token_id);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_get_token_name(ulong context_handle, ulong token_id,
            char** label);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_assure_token(ulong context_handle,
            string label, ulong* token_id);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern uint star_table_ref_by_layout_id(ulong context_handle,
            ushort layout_id, out ulong table_oid, out ulong table_ref);
        //internal static void GetTableRefByLayoutId(ushort layout_id,
        //    out ulong table_oid, out ulong table_ref) {
        //    MetalayerThrowIfError(star_table_ref_by_layout_id(ThreadData.ContextHandle,
        //        layout_id, out table_oid, out table_ref));
        //}

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_create_index(ulong context,
            ulong table_oid, ulong table_ref, string name, ushort sort_mask,
            short* column_indexes, uint attribute_flags);
        internal static unsafe void CreateIndex(ulong table_oid, ulong table_ref,
            string name, ushort sort_mask, short* column_indexes, uint attribute_flags) {
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_create_index(ThreadData.ContextHandle,
                    table_oid, table_ref, name, sort_mask, column_indexes,
                    attribute_flags));
            });
        }

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_create_index_ids(ulong context,
            ushort layout_id, string name, ushort sort_mask, short* column_indexes,
            uint attribute_flags);
        internal static unsafe void CreateIndexByIds(ushort layout_id, string name, 
            ushort sort_mask, short* column_indexes, uint attribute_flags) {
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_create_index_ids(ThreadData.ContextHandle,
                    layout_id, name, sort_mask, column_indexes, attribute_flags));
            });
        }

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_setspec_ref_by_table_ref(ulong context_handle, 
            ulong table_oid, ulong table_ref, ulong* setspec_oid, ulong* setspec_ref);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct STAR_COLUMN_DEFINITION_WITH_NAMES {
            public byte primitive_type;
            public char* type_name; // Always null if primitive type is not reference
            public byte is_nullable;
            public char* name;
        };

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_create_table_by_names(ulong context_handle,
            string name,
            string base_table_name,
            SqlProcessor.STAR_COLUMN_DEFINITION_WITH_NAMES *column_definitions,
			out ushort layout_id);
        internal static unsafe void CreatTableByNames(string name,
            string base_table_name, SqlProcessor.STAR_COLUMN_DEFINITION_WITH_NAMES* column_definitions,
            out ushort layout_id) {
            ushort x = 0;
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_create_table_by_names(ThreadData.ContextHandle,
                    name, base_table_name, column_definitions, out x));
            });
            layout_id = x;
        }

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern uint star_update_reference_columns_types(ulong context_handle);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct STAR_INDEXED_COLUMN {
            public char* column_name;
            public byte ascending;
        };

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_create_index_by_names(ulong context,
            string index_name,
            string table_name,
            STAR_INDEXED_COLUMN *columns,
			bool is_unique);

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern uint star_drop_index_by_table_and_name(ulong context,
            string table_full_name, string index_name);
        internal static void DropIndexByTableAndIndexName(string table_full_name, 
            string index_name) {
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_drop_index_by_table_and_name(ThreadData.ContextHandle,
                    table_full_name, index_name));
            });
        }

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_drop_table_cascade(ulong context,
            string table_full_name);

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_clrmetadata_clean(ulong context_handle);

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_alter_table_add_columns(ulong context_handle,
            string full_table_name, STAR_COLUMN_DEFINITION_WITH_NAMES* added_columns,
            out ulong new_layout_handle);
        internal static unsafe void AlterTableAddColumns(string full_table_name, 
            STAR_COLUMN_DEFINITION_WITH_NAMES* added_columns, out ulong new_layout_handle) {

            ulong x=0;
            NewTransaction(() =>
                {
                    MetalayerThrowIfError(star_alter_table_add_columns(ThreadData.ContextHandle,
                        full_table_name, added_columns, out x));
                });
            new_layout_handle = x;
        }

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern char* star_metalayer_errbuf(ulong context_handle);

        public const byte SQL_QUERY_TYPE_SELECT = 0;
        public const byte SQL_QUERY_TYPE_NONSELECT = 1;

        public static unsafe Exception CallSqlProcessor(String query, out byte queryType, out ulong iterator) {
            ScError scerror;
            uint err = scsql_process_query(ThreadData.ContextHandle, query, out queryType, out iterator, &scerror);
            if (err == 0)
                return null;
            Exception ex = GetSqlException(err, query, &scerror);
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < 10000);
            // create the exception
            scsql_free_memory();
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < 10000);
            return ex;
        }

        internal static unsafe Exception CallSelectPrepare(String query, out ulong iterator) {
            ScError scerror;
            uint err = scsql_process_select_query(ThreadData.ContextHandle, query, out iterator, &scerror);
            if (err == 0)
                return null;
            Exception ex = GetSqlException(err, query, &scerror);
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < MaxErrorCode);
            // create the exception
            scsql_free_memory();
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < MaxErrorCode);
            return ex;
        }

        internal static unsafe int ExecuteQuerySqlProcessor(String query) {
            int nrObjs = 0;

            ScError scerror;
            uint err = scsql_process_modifyquery(ThreadData.ContextHandle, query, out nrObjs, &scerror);
            if (err == 0)
                return nrObjs;
            Exception ex = GetSqlException(err, query, &scerror);
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < MaxErrorCode);
            // create the exception
            scsql_free_memory();
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < MaxErrorCode);
            throw ex;
        }

        internal static unsafe string GetSetSpecifier(ushort layoutId) {
            ulong rawviewRecordOid;
            ulong rawviewRecordAddr;
            ulong setspecRecordOid;
            ulong setspecRecordAddr;
            MetalayerThrowIfError(Starcounter.SqlProcessor.SqlProcessor.star_table_ref_by_layout_id(
                ThreadData.ContextHandle, layoutId, out rawviewRecordOid, out rawviewRecordAddr));
            MetalayerThrowIfError(Starcounter.SqlProcessor.SqlProcessor.star_setspec_ref_by_table_ref(
                ThreadData.ContextHandle, rawviewRecordOid, rawviewRecordAddr,
                &setspecRecordOid, &setspecRecordAddr));
            return DbState.ReadString(setspecRecordOid, setspecRecordAddr, 3);
        }

        delegate uint continuation(out IntPtr next_continuation, out bool commit_required);

        static uint run_continuations(continuation cont)
        {
            uint r = 0;
            IntPtr continuation = IntPtr.Zero;

            using (var tran = new Transaction(false,false))
            {
                tran.Scope(() =>
                {
                    bool commit_required;
                    r = cont(out continuation, out commit_required);

                    if (commit_required)
                        tran.Commit();
                });
            }

            if ( continuation != IntPtr.Zero )
            {
                return run_continuations(
                    (out IntPtr next_continuation, out bool next_commit_required) =>
                    {
                        try
                        {
                            return run_continuation(continuation, out next_continuation, out next_commit_required);
                        }
                        finally
                        {
                            free_continuation_handle(continuation);
                        }
                    });
            }
            return r;

        }

        public static void PopulateRuntimeMetadata() {
            ulong context = ThreadData.ContextHandle;
            MetalayerThrowIfError(
                run_continuations(
                    (out IntPtr next_continuation, out bool next_commit_required) =>
                    {
                        return star_prepare_system_tables(context, out next_continuation, out next_commit_required);
                    }
            ));
            LoadGlobalSetspecIndexHandle(context);
        }

        private static ulong globalSetspecIndexHandle_ = 0;

        internal static ulong GetGlobalSetspecIndexHandle(ulong contextHandle) {
            return globalSetspecIndexHandle_;
        }

        [DllImport("sccoredb.dll")]
        private static extern unsafe uint stari_context_get_index_infos_by_token(
            ulong handle, ulong token, uint* pic, sccoredb.STARI_INDEX_INFO *piis
            );

        private static void LoadGlobalSetspecIndexHandle(ulong contextHandle) {
            ulong transactionHandle;

            transactionHandle = 0;

            try {
                unsafe {
                    uint r;

                    r = sccoredb.star_create_transaction(
                        0, out transactionHandle
                        );
                    if (r != 0) {
                        transactionHandle = 0;
                        goto err;
                    }
                    r = sccoredb.star_context_set_transaction(
                        contextHandle, transactionHandle
                        );
                    if (r != 0) goto err;

                    ulong indexToken = STAR_GLOBAL_SETSPEC_INDEX_NAME_TOKEN;
                    uint indexInfoCount = 1;
                    sccoredb.STARI_INDEX_INFO indexInfo;
                    r = stari_context_get_index_infos_by_token(
                        contextHandle, indexToken, &indexInfoCount, &indexInfo
                        );
                    if (r != 0) goto err;

                    if (indexInfoCount == 1) {
                        globalSetspecIndexHandle_ = indexInfo.handle;
                    }
                    else {
                        r = Error.SCERRUNEXPMETADATA;
                        goto err;
                    }


                    return;

                err:
                    throw ErrorCode.ToException(r);
                }
            }
            finally {
                if (transactionHandle != 0) {
                    uint r = sccoredb.star_transaction_free(
                        transactionHandle, ThreadData.ObjectVerify
                        );
                    if (r != 0) ErrorCode.ToException(r); // Fatal.
                }
            }
        }

        public static void CleanClrMetadata(ulong context) {
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_clrmetadata_clean(context));
            });
        }

        public static unsafe ulong GetTokenFromName(string Name) {
            ulong token;
            uint err = star_get_token(ThreadData.ContextHandle, Name, &token);
            if (err != 0)
                return 0;
            Debug.Assert(token != 0);
            return token;
        }

        public static unsafe string GetNameFromToken(ulong Token) {
            char* name;
            uint err = star_get_token_name(ThreadData.ContextHandle, Token, &name);
            if (err != 0)
                throw ErrorCode.ToException(err);
            return new String(name);
        }

        public static unsafe ulong AssureToken(string Name) {
            ulong token;
            uint err = star_assure_token(ThreadData.ContextHandle, Name, &token);
            if (err != 0)
                throw ErrorCode.ToException(err);
            return token;
        }

        internal static unsafe void MetalayerThrowIfError(uint err) {
            if (err != 0) {
                char* errorMessage = star_metalayer_errbuf(ThreadData.ContextHandle);
                if (errorMessage != null)
                    throw ErrorCode.ToException(err, new String(errorMessage));
                else
                    throw ErrorCode.ToException(err);
            }
        }

        public static void DropTableCascade(string tableFullName) {
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_drop_table_cascade(ThreadData.ContextHandle, tableFullName));
            });
        }

        internal static unsafe void CreateIndex(string index_name, string table_name, 
            STAR_INDEXED_COLUMN* columns, bool is_unique) {
            NewTransaction(() =>
            {
                MetalayerThrowIfError(star_create_index_by_names(ThreadData.ContextHandle,
                    index_name, table_name, columns, is_unique));
            });
        }

        /// <summary>
        /// Creates exception with error location and token by using Starcounter factory.
        /// </summary>
        /// <param name="errorCode">Starcounter error code</param>
        /// <param name="message">The detailed error message</param>
        /// <param name="location">Start of the error token in the query</param>
        /// <param name="token">The error token</param>
        /// <returns></returns>
        internal unsafe static Exception GetSqlException(uint scErrorCode, String query, ScError* scerror) {
            unsafe {
                if (scerror == null)
                    throw ErrorCode.ToException(Error.SCERRUNEXPERRUNAVAILABLE);
                String message = new String(scerror->scerrmessage);
                uint errorCode = (uint)scerror->scerrorcode;
                Debug.Assert(scErrorCode == errorCode);
                int position = scerror->scerrposition;
                String token = scerror->token;
                if (message == "syntax error" && token != null)
                    message = "Unexpected token.";
                if (message == "syntax error" && token == null)
                    message = "Unexpected end of query.";
                message += " The error near or at position " + position;
                if (token != null) {
                    message += ", near or at token: " + token;
                    if (scerror->isKeyword > 0)
                        message += ". Note that the token is a keyword.";
                } else
                    message += ".";
                if (query.Length > 1000)
                    message += "\nIn query: " + query.Substring(0, 500);
                else
                    message += "\nIn query: " + query;
                return ErrorCode.ToException(errorCode, message, (m, e) => new SqlException(errorCode, m, message, position, token, query));
            }
        }
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        internal static extern uint scsql_advance_iter(ulong iter);
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        internal static extern uint scsql_delete_iter(ulong iter);
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        internal static extern uint scsql_deref_db_iter(ulong iter, out ulong rec_id, out ulong rec_ref);

        private static void NewTransaction(Action a)
        {
            using (var tran = new Transaction(false,false))
            {
                tran.Scope(() =>
                {
                    a();
                    tran.Commit();
                });
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ScError {
        public uint scerrorcode;
        public char* scerrmessage;
        public int scerrposition;
        public byte isKeyword;
        private IntPtr _token;

        public String token {
            get {
                return Marshal.PtrToStringAuto(_token);
            }
        }
    }
}
