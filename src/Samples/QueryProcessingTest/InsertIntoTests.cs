using System;
using Starcounter;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class InsertIntoTests {
        public static void TestValuesInsertIntoWebVisits() {
            HelpMethods.LogEvent("Test insert into statements with values on web visit data model");
            Db.Transaction(delegate {
                if (Db.SQL("select c from company c").First != null) {
                    WebPage w1 = Db.SQL<WebPage>("select w from webpage w where title = ?", "MyCompany, AboutUs").First;
                    w1.Delete();
                    foreach (Company c1 in Db.SQL<Company>("select c from company c"))
                        c1.Delete();
                    foreach (Country c1 in Db.SQL<Country>("select c from country c"))
                        c1.Delete();
                }
            });
            Db.Transaction(delegate {
                String query = "INSERT INTO WebPage (Title, uRL, PageValue, PersonalPageValue, TrackingCode, Located, deleted)" +
                    "Values ('MyCompany, AboutUs', '168.12.147.2/AboutUs', 100, 90, '', false, false)";
                Db.SQL(query);
            });
            WebPage w = Db.SQL<WebPage>("select w from Webpage w where title = ?", "MyCompany, AboutUs").First;
            Trace.Assert(w != null);
            Trace.Assert(w.Title == "MyCompany, AboutUs");
            Trace.Assert(w.URL == "168.12.147.2/AboutUs");
            Trace.Assert(w.PageValue == 100);
            Trace.Assert(w.PersonalPageValue == 90);
            Trace.Assert(w.TrackingCode == "");
            Trace.Assert(w.Located == false);
            Trace.Assert(w.Deleted == false);
            Db.Transaction(delegate { Db.SQL("insert into country(name) values ('Sweden'), ('Germany'), ('France')"); });
            
            var counEnum = Db.SQL<Country>("select c from country c").GetEnumerator();
            Trace.Assert(counEnum.MoveNext());
            Country c = counEnum.Current;
            Trace.Assert(c.Name == "Sweden");
            Trace.Assert(counEnum.MoveNext());
            c = counEnum.Current;
            Trace.Assert(c.Name == "Germany");
            Trace.Assert(counEnum.MoveNext());
            c = counEnum.Current;
            Trace.Assert(c.Name == "France");
            Trace.Assert(!counEnum.MoveNext());
            Db.Transaction(delegate { Db.SQL("insert into company (name,country) values ('Canal+',object " + c.GetObjectNo()+")"); });
            var compEnum = Db.SQL<Company>("select c from company c").GetEnumerator();
            Trace.Assert(compEnum.MoveNext());
            Company co = compEnum.Current;
            Trace.Assert(co.Name == "Canal+");
            Trace.Assert(co.Country.Equals(c));
            Trace.Assert(!compEnum.MoveNext());
            HelpMethods.LogEvent("Finished testing insert into statements with values on web visit data model");
        }
    }
}
