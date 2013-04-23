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
            // Reference lookup tests
            var accounts = Db.SQL<Account>("select a from user u, account a where u = a.client").GetEnumerator();
            accounts.ToString();
            accounts = Db.SQL<Account>("select a from user u, account a where a.client = u").GetEnumerator();
            accounts.ToString();
            accounts = Db.SQL<Account>("select a from account a, user u where a.client = u").GetEnumerator();
            accounts.ToString();
            accounts = Db.SQL<Account>("select a from account a, user u where u = a.client").GetEnumerator();
            accounts.ToString();
            // Different cases with equality conditions for optimization
            ulong accountNo = Db.SQL<ulong>("select objectno from account a where accountid = ?", 1).First;
            a = Db.SQL<Account>("select a from account a where objectno = ?", accountNo).First;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            a = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a1.accountid = ?", 1).First;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            a = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a2.accountid = ?", 1).First;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            a = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a1.objectNo = ?", accountNo).First;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            a = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a2.objectNo = ?", accountNo).First;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            a = Db.SlowSQL<Account>("select a from account a where objectno = "+accountNo).First;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            HelpMethods.LogEvent("Finished testing object identities");
        }
    }
}
