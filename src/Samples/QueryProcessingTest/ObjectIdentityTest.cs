using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class ObjectIdentityTest {
        public static void TestObjectIdentityInSQL() {
            HelpMethods.LogEvent("Test object identities in SQL");
            ulong n = Db.SQL<ulong>("select objectno from account").First;
            Trace.Assert(n > 0);
            HelpMethods.LogEvent("Finished testing object identities in SQL");
        }
    }
}
