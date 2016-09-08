using System;
using Starcounter;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class NewSqlTest {
        public static void RunNewSqlTests() {
            HelpMethods.LogEvent("Test New SQL");
            SimpleSelectTest();
            HelpMethods.LogEvent("Finished testing New SQL");
        }

        public static void SimpleSelectTest() {
            UpdateTestItem i1 = null, i2 = null;
            Db.Transact(delegate {
                i1 = new UpdateTestItem { field = 1 };
                i2 = new UpdateTestItem { field = 2 };
            });
            var queryOldEnum = Db.SQL<UpdateTestItem>("select w from QueryProcessingTest.updatetestitem w");
            Trace.Assert(queryOldEnum != null);
            var queryEnum = Db.NewSQL<UpdateTestItem>("select w from QueryProcessingTest.updatetestitem w");
            Trace.Assert(queryEnum != null);
            Db.Transact(delegate {
                i1.Delete();
                i2.Delete();
            });
        }
    }
}
