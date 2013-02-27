using System;
using System.Collections;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class SqlBugsTest {
        public static void QueryTests() {
            // Test query with FETCH and ORDER BY
            decimal amounts = 0;
            int nrs = 0;
            var accounts = Db.SQL("select a from account a order by a.updated desc fetch ?", 10);
            Account acc = accounts.First;
            foreach (Account a in Db.SQL("select a from account a order by a.updated desc fetch ?", 10)) {
                amounts += a.Amount;
                nrs++;
            }
            Trace.Assert(nrs == 10);
            nrs = 0;
            foreach (Account a in Db.SQL("select a from account a order by a.updated desc fetch ?", 20))
                nrs++;
            Trace.Assert(nrs == 20);
            nrs = 0;
            foreach (Account a in Db.SQL("select a from account a order by a.accountid desc fetch ?", 10))
                nrs++;
            Trace.Assert(nrs == 10);
            nrs = 0;
            foreach (IObjectView obj in Db.SlowSQL("select client, count(updated) from account group by client order by client fetch ?", 10))
                nrs++;
            Trace.Assert(nrs == 10);
            nrs = 0;
            foreach (IObjectView obj in Db.SlowSQL("select client, count(updated) from account group by client order by client fetch ?", 20))
                nrs++;
            Trace.Assert(nrs == 20);
            TestOffsetkeyWithSorting();
            // See simple aggregate plan. Try to get aggregate node on top
            HelpMethods.PrintSlowQueryPlan("select sum(amount) from account");
            // Test queries with LIKE ?
            HelpMethods.PrintQueryPlan("select u from user u where userid like ?");
            nrs = 0;
            foreach (User u in Db.SQL<User>("select u from user u where userid like ?", "kati%"))
                nrs++;
            Console.WriteLine(nrs);
            nrs = 0;
            foreach (User u in Db.SQL<User>("select u from user u where userid like ?", "%ti1%"))
                nrs++;
            Console.WriteLine(nrs);
            foreach (User u in Db.SQL<User>("select u from user u where userid like ?", "kati1"))
                Console.WriteLine(u.UserId);
        }

        public static void TestOffsetkeyWithSorting() {
            // Offset key byte buffer.
            Byte[] offsetKey = null;

            // Starting some SQL query.
            HelpMethods.PrintQueryPlan("SELECT a FROM Account a ORDER BY a.accountid FETCH ?");
            using (var sqlEnum = Db.SQL("SELECT a FROM Account a ORDER BY a.accountid FETCH ?", 5).GetEnumerator()) {
                for (Int32 i = 0; i < 5; i++) {
                    sqlEnum.MoveNext();
                    Trace.Assert(((Account)sqlEnum.Current).AccountId == i);
                }

                // Fetching the offset key.
                offsetKey = sqlEnum.GetOffsetKey();
                Trace.Assert(offsetKey != null);
                Trace.Assert(!sqlEnum.MoveNext());
            }

            // Now recreating the enumerator state using the offset key.
            HelpMethods.PrintQueryPlan("SELECT a FROM Account a ORDER BY a.accountid FETCH ? OFFSETKEY ?");
            using (var sqlEnum = Db.SQL("SELECT a FROM Account a ORDER BY a.accountid FETCH ? OFFSETKEY ?", 10, offsetKey).GetEnumerator()) {
                for (Int32 i = 0; i < 10; i++) {
                    sqlEnum.MoveNext();
                    Trace.Assert(((Account)sqlEnum.Current).AccountId == i+4);
                }
                Trace.Assert(!sqlEnum.MoveNext());
            }

#if false   // does not work
            // Another query with sorting on non-index property
            // Starting some SQL query.
            using (var sqlEnum = Db.SQL("SELECT a FROM Account a ORDER BY a.client FETCH ?", 5).GetEnumerator()) {
                for (Int32 i = 0; i < 5; i++) {
                    sqlEnum.MoveNext();
                    Trace.Assert(((Account)sqlEnum.Current).AccountId == i);
                }

                // Fetching the offset key.
                offsetKey = sqlEnum.GetOffsetKey();
                Trace.Assert(offsetKey != null);
                Trace.Assert(!sqlEnum.MoveNext());
            }

            // Now recreating the enumerator state using the offset key.
            using (var sqlEnum = Db.SQL("SELECT a FROM Account a ORDER BY a.client FETCH ? OFFSETKEY ?", 10, offsetKey).GetEnumerator()) {
                for (Int32 i = 0; i < 10; i++) {
                    sqlEnum.MoveNext();
                    Trace.Assert(((Account)sqlEnum.Current).AccountId == i + 4);
                }
                Trace.Assert(!sqlEnum.MoveNext());
            }
#endif
        }

    }
}
