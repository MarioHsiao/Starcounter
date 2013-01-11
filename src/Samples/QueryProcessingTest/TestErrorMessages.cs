using System;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace QueryProcessingTest {
    public static class TestErrorMessages {
        public static void RunTestErrorMessages() {
            ParserAnalyzer analyzer = new ParserAnalyzer();
            analyzer.ParseAndAnalyzeQuery("DELETE FROM Account");
        }
    }
}
