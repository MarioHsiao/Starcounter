using System;
using System.Diagnostics;

namespace Starcounter {
    public class ScAssertion {
        public static void Assert(bool checkTrue, string message) {
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True")
                throw new Exception(message);
            else
                Trace.Assert(checkTrue, message);
        }
    }
}
