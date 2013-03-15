using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class SelectClauseExpressionsTests {
        public static void TestSelectClauseExpressions() {
            HelpMethods.LogEvent("Test expressions in select clause");
            int nrs = 0;
            // Arithmetic in select without variables
            Db.Transaction(delegate {
                foreach (Decimal a in Db.SQL("select amount / (accountid+?) from account where accountid < ?", 1, 3)) {
                    Trace.Assert(a >= nrs);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 3);
#if false   // not supported
            nrs = 0;
            Db.Transaction(delegate {
                foreach (Boolean a in Db.SQL<Boolean>("select amount > accountid from account where accountid < ?", 3)) {
                    Trace.Assert(a == (nrs > 0));
                    nrs++;
                }
            });
            Trace.Assert(nrs == 3);
            nrs = 0;
            // Arithmetic in select clause
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView a in Db.SQL("select amount / ?, amount > ? from account where accountid < ?",
                    100, 95, 3)) {
                        Trace.Assert(a.GetDecimal(0) == nrs);
                        Trace.Assert(a.GetBoolean(1) == (nrs > 0));
                        nrs++;
                }
            });
            Trace.Assert(nrs == 3);
            // String operator in select clause
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SQL("select FirstName+?+LastName, name from user where userid < ?", " ", 4)) {
                    Trace.Assert(o.GetString(0) == o.GetString(1));
                    Trace.Assert(o.GetString(0) == "Fn" + nrs + " Ln" + nrs);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 4);
            // String operator in select clause with code properties
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SQL("select (FirstName+?+LastName) = name, userid from user where userid < ?", " ", 4)) {
                    Trace.Assert(o.GetBoolean(0) == true);
                    Trace.Assert(o.GetString(1) == DataPopulation.FakeUserId(nrs));
                    nrs++;
                }
            });
            Trace.Assert(nrs == 4);
            // Like in select clause
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SQL("select firstname like ?, name from user where userid between ? and ?", "Fn1%", 8, 13)) {
                    Trace.Assert(o.GetBoolean(0) == (nrs > 1));
                    nrs++;
                }
            });
            Trace.Assert(nrs == 6);
            // Like in select clause with code properties
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SQL("select name like ?, name from user where userid between ? and ?", "Fn1%", 8, 13)) {
                    Trace.Assert(o.GetBoolean(0) == (nrs > 1));
                    nrs++;
                }
            });
            Trace.Assert(nrs == 6);
#endif
            HelpMethods.LogEvent("Finished testing expressions in select clause");
        }
    }
}
