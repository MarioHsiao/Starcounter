﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public class SqlProcessor {
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern uint scsql_process_query([MarshalAs(UnmanagedType.LPWStr)]string query);
            /*void* caller, void* executor, */
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern ScError* scsql_get_error();
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern uint scsql_free_memory();
        [DllImport("scsqlprocessor.dll")]
        public static unsafe extern uint scsql_dump_memory_leaks();

        public static unsafe Exception CallSqlProcessor(String query) {
            uint err = scsql_process_query(query);
            if (err == 0)
                return null;
            Exception ex = GetSqlException(err, query);
            // create the exception
            scsql_free_memory();
            return ex;
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
