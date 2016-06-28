using Starcounter;
using System;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class WebVisitTests {
        public static void TestVisits() {
            HelpMethods.LogEvent("Test queries on web visit data model");
            Db.SQL(@"create index impressionvisit 
                        on Impression (Visit)");
            PopulateData();
            foreach (Visit v in Db.SlowSQL<Visit>(@"select v from Visit v 
                INNER JOIN Impression i0 ON i0.Visit = v INNER JOIN Impression i1 ON i1.Visit = v 
                INNER JOIN Impression i2 ON i2.Visit = v where i0.WebPage.Title = ? AND i1.WebPage.Title = ? 
                and i2.WebPage.Title = ? GROUP BY v ORDER BY v.Start DESC", "About Us", "Contact", "Welcome"))
                Trace.Assert(v.Id >= 0);
            foreach (Visit v in Db.SlowSQL<Visit>("select v from Visit v " +
                "INNER JOIN Impression i ON i.Visit = v " +
                "INNER JOIN Impression i0 ON i0.Visit = v INNER JOIN Impression i1 ON i1.Visit = v " +
                "INNER JOIN Impression i2 ON i2.Visit = v where i0.WebPage.Title = ? AND i1.WebPage.Title = ? " +
                "and i2.WebPage.Title = ? FETCH ? OFFSET ?", "About Us", "Contact", "Welcome", 10, 0))
                Trace.Assert(v.Id >= 0);
            foreach (Visit v in Db.SlowSQL("SELECT v FROM Visit v "+
                "INNER JOIN Impression i ON i.Visit = v INNER JOIN Impression i0 ON i0.Visit = v "+
                "INNER JOIN Impression i1 ON i1.Visit = v INNER JOIN Impression i2 ON  i2.Visit = v "+
                "WHERE i0.WebPage.Title = ? AND i1.WebPage.Title = ? AND i2.WebPage.Title = ? "+
                "FETCH ? OFFSET ?", "About us", "Contact", "Home Page", 10, 0))
                Trace.Assert(v.Id >= 0);
            foreach (Visit v in Db.SQL<Visit>(@"select v from Visit v 
                INNER JOIN Impression i0 ON i0.Visit = v INNER JOIN Impression i1 ON i1.Visit = v 
                INNER JOIN Impression i2 ON i2.Visit = v where i0.WebPage.Title = ? AND i1.WebPage.Title = ? 
                and i2.WebPage.Title = ? GROUP BY v ORDER BY v.Start DESC", "About Us", "Contact", "Welcome"))
                Trace.Assert(v.Id >= 0);
            Db.SQL("drop index impressionvisit on Impression");
            foreach (Visit v in Db.SQL<Visit>("select v from Visit v " +
                "INNER JOIN Impression i0 ON i0.Visit = v INNER JOIN Impression i1 ON i1.Visit = v " +
                "INNER JOIN Impression i2 ON i2.Visit = v where i0.WebPage.Title = ? AND i1.WebPage.Title = ? " +
                "and i2.WebPage.Title = ? GROUP BY v ORDER BY v.Start DESC", "About Us", "Contact", "Welcome")) {
                Trace.Assert(v.Id >= 0);
                Trace.Assert(v.IpBytes.Equals(new Binary(new byte[] { 1, 1, 1, 1 })));
            }
            TestBinaries();
            HelpMethods.LogEvent("Finished testing queries on web visit data model");
        }

        public static void TestBinaries() {
            var visits = Db.SQL<Visit>("select v from visit v where IpBytes = ? and id = ?", new Binary(new byte[] { 1, 1, 1, 1 }), 1).GetEnumerator();
            Trace.Assert(visits.MoveNext());
            Visit vi = visits.Current;
            Trace.Assert(vi.Id == 1);
            Trace.Assert(vi.IpBytes.Equals(new Binary(new byte[] { 1, 1, 1, 1 })));
            Trace.Assert(Db.BinaryToHex(vi.IpBytes) == "01010101");
            Trace.Assert(!visits.MoveNext());
            visits.Dispose();
            Db.SQL("create index ipBytesIndx on visit(ipbytes)");
            vi = Db.SQL<Visit>("select v from visit v where IpBytes = ?", new Binary(new byte[] { 1, 1, 1, 1 })).First;
            Trace.Assert(vi != null);
            Trace.Assert(vi.IpBytes.Equals(new Binary(new byte[] { 1, 1, 1, 1 })));
            Trace.Assert(Db.BinaryToHex(vi.IpBytes) == "01010101");
            vi = Db.SQL<Visit>("select v from visit v where IpBytes > ?", new Binary(new byte[] { 1, 1, 1, 0 })).First;
            Trace.Assert(vi != null);
            Trace.Assert(vi.IpBytes.Equals(new Binary(new byte[] { 1, 1, 1, 1 })));
            Trace.Assert(Db.BinaryToHex(vi.IpBytes) == "01010101");
            Db.SQL("drop index ipBytesIndx on visit");
        }

        static void PopulateData() {
            if (Db.SQL("select w from webpage w").First != null)
                return;
            Db.Transact(delegate {
                var web1 = new WebPage() {
                    Title = "About us"
                };

                var web2 = new WebPage() {
                    Title = "Contact"
                };

                for (ulong i = 0; i < 1000; i++) {
                    var vis1 = new Visit() {
                        Id = i,
                        Ip = "1.1.1.1",
                        IpBytes = new Binary(new byte[] { 1, 1, 1, 1 })
                    };

                    var imp1 = new Impression() {
                        Visit = vis1,
                        WebPage = web1
                    };

                    var imp2 = new Impression() {
                        Visit = vis1,
                        WebPage = web2
                    };
                }
            });
            if (Db.SQL("select a from agent a").First == null)
                Db.Transact(delegate {
                    new Agent();
                });
        }
    }
}
