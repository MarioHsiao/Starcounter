using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class ObjectIdentityTest {
        public static void TestObjectIdentityInSQL() {
            HelpMethods.LogEvent("Test get object identities extension methods");
            Account a = Db.SQL<Account>("select a from account a").First;
            Trace.Assert(a != null);
            Trace.Assert(a.GetObjectID() != null);
            Trace.Assert(DbHelper.Base64ForUrlDecode(a.GetObjectID()) == a.GetObjectNo());
            Trace.Assert(DbHelper.Base64ForUrlDecode(DbHelper.GetObjectID(a)) == DbHelper.GetObjectNo(a));
            HelpMethods.LogEvent("Finished testing get object identities extension methods");
#if false
            HelpMethods.LogEvent("Test object identities in SQL");
            ulong n = Db.SQL<ulong>("select objectno from account").First;
            Trace.Assert(n > 0);
            HelpMethods.LogEvent("Finished testing object identities in SQL");
#endif
        }
    }
}
