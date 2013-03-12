using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace QueryProcessingTest {
    public static class TestErrorMessages {
        private static ParserAnalyzerHelloTest analyzer = null;
        private static int ignored = 0;
        private static int sqlexceptions = 0;
        public static void RunTestErrorMessages() {
            HelpMethods.LogEvent("Test error messages.");
            ignored = 0;
            sqlexceptions = 0;
            analyzer = new ParserAnalyzerHelloTest();
            RunErrorQuery("DELETE FROM Account");
            Trace.Assert(ignored == 1);
            Trace.Assert(sqlexceptions == 0);
            HelpMethods.LogEvent("Finished test of error messages");
        }

        internal static void RunErrorQuery(string query) {
            try {
                analyzer.ParseAndAnalyzeQuery(query);
            } catch (Starcounter.Query.RawParserAnalyzer.SQLParserAssertException) {
                ignored++;
            } catch (SqlException ex) {
                ex.ToString();
                sqlexceptions++;
                //Console.WriteLine(ex.ToString());
            }
        }
    }
}
