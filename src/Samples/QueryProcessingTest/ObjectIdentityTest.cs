using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Binding;

namespace QueryProcessingTest {
    public static class ObjectIdentityTest {
        public static void TestObjectIdentityInSQL() {
            HelpMethods.LogEvent("Test get object identities extension methods");
            Account a = Db.SQL<Account>("select a from account a").First;
            Trace.Assert(a != null);
            Trace.Assert(a.GetObjectID() != null);
            Trace.Assert(DbHelper.Base64ForUrlDecode(a.GetObjectID()) == a.GetObjectNo());
            HelpMethods.LogEvent("Finished testing get object identities extension methods");
#if false
            HelpMethods.LogEvent("Test object identities in SQL");
            Account a = Db.SQL<Account>("select a from account a").First;
            var ao = a as IObjectProxy;
            Trace.Assert(ao.ObjectID != null);
            Trace.Assert(ao.ObjectNo == Starcounter.Query.ObjectIdentityHelpMethods.Base64ForUrlDecode(ao.ObjectID));
            var r = Db.SQL<string>("select objectid from account");
            var e = r.GetEnumerator();
            Trace.Assert(e.MoveNext());
            string s = e.Current;
            Trace.Assert(s != null && s != "");
            Console.WriteLine(s);
            ulong n = Db.SQL<ulong>("select objectno from account").First;
            Trace.Assert(n > 0);
            HelpMethods.LogEvent("Finished testing object identities in SQL");
#endif
        }
    }
}
