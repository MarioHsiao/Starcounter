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
            Trace.Assert(DbHelper.Base64DecodeObjectID(a.GetObjectID()) == a.GetObjectNo());
            Trace.Assert(DbHelper.GetObjectNo(a) == a.GetObjectNo());
            Trace.Assert(DbHelper.GetObjectID(a) == a.GetObjectID());
            var r = Db.SQL<string>("select objectid from account");
            var e = r.GetEnumerator();
            Trace.Assert(e.MoveNext());
            string s = e.Current;
            e.Dispose();
            Trace.Assert(s != null && s != "");
            ulong n = Db.SQL<ulong>("select objectno from account").First;
            Trace.Assert(n > 0);
            // Different cases with equality conditions for optimization on ObjectNo
            ulong accountNo = Db.SQL<ulong>("select objectno from account a where accountid = ?", 1).First;
            var accounts = Db.SQL<Account>("select a from account a where objectno = ?", accountNo).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a1.accountid = ?", 1).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a2.accountid = ?", 1).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a1.objectNo = ?", accountNo).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectno = a2.objectno and a2.objectNo = ?", accountNo).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            accounts = Db.SlowSQL<Account>("select a from account a where objectno = " + accountNo).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectNo() == accountNo);
            // Different cases with equality conditions for optimization on ObjectID
            string accountID = Db.SQL<string>("select objectid from account a where accountid = ?", 1).First;
            accounts = Db.SQL<Account>("select a from account a where objectid = ?", accountID).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectID() == accountID);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectID = a2.objectid and a1.accountid = ?", 1).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectID() == accountID);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectid = a2.objectid and a2.accountid = ?", 1).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectID() == accountID);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectid = a2.objectId and a1.objectID = ?", accountID).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectID() == accountID);
            accounts = Db.SQL<Account>("select a1 from account a1, account a2 where a1.objectid = a2.objectid and a2.objectid = ?", accountID).GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectID() == accountID);
            accounts = Db.SlowSQL<Account>("select a from account a where objectid = '" + accountID + "'").GetEnumerator();
            //Console.WriteLine(accounts.ToString());
            Trace.Assert(accounts.MoveNext());
            a = accounts.Current;
            accounts.Dispose();
            Trace.Assert(a.AccountId == 1);
            Trace.Assert(a.GetObjectID() == accountID);
            a = Db.SQL<Account>("select a from account a where objectno = ?", 1230932).First;
            Trace.Assert(a == null);
            string idval = "23";
            try {
                a = Db.SQL<Account>("select a from account a where objectid = ?", idval).First;
            } catch (System.ArgumentException ex) {
                Trace.Assert((uint)ex.Data[ErrorCode.EC_TRANSPORT_KEY] == Error.SCERRBADARGUMENTS);
            }
            a = Db.SQL<Account>("select a from account a where objectid = ?", "").First;
            Trace.Assert(a == null);
            a = Db.SQL<Account>("select a from account a where objectid = ?", "/asdfasdfa/asdfasdf").First;
            Trace.Assert(a == null);
            a = Db.SQL<Account>("select a from account a where objectid = ?", "/a+").First;
            Trace.Assert(a == null);
            HelpMethods.LogEvent("Finished testing object identities");
        }
    }
}
