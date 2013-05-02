using System;
using System.Collections;
using System.Diagnostics;
using Starcounter;
using Starcounter.Metadata;

namespace QueryProcessingTest {
    public static class SqlBugsTest {
        public static void QueryTests() {
            HelpMethods.LogEvent("Test queries with fetch and order by");
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
            HelpMethods.LogEvent("Finished test query with fetch and sorting");
            HelpMethods.LogEvent("Test queries with like");
            // Test queries with LIKE ?
            //HelpMethods.PrintQueryPlan("select u from user u where userid like ?");
            nrs = 0;
            foreach (User u in Db.SQL<User>("select u from user u where userid like ?", "kati%"))
                nrs++;
            Trace.Assert(nrs == 157);
            nrs = 0;
            foreach (User u in Db.SQL<User>("select u from user u where userid like ?", "%ti1%"))
                nrs++;
            Trace.Assert(nrs == 544);
            nrs = 0;
            foreach (User u in Db.SQL<User>("select u from user u where userid like ?", "kati1")) {
                Trace.Assert(u.UserId == "kati1");
                nrs++;
            }
            Trace.Assert(nrs == 1);
            HelpMethods.LogEvent("Finished test queries with like");
            HelpMethods.LogEvent("Start testing projections");
            TestProjectionName();
            HelpMethods.LogEvent("Finished testing projections");
            HelpMethods.LogEvent("Some tests on variables and case insensitivity");
            Account account = Db.SQL("select a from account a where client.firstname = ?", null).First;
            Trace.Assert(account == null);
            var row = Db.SlowSQL("select Client, count(accountid) from account group by Client").First;
            var row2 = Db.SlowSQL("select Client, count(accountid) from account group by client").First;
            Trace.Assert(row is IObjectView);
            Trace.Assert(row2 is IObjectView);
            Trace.Assert((row as IObjectView).GetObject(0).GetObjectNo() == (row2 as IObjectView).GetObject(0).GetObjectNo());
            Trace.Assert((row as IObjectView).GetInt64(1) == (row2 as IObjectView).GetInt64(1));
            account = Db.SQL<Account>("select a from account a where accountid = ?", null).First;
            Trace.Assert(account == null);
            HelpMethods.LogEvent("Finished some tests on variables and case insensitivity");
            HelpMethods.LogEvent("Start testing queries on comparison bug");
            TestComparison();
            HelpMethods.LogEvent("Finished testing queries on comparison bug");
            HelpMethods.LogEvent("Test enumerator related bugs");
            TestEnumerators();
            HelpMethods.LogEvent("Finished testing enumerator related bugs");
        }

