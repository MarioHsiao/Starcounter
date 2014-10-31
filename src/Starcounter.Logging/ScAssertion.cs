using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace Starcounter {
    public class ScAssertion {
        public static void Assert(bool checkTrue, string message) {
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True")
                if (!checkTrue)
                    throw ErrorCode.ToException(Error.SCERRTESTASSERTIONFAILURE, message);
                else
                    Trace.Assert(checkTrue, message);
        }

        public static void Assert(bool checkTrue) {
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True")
                if (!checkTrue)
                    throw ErrorCode.ToException(Error.SCERRTESTASSERTIONFAILURE);
                else
                    Trace.Assert(checkTrue);
        }
    }
}
