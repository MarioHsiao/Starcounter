using System;
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
            foreach (Account a in Db.SQL("select a from account a order by a.updated desc fetch ?", 20)) {
                amounts += a.Amount;
                nrs++;
            }
            Trace.Assert(nrs == 20);
        }
    }
}
