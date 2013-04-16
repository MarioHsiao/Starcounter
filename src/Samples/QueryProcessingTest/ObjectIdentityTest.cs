using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class ObjectIdentityTest {
        public static void TestObjectIdentityInSQL() {
            HelpMethods.LogEvent("Test object identities in SQL");
            var r = Db.SQL<string>("select objectid from account");
            var e = r.GetEnumerator();
            Trace.Assert(e.MoveNext());
            string s = e.Current;
            Trace.Assert(s != null && s != "");
            Console.WriteLine(s);
            ulong n = Db.SQL<ulong>("select objectno from account").First;
            Trace.Assert(n > 0);
            HelpMethods.LogEvent("Finished testing object identities in SQL");
        }
    }
}
