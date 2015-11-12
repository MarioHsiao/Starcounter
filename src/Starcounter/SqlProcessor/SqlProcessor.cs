using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public class SqlProcessor {
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern uint scsql_process_query([MarshalAs(UnmanagedType.LPWStr)]string query);
            /*void* caller, void* executor, */
        [DllImport("scsqlprocessor.dll")]
        internal static unsafe extern uint scsql_process_modifyquery(ulong transaction_handle, [MarshalAs(UnmanagedType.LPWStr)]string query, 
            int* nrObjs);
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern ScError* scsql_get_error();
        [DllImport("scsqlprocessor.dll")]
        public static extern uint scsql_free_memory();
        [DllImport("scsqlprocessor.dll")]
        public static extern uint scsql_dump_memory_leaks();
        [DllImport("scdbmetalayer.dll")]
        private static extern uint star_prepare_system_tables(ulong context);
        [DllImport("scsqlprocessor.dll")]
        private static extern uint scsql_clean_clrclass();
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
            ushort layout_id, ulong* table_oid, ulong* table_ref);
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
			ushort* layout_id);

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

        public static unsafe Exception CallSqlProcessor(String query) {
            uint err = scsql_process_query(query);
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

            uint err = scsql_process_modifyquery(ThreadData.ContextHandle, query, &nrObjs);
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
                ThreadData.ContextHandle, layoutId, &rawviewRecordOid, &rawviewRecordAddr);
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
