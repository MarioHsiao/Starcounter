using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class AggregationTest {
        public static void RunAggregationTest() {
            // Aggregate without grouping
            int nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ?", 10)) {
                Trace.Assert(s == 900m);
                nrs++;
            }
            Trace.Assert(nrs == 1);
            // Aggregate without grouping with fetch
            nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ? fetch ?", 10, 2)) {
                Trace.Assert(s == 900m);
                nrs++;
            }
            Trace.Assert(nrs == 1);
            nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ? fetch ?", 10, 0)) {
                Trace.Assert(s == 0m);
                nrs++;
            }
            Trace.Assert(nrs == 0);
            // Aggregate without grouping with offset
            nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ? offset ?", 10, 0)) {
                Trace.Assert(s == 900m);
                nrs++;
            }
            Trace.Assert(nrs == 1);
            nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ? offset ?", 10, 2)) {
                Trace.Assert(s == 0m);
                nrs++;
            }
            Trace.Assert(nrs == 0);
            // Aggregate without grouping with fetch and offset
            HelpMethods.PrintSlowQueryPlan("select sum(a.Amount) from account a where accountid < ? fetch ? offset ?");
            nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ? fetch ? offset ?", 10, 2, 0)) {
                Trace.Assert(s == 900m);
                nrs++;
            }
            Trace.Assert(nrs == 1);
            nrs = 0;
            foreach (Decimal s in Db.SlowSQL("select sum(a.Amount) from account a where accountid < ? fetch ? offset ?", 10, 2, 2)) {
                Trace.Assert(s == 0m);
                nrs++;
            }
            Trace.Assert(nrs == 0);
            // Aggregate with grouping
            HelpMethods.PrintSlowQueryPlan("select sum(Amount) from account where accountid < ? group by client");
            nrs = 0;
            foreach (Decimal d in Db.SlowSQL("select sum(Amount) from account where accountid < ? group by client", 10)) {
                if (nrs == 3)
                    Trace.Assert(d == 0m);
                else
                    Trace.Assert(d == 300m);
                nrs++;
            }
            Trace.Assert(nrs == 4);
            // Aggregate with grouping with fetch
            nrs = 0;
            foreach (Decimal d in Db.SlowSQL("select sum(Amount) from account where accountid < ? group by client fetch ?", 12, 2)) {
                Trace.Assert(d == 300m);
                nrs++;
            }
            Trace.Assert(nrs == 2);
            // Aggregate with grouping with offset
            nrs = 0;
            foreach (Decimal d in Db.SlowSQL("select sum(Amount) from account where accountid < ? group by client offset ?", 12, 2)) {
                Trace.Assert(d == 300m);
                nrs++;
            }
            Trace.Assert(nrs == 2);
            // Aggregate with grouping with fetch and offset
            nrs = 0;
            foreach (Decimal d in Db.SlowSQL("select sum(Amount) from account where accountid < ? group by client fetch ? offset ?", 12, 2, 1)) {
                Trace.Assert(d == 300m);
                nrs++;
            }
            Trace.Assert(nrs == 2);
        }
    }
}
