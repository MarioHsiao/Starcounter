using Starcounter;
using System;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class WebVisitTests {
        public static void TestVisits() {
            HelpMethods.LogEvent("Test queries on web visit data model");
            Db.SQL("create index impressionvisit on Impression (Visit)");
            foreach (Visit v in Db.SlowSQL<Visit>("select v from Visit v " +
                "INNER JOIN Impression i0 ON i0.Visit = v INNER JOIN Impression i1 ON i1.Visit = v " +
                "INNER JOIN Impression i2 ON i2.Visit = v where i0.WebPage.Title = ? AND i1.WebPage.Title = ? " +
                "and i2.WebPage.Title = ? GROUP BY v ORDER BY v.Start DESC", "About Us", "Contact", "Welcome"))
                Trace.Assert(v.Id >= 0);
            Db.SQL("drop index impressionvisit on Impression");
            foreach (Visit v in Db.SQL<Visit>("select v from Visit v " +
                "INNER JOIN Impression i0 ON i0.Visit = v INNER JOIN Impression i1 ON i1.Visit = v " +
                "INNER JOIN Impression i2 ON i2.Visit = v where i0.WebPage.Title = ? AND i1.WebPage.Title = ? " +
                "and i2.WebPage.Title = ? GROUP BY v ORDER BY v.Start DESC", "About Us", "Contact", "Welcome"))
                Trace.Assert(v.Id >= 0);
            HelpMethods.LogEvent("Finished testing queries on web visit data model");
        }
    }
}
