using System;
using Starcounter;

namespace QueryProcessingTest {
    public static class SqlBugsTest {
        public static void QueryTests() {
            // Test query with FETCH and ORDER BY
            decimal amounts = 0;
            var accounts = Db.SQL("select a from account a order by a.updated desc fetch ?", 10);
            foreach (Account a in Db.SQL("select a from account a order by a.updated desc fetch ?", 10))
                amounts += a.Amount;
        }
    }
}
