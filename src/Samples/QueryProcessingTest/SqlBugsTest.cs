using System;
using System.Collections;
using System.Diagnostics;
using Starcounter;
using Starcounter.Metadata;

namespace QueryProcessingTest {
    public static class SqlBugsTest {
        public static void QueryTests() {
            TestFetchOrderBy();
            TestLike();
            TestProjectionName();
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
            TestComparison();
            TestEnumerators();
            QueryResultMismatch();
            TestIndexQueryOptimization();
        }

        public static void TestFetchOrderBy() {
            HelpMethods.LogEvent("Test queries with fetch and order by");
            // Test query with FETCH and ORDER BY
            decimal amounts = 0;
            int nrs = 0;
            var accounts = Db.SQL("select a from account a order by a.\"when\" desc fetch ?", 10);
            Account acc = accounts.First;
            foreach (Account a in Db.SQL("select a from account a order by a.\"when\" desc fetch ?", 10)) {
                amounts += a.Amount;
                nrs++;
            }
            Trace.Assert(nrs == 10);
            nrs = 0;
            foreach (Account a in Db.SQL("select a from account a order by a.\"when\" desc fetch ?", 20))
                nrs++;
            Trace.Assert(nrs == 20);
            nrs = 0;
            foreach (Account a in Db.SQL("select a from account a order by a.accountid desc fetch ?", 10))
                nrs++;
            Trace.Assert(nrs == 10);
            nrs = 0;
            foreach (IObjectView obj in Db.SlowSQL("select client, count(\"when\") from account group by client order by client fetch ?", 10))
                nrs++;
            Trace.Assert(nrs == 10);
            nrs = 0;
            foreach (IObjectView obj in Db.SlowSQL("select client, count(\"when\") from account group by client order by client fetch ?", 20))
                nrs++;
            Trace.Assert(nrs == 20);
            TestOffsetkeyWithSorting();
            HelpMethods.LogEvent("Finished test query with fetch and sorting");
        }

