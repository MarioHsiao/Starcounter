using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Internal.ErrorHandling {
    public class TestTraceListener : TraceListener {
        List<string> messages = new List<string>();
        public TestTraceListener() : base() {
        }

        public TestTraceListener(String name)
            : base(name) {
        }

        public override void Fail(string message) {
            throw new Exception("Test assertion failure. " + message);
        }

        public override void Fail(string message, string detailMessage) {
            Fail(message + " " + detailMessage);
        }

        public override void Write(string message) {
            messages.Add(message);
        }

        public override void WriteLine(string message) {
            messages.Add(message);
        }
    }
}
