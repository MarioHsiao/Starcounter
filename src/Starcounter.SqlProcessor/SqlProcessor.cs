using System;
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

        public static unsafe uint CallSqlProcessor(String query) {
            uint err = scsql_process_query(query);
            if (err == 0)
                return err;
            ScError* scerror = scsql_get_error();
            err = scerror->scerrorcode;
            // create the exception
            scsql_free_memory();
            return err;
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