        public static void TestLike() {
            HelpMethods.LogEvent("Test queries with like");
            // Test queries with LIKE ?
            //HelpMethods.PrintQueryPlan("select u from user u where userid like ?");
            int nrs = 0;
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
            HelpMethods.LogEvent("Start testing projections");
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
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(7)).DisplayName;
            Trace.Assert(n == "Name");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(7).DisplayName;
            Trace.Assert(n == "Name");
            n = ((Starcounter.Query.Execution.PropertyMapping)((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(7)).Name;
            Trace.Assert(n == "7");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(7).Name;
            Trace.Assert(n == "7");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(9).DisplayName;
            Trace.Assert(n == "ObjectNo");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(9).Name;
            Trace.Assert(n == "9");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(10).DisplayName;
            Trace.Assert(n == "ObjectID");
            n = ((SqlEnumerator<object>)e).TypeBinding.GetPropertyBinding(10).Name;
            Trace.Assert(n == "10");
            HelpMethods.LogEvent("Finished testing projections");
        }

        public static void TestComparison() {
            HelpMethods.LogEvent("Start testing queries on comparison bug");
            var e = Db.SQL<SysTable>("select s from systable s where tableid = ?", 4).GetEnumerator();
            Trace.Assert(e.MoveNext());
            SysTable s = e.Current;
            Trace.Assert(s.Name == "QueryProcessingTest.Account");
            Trace.Assert(s.TableId == 4);
            e.Dispose();
            e = Db.SlowSQL<SysTable>("select s from systable s where tableid = 4").GetEnumerator();
            Trace.Assert(e.MoveNext());
            s = e.Current;
            Trace.Assert(s.Name == "QueryProcessingTest.Account");
            Trace.Assert(s.TableId == 4);
            e.Dispose();
            e = Db.SlowSQL<SysTable>("select s from systable s where tableid = 10").GetEnumerator();
            Trace.Assert(e.MoveNext());
            e.Dispose();
            e = Db.SlowSQL<SysTable>("select s from systable s where tableid = 1.0E1").GetEnumerator();
            Trace.Assert(e.MoveNext());
            e.Dispose();
            HelpMethods.LogEvent("Finished testing queries on comparison bug");
        }

        public static void TestEnumerators() {
            HelpMethods.LogEvent("Test enumerator related bugs");
            SqlResult<dynamic> accounts = Db.SQL("select accountid as accountid, client.name as name, amount as amount from account where accountid = ?", 1);
            Type t = accounts.First.GetType();
            Trace.Assert(t == typeof(Starcounter.Query.Execution.Row));
            t = accounts.First.GetType();
            Trace.Assert(t == typeof(Starcounter.Query.Execution.Row));
            long accountid = accounts.First.accountid;
            accountid = accounts.First.AccountId;
            Trace.Assert(accountid == 1);
            decimal amount = accounts.First.amount;
            amount = accounts.First.Amount;

            //Console.WriteLine(Db.SQL("select u from user u where nickname = ?", "Nk1").GetEnumerator().ToString());
#if false // Does not work
            accounts.First.Amount += 10;
            decimal newAmount = accounts.First.Amount;
            Trace.Assert(amount + 10 == newAmount);
#endif
            HelpMethods.LogEvent("Finished testing enumerator related bugs");
        }

        public static void QueryResultMismatch() {
            HelpMethods.LogEvent("Start testing query result mismatch errors.");
            var accs = Db.SQL<Account>("select * from account a");
            bool wasException = false;
            try {
                var a = accs.First;
            } catch (Exception exc) {
                if (exc.Data[ErrorCode.EC_TRANSPORT_KEY] == null || (uint)exc.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRQUERYRESULTTYPEMISMATCH)
                    throw exc;
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            var users = Db.SQL<User>("select * from user u");
            try {
                using (var res = users.GetEnumerator()) {
                    Trace.Assert(res.MoveNext());
                    var row = res.Current;
                }
            } catch (Exception exc) {
                if (exc.Data[ErrorCode.EC_TRANSPORT_KEY] == null || (uint)exc.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRQUERYRESULTTYPEMISMATCH)
                    throw exc;
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            var users2 = Db.SQL<Account>("select a.client from account a");
            try {
                using (var res = users2.GetEnumerator()) {
                    Trace.Assert(res.MoveNext());
                    var row = res.Current;
                }
            } catch (Exception exc) {
                if (exc.Data[ErrorCode.EC_TRANSPORT_KEY] == null || (uint)exc.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRQUERYRESULTTYPEMISMATCH)
                    throw exc;
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            var astrs = Db.SQL<Account>("select name from user");
            try {
                using (var res = astrs.GetEnumerator()) {
                    Trace.Assert(res.MoveNext());
                    var row = res.Current;
                }
            } catch (Exception exc) {
                if (exc.Data[ErrorCode.EC_TRANSPORT_KEY] == null || (uint)exc.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRQUERYRESULTTYPEMISMATCH)
                    throw exc;
                wasException = true;
            }
            Trace.Assert(wasException);
            wasException = false;
            var decs = Db.SQL<Decimal>("select name from user");
            try {
                var decsres = astrs.First;
            } catch (Exception exc) {
                if (exc.Data[ErrorCode.EC_TRANSPORT_KEY] == null || (uint)exc.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRQUERYRESULTTYPEMISMATCH)
                    throw exc;
                wasException = true;
            }
            Trace.Assert(wasException);
            // No exceptions
            var res1 = Db.SQL("select * from account").First;
            using (var query2 = Db.SQL<Starcounter.Query.Execution.Row>("select * from account").GetEnumerator()) {
                Trace.Assert(query2.MoveNext());
                var res2 = query2.Current;
            }
            var res3 = Db.SQL<Account>("select a from account a").First;
            var res4 = Db.SQL<Decimal>("select amount from account").First;
            var res5 = Db.SQL<Decimal>("select accountid from account").First;

            HelpMethods.LogEvent("Finished testing query result mismatch errors.");
        }

        public static void TestIndexQueryOptimization() {
            HelpMethods.LogEvent("Test query optimization with indexes");
            // Issue #563
            Trace.Assert(((SqlEnumerator<User>)Db.SQL<User>("select u from user u where nickname = ?", "Nk1").GetEnumerator()).subEnumerator.GetType() == typeof(Starcounter.Query.Execution.IndexScan));

            // Issue #645
            var query = Db.SQL<User>("select u from user u where nickname = ? and lastname = ?", "Nk2", "Ln2");
            var qEnum = query.GetEnumerator();
            String enumStr = qEnum.ToString();
            Trace.Assert(!enumStr.Contains("ComparisonString"));
            HelpMethods.LogEvent("Finished testing query optimization with indexes");
        }
    }
}