        public static void TestOffsetkeyWithSorting() {
            // Offset key byte buffer.
            Byte[] offsetKey = null;

            // Starting some SQL query.
            //HelpMethods.PrintQueryPlan("SELECT a FROM Account a ORDER BY a.accountid FETCH ?");
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
            //HelpMethods.PrintQueryPlan("SELECT a FROM Account a ORDER BY a.accountid FETCH ? OFFSETKEY ?");
            using (var sqlEnum = Db.SQL("SELECT a FROM Account a ORDER BY a.accountid FETCH ? OFFSETKEY ?", 10, offsetKey).GetEnumerator()) {
                for (Int32 i = 0; i < 10; i++) {
                    Trace.Assert(sqlEnum.MoveNext());
                    Account a = (Account)sqlEnum.Current;
                    Trace.Assert(a.AccountId == i+5);
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

        public static void TestProjectionName() {
            var q = Db.SlowSQL("select userid, useridnr from User u where useridnr <?", 2);
            var e = q.GetEnumerator();
            string n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0)).DisplayName;
            Trace.Assert(n == "UserId");
            Trace.Assert(((SqlEnumerator<object>)e).PropertyBinding == null);
            q = Db.SlowSQL("select userid, FirstName||' '||LastName from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1)).DisplayName;
            Trace.Assert(n == "1");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1).DisplayName;
            Trace.Assert(n == "1");
            Trace.Assert(((SqlEnumerator<object>)e).PropertyBinding == null);
            q = Db.SlowSQL("select userid from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            Trace.Assert(((SqlEnumerator<object>)e).TypeBinding == null);
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).PropertyBinding).DisplayName;
            Trace.Assert(n == "UserId");
            n = ((SqlEnumerator<object>)e).PropertyBinding.DisplayName;
            Trace.Assert(n == "UserId");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).PropertyBinding).Name;
            Trace.Assert(n == "0");
            n = ((SqlEnumerator<object>)e).PropertyBinding.Name;
            Trace.Assert(n == "0");
            q = Db.SlowSQL("select userid, FirstName||' '||LastName as Name from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1)).DisplayName;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1).DisplayName;
            Trace.Assert(n == "Name");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1)).Name;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1).Name;
            Trace.Assert(n == "Name");
            Trace.Assert(((SqlEnumerator<object>)e).PropertyBinding == null);
            q = Db.SlowSQL("select userid, Name from User u where useridnr < ?", 2);
            e = q.GetEnumerator();
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1)).DisplayName;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1).DisplayName;
            Trace.Assert(n == "Name");
            Trace.Assert(((SqlEnumerator<object>)e).PropertyBinding == null);
            q = Db.SlowSQL("select name from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            Trace.Assert(((SqlEnumerator<object>)e).TypeBinding == null);
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).PropertyBinding).DisplayName;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).PropertyBinding.DisplayName;
            Trace.Assert(n == "Name");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).PropertyBinding).Name;
            Trace.Assert(n == "0");
            n = ((SqlEnumerator<object>)e).PropertyBinding.Name;
            Trace.Assert(n == "0");
            q = Db.SlowSQL("select name as FullName from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            Trace.Assert(((SqlEnumerator<object>)e).TypeBinding == null);
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).PropertyBinding).DisplayName;
            Trace.Assert(n == "FullName");
            n = ((SqlEnumerator<object>)e).PropertyBinding.DisplayName;
            Trace.Assert(n == "FullName");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).PropertyBinding).Name;
            Trace.Assert(n == "FullName");
            q = Db.SlowSQL("select name,firstname from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            Trace.Assert(((SqlEnumerator<object>)e).PropertyBinding == null);
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0)).DisplayName;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0).DisplayName;
            Trace.Assert(n == "Name");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0)).Name;
            Trace.Assert(n == "0");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1)).DisplayName;
            Trace.Assert(n == "FirstName");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1).DisplayName;
            Trace.Assert(n == "FirstName");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(1)).Name;
            Trace.Assert(n == "1");
            q = Db.SlowSQL("select * from User u where useridnr <?", 2);
            e = q.GetEnumerator();
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0)).DisplayName;
            Trace.Assert(n == "UserId");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0).DisplayName;
            Trace.Assert(n == "UserId");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0)).Name;
            Trace.Assert(n == "0");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(0).Name;
            Trace.Assert(n == "0");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(5)).DisplayName;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(5).DisplayName;
            Trace.Assert(n == "Name");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(5)).Name;
            Trace.Assert(n == "5");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(5).Name;
            Trace.Assert(n == "5");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(7).DisplayName;
            Trace.Assert(n == "ObjectNo");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(7).Name;
            Trace.Assert(n == "7");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(8).DisplayName;
            Trace.Assert(n == "ObjectID");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(8).Name;
            Trace.Assert(n == "8");
        }

        public static void TestComparison() {
            var e = Db.SQL<SysTable>("select s from systable s where tableid = ?",4).GetEnumerator();
            Trace.Assert(e.MoveNext());
            SysTable s = e.Current;
            Trace.Assert(s.Name == "QueryProcessingTest.Account");
            Trace.Assert(s.TableId == 4);
            e = Db.SlowSQL<SysTable>("select s from systable s where tableid = 4").GetEnumerator();
            Trace.Assert(e.MoveNext());
            s = e.Current;
            Trace.Assert(s.Name == "QueryProcessingTest.Account");
            Trace.Assert(s.TableId == 4);
            e = Db.SlowSQL<SysTable>("select s from systable s where tableid = 1.0E1").GetEnumerator();
            Trace.Assert(e.MoveNext());
        }

        public static void TestEnumerators() {
            SqlResult<dynamic> accounts = Db.SQL("select accountid, client.name, amount from account where accountid = ?", 1);
            Type t = accounts.First.GetType();
            Trace.Assert(t == typeof(Starcounter.Query.Execution.Row));
            t = accounts.First.GetType();
            Trace.Assert(t == typeof(Starcounter.Query.Execution.Row));
#if false // Does not work
            int accountid = accounts.First.AccountId;
            Trace.Assert(accountid == 1);
            decimal amount = accounts.First.Amount;
            accounts.First.Amount += 10;
            decimal newAmount = accounts.First.Amount;
            Trace.Assert(amount + 10 == newAmount);
#endif
        }
    }
}
