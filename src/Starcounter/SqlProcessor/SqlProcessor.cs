using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public class SqlProcessor {
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall, 
            CharSet = CharSet.Unicode)]
        public static unsafe extern uint scsql_process_query(ulong context, 
            string query, out byte query_type, out ulong iter);
            /*void* caller, void* executor, */
        [DllImport("scsqlprocessor.dll", CallingConvention = CallingConvention.StdCall,
            CharSet = CharSet.Unicode)]
        internal static unsafe extern uint scsql_process_modifyquery(ulong context, 
            string query, out int nrObjectsUpdated);
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern ScError* scsql_get_error();
        [DllImport("scsqlprocessor.dll")]
        public static extern uint scsql_free_memory();
        [DllImport("scsqlprocessor.dll")]
        public static extern uint scsql_dump_memory_leaks();
        [DllImport("scdbmetalayer.dll")]
        private static extern uint star_prepare_system_tables(ulong context);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_get_token(ulong context_handle, string spelling, ulong* token_id);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_get_token_name(ulong context_handle, ulong token_id,
            char** label);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_assure_token(ulong context_handle,
            string label, ulong* token_id);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_table_ref_by_layout_id(ulong context_handle, 
            ushort layout_id, out ulong table_oid, out ulong table_ref);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_create_index(ulong context,
            ulong table_oid, ulong table_ref, string name, ushort sort_mask,
            short* column_indexes, uint attribute_flags);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_create_index_ids(ulong context,
        ushort layout_id,
        string name,
        ushort sort_mask,
        short* column_indexes,
        uint attribute_flags);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_setspec_ref_by_table_ref(ulong context_handle, 
            ulong table_oid, ulong table_ref, ulong* setspec_oid, ulong* setspec_ref);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct STAR_COLUMN_DEFINITION_HIGH {
            public byte primitive_type;
            public char* type_name; // Always null if primitive type is not reference
            public byte is_nullable;
            public char* name;
        };

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_create_table_high(ulong context,
            string name,
            string base_table_name,
            SqlProcessor.STAR_COLUMN_DEFINITION_HIGH *column_definitions,
			out ushort layout_id);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct STAR_INDEXED_COLUMN {
            public char* column_name;
            public byte ascending;
        };

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_create_index_high(ulong context,
            string index_name,
            string table_name,
            STAR_INDEXED_COLUMN *columns,
			bool is_unique);
        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_drop_index_by_table_and_name(ulong context,
            string table_full_name, string index_name);

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static unsafe extern uint star_clrmetadata_clean(ulong context_handle);

        [DllImport("scdbmetalayer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static unsafe extern uint star_alter_table_add_columns(ulong context_handle,
            string full_table_name, STAR_COLUMN_DEFINITION_HIGH* added_columns,
            out ulong new_layout_handle);

        public static unsafe Exception CallSqlProcessor(String query, out byte queryType, out ulong iterator) {
            uint err = scsql_process_query(ThreadData.ContextHandle, query, out queryType, out iterator);
            if (err == 0)
                return null;
            Exception ex = GetSqlException(err, query);
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < 10000);
            // create the exception
            scsql_free_memory();
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < 10000);
            return ex;
        }

        internal static unsafe int ExecuteQuerySqlProcessor(String query) {
            int nrObjs = 0;

            uint err = scsql_process_modifyquery(ThreadData.ContextHandle, query, out nrObjs);
            if (err == 0)
                return nrObjs;
            Exception ex = GetSqlException(err, query);
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < 10000);
            // create the exception
            scsql_free_memory();
            Debug.Assert(err == (uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
            Debug.Assert(err < 10000);
            throw ex;
        }

        internal static unsafe string GetSetSpecifier(ushort layoutId) {
            ulong rawviewRecordOid;
            ulong rawviewRecordAddr;
            ulong setspecRecordOid;
            ulong setspecRecordAddr;
            uint err = Starcounter.SqlProcessor.SqlProcessor.star_table_ref_by_layout_id(
                ThreadData.ContextHandle, layoutId, out rawviewRecordOid, out rawviewRecordAddr);
            if (err != 0) throw ErrorCode.ToException(err);
            err = Starcounter.SqlProcessor.SqlProcessor.star_setspec_ref_by_table_ref(
                ThreadData.ContextHandle, rawviewRecordOid, rawviewRecordAddr,
                &setspecRecordOid, &setspecRecordAddr);
            if (err != 0) throw ErrorCode.ToException(err);
            return DbState.ReadString(setspecRecordOid, setspecRecordAddr, 2);
        }

        public static void PopulateRuntimeMetadata(ulong context) {
            uint err = star_prepare_system_tables(context);
            if (err != 0)
                throw ErrorCode.ToException(err);
        }

#if true
        private const string GlobalSetspecLayoutToken = "setspec";

        [DllImport("sccoredb.dll")]
        private static extern unsafe uint stari_context_create_layout(
            ulong handle, ulong token, ushort base_layout_handle,
            sccoredb.STARI_COLUMN_DEFINITION *column_definitions, uint attribute_flags,
            ushort *playout_handle
            );

        private const string GlobalSetspecIndexToken = "setspec";

        [DllImport("sccoredb.dll")]
        private static extern unsafe uint stari_context_get_index_infos_by_token(
            ulong handle, ulong token, uint* pic, sccoredb.STARI_INDEX_INFO *piis
            );

        [DllImport("sccoredb.dll", CharSet = CharSet.Unicode)]
        private static extern unsafe uint stari_context_create_index(
            ulong handle, ulong token, string setspec, ushort layout_handle, short* column_indexes,
            ushort sort_mask, uint attribute_flags, ulong *pindex_handle
            );

        internal static void AssureGlobalSetspecIndexExists(ulong contextHandle) { // TODO:
            ulong transactionHandle;

            transactionHandle = 0;

            try {
                unsafe {
                    uint r;

                    // Assure that base layout exists.

                    r = sccoredb.star_context_create_transaction(
                        contextHandle, 0, out transactionHandle
                        );
                    if (r != 0) {
                        transactionHandle = 0;
                        goto err;
                    }
                    r = sccoredb.star_context_set_current_transaction(
                        contextHandle, transactionHandle
                        );
                    if (r != 0) goto err;

                    ulong layoutToken = AssureToken(GlobalSetspecLayoutToken);

                    ushort layoutHandle;

                    uint layoutInfoCount = 1;
                    sccoredb.STARI_LAYOUT_INFO layoutInfo;
                    r = sccoredb.stari_context_get_layout_infos_by_token(
                        contextHandle, layoutToken, &layoutInfoCount, &layoutInfo
                        );
                    if (r != 0) goto err;
                    if (layoutInfoCount == 1) {
                        if (
                            layoutInfo.column_count == 2 && layoutInfo.inherited_layout_handle == 0
                            ) {
                            layoutHandle = layoutInfo.layout_handle;
                        }
                        else {
                            r = Error.SCERRUNEXPMETADATA;
                            goto err;
                        }
                    }
                    else if (layoutInfoCount == 0) {
                        sccoredb.STARI_COLUMN_DEFINITION columnDef;
                        columnDef.type = 0;
                        r = stari_context_create_layout(
                            contextHandle, layoutToken, 0, &columnDef, 0, &layoutHandle
                            );
                        if (r != 0) goto err;
                    }
                    else {
                        r = Error.SCERRUNEXPMETADATA;
                        goto err;
                    }

#if true
                    r = sccoredb.star_context_commit(contextHandle, 1);
                    if (r != 0) goto err;
                    transactionHandle = 0;
                    
                    r = sccoredb.star_context_create_transaction(
                        contextHandle, 0, out transactionHandle
                        );
                    if (r != 0) {
                        transactionHandle = 0;
                        goto err;
                    }
                    r = sccoredb.star_context_set_current_transaction(
                        contextHandle, transactionHandle
                        );
                    if (r != 0) goto err;
#endif

                    // Assure that global index on set specifier exists.

                    ulong indexToken = AssureToken(GlobalSetspecIndexToken);

                    uint indexInfoCount = 1;
                    sccoredb.STARI_INDEX_INFO indexInfo;
                    r = stari_context_get_index_infos_by_token(
                        contextHandle, indexToken, &indexInfoCount, &indexInfo
                        );
                    if (r != 0) goto err;

                    if (indexInfoCount == 1) {
                        // Already exists.

                        if (
                            indexInfo.attributeCount == 1 && indexInfo.attrIndexArr_0 == 1 &&
                            indexInfo.flags == 0 && indexInfo.layout_handle == layoutHandle &&
                            indexInfo.sortMask == 0
                            ) {
                            // Verified.
                        }
                        else {
                            r = Error.SCERRUNEXPMETADATA;
                            goto err;
                        }
                    }
                    else if (indexInfoCount == 0) {
                        short* columnIndexes = stackalloc short[2];
                        columnIndexes[0] = 1; // Setspec column.
                        columnIndexes[1] = -1;
                        ulong indexHandle;
                        r = stari_context_create_index(
                            contextHandle, indexToken, "", layoutHandle, columnIndexes, 0, 0,
                            &indexHandle
                            );
                        if (r != 0) goto err;
                    }
                    else {
                        r = Error.SCERRUNEXPMETADATA;
                        goto err;
                    }

                    r = sccoredb.star_context_commit(contextHandle, 1);
                    if (r != 0) goto err;
                    transactionHandle = 0;

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
#endif

        public static void CleanClrMetadata(ulong context) {
            uint err = star_clrmetadata_clean(context);
            if (err != 0)
                throw ErrorCode.ToException(err);
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

        /// <summary>
        /// Creates exception with error location and token by using Starcounter factory.
        /// </summary>
        /// <param name="errorCode">Starcounter error code</param>
        /// <param name="message">The detailed error message</param>
        /// <param name="location">Start of the error token in the query</param>
        /// <param name="token">The error token</param>
        /// <returns></returns>
        internal static Exception GetSqlException(uint scErrorCode, String query) {
            unsafe {
                ScError* scerror = scsql_get_error();
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
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ScError {
        public uint scerrorcode;
        public sbyte* scerrmessage;
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
