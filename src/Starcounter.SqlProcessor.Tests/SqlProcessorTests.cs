using System;
using NUnit.Framework;
using Starcounter.SqlProcessor;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public class SqlProcessorTests {
        [Test]
        public static void HelloProcessor() {
            Assert.AreEqual(0, SqlProcessor.CallSqlProcessor(""));
        }
    }
}
