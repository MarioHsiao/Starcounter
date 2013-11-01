using System;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public class SqlProcessor {
        [DllImport("scsqlprocessor.dll")]
        internal static unsafe extern uint scsql_process_query([MarshalAs(UnmanagedType.LPWStr)]string query, 
            /*void* caller, void* executor, */ScError* scerror);

        public static unsafe uint CallSqlProcessor(String query) {
            ScError scerror = new ScError();
            ScError* scerrorptr = &scerror;
            return scsql_process_query(query, scerrorptr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ScError {
        internal int scerrorcode;
        internal sbyte* scerrmessage;
        internal int scerrposition;
        internal byte isKeyword;
        private IntPtr _token;

        internal String token {
            get {
                return Marshal.PtrToStringAuto(_token);
            }
        }
    }
}
