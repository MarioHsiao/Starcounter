using System;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace QueryProcessingTest {
    public static class TestErrorMessages {
        public static void RunTestErrorMessages() {
            ParserAnalyzerHelloTest analyzer = new ParserAnalyzerHelloTest();
            analyzer.ParseAndAnalyzeQuery("DELETE FROM Account");
        }
    }
}
