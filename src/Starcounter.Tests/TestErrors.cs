using System;
using NUnit.Framework;
using Starcounter;
using System.Diagnostics;
using Starcounter.Internal.ErrorHandling;

namespace Starcounter.Tests {
    [TestFixture]
    public static class TestErrors {
        [Test]
        public static void TestAssertions() {
            Trace.Listeners.Remove("Default");
            Trace.Listeners.Add(new TestTraceListener("QueryProcessingListener"));
            Exception ex = Assert.Throws<Exception>(() => ScAssertion.Assert(false));
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True") {
                Assert.AreEqual(ex.Data[ErrorCode.EC_TRANSPORT_KEY], Error.SCERRTESTASSERTIONFAILURE);
                Assert.AreEqual(ex.Message, ErrorCode.ToException(Error.SCERRTESTASSERTIONFAILURE).Message);
            }  else
                Assert.AreEqual(ex.Message, "Test assertion failure. ");
        }

        [Test]
        public static void TestAssertionsMessage() {
            Trace.Listeners.Remove("Default");
            Trace.Listeners.Add(new TestTraceListener("QueryProcessingListener"));
            string message = "Test assertion failure.";
            Exception ex = Assert.Throws<Exception>(() => ScAssertion.Assert(false, message));
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True") {
                Assert.AreEqual(ex.Data[ErrorCode.EC_TRANSPORT_KEY], Error.SCERRTESTASSERTIONFAILURE);
                Assert.AreEqual(ex.Message, ErrorCode.ToException(Error.SCERRTESTASSERTIONFAILURE).Message);
            } else
                Assert.AreEqual(ex.Message, "Test assertion failure. "+message);
        }
    }
}
