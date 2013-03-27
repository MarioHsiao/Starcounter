using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace QueryProcessingTest {
    public static class TestErrorMessages {
        private static MapParserTree analyzer = null;
        private static ParserTreeWalker walker = null;
        private static int ignored = 0;
        private static int sqlexceptions = 0;
        public static void RunTestErrorMessages() {
            HelpMethods.LogEvent("Test error messages.");
            ignored = 0;
            sqlexceptions = 0;
            analyzer = new MapParserTree();
            walker = new ParserTreeWalker();
            RunErrorQuery("DELETE FROM Account");
            RunErrorQuery("select from fro fro");
            Trace.Assert(ignored == 1);
            Trace.Assert(sqlexceptions == 1);
            HelpMethods.LogEvent("Finished test of error messages");
        }

        internal static void RunErrorQuery(string query) {
            try {
                walker.ParseQueryAndWalkTree(query, analyzer);
            } catch (Exception ex) {
                if (ex is SqlException) {
                    ex.ToString();
                    sqlexceptions++;
                    //HelpMethods.LogEvent(ex.ToString());
                    return;
                }
                if (!((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRSQLINTERNALERROR))
                    throw;
                ignored++;
            }
        }
    }
}
