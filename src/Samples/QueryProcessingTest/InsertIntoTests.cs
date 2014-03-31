using System;
using Starcounter;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class InsertIntoTests {
        public static void TestValuesInsertIntoWebVisits() {
            HelpMethods.LogEvent("Test insert into statements with values on web visit data model");
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
            HelpMethods.LogEvent("Finished testing insert into statements with values on web visit data model");
        }
    }
}
