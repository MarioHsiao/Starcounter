using System;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace QueryProcessingTest {
    public static class TestErrorMessages {
        private static ParserAnalyzerHelloTest analyzer = null;
        public static void RunTestErrorMessages() {
            analyzer = new ParserAnalyzerHelloTest();
            RunErrorQuery("DELETE FROM Account");
        }

        internal static void RunErrorQuery(string query) {
            try {
                analyzer.ParseAndAnalyzeQuery(query);
            } catch (Starcounter.Query.RawParserAnalyzer.SQLParserAssertException) {
                Console.WriteLine("Ignored exception");
            } catch (SqlException ex) {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
