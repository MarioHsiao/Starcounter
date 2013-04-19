using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Binding;

namespace QueryProcessingTest {
    public static class ObjectIdentityTest {
        public static void TestObjectIdentityInSQL() {
            HelpMethods.LogEvent("Test object identities");
            Account a = Db.SQL<Account>("select a from account a").First;
            Trace.Assert(a != null);
            Trace.Assert(a.GetObjectID() != null);
            Trace.Assert(DbHelper.Base64ForUrlDecode(a.GetObjectID()) == a.GetObjectNo());
            Trace.Assert(DbHelper.GetObjectNo(a) == a.GetObjectNo());
            Trace.Assert(DbHelper.GetObjectID(a) == a.GetObjectID());
            var r = Db.SQL<string>("select objectid from account");
            var e = r.GetEnumerator();
            Trace.Assert(e.MoveNext());
            string s = e.Current;
            Trace.Assert(s != null && s != "");
            ulong n = Db.SQL<ulong>("select objectno from account").First;
            Trace.Assert(n > 0);
            HelpMethods.LogEvent("Finished testing object identities");
        }
    }
}
