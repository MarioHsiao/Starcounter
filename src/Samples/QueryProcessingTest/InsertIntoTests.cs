using System;
using Starcounter;

namespace QueryProcessingTest {
    public static class InsertIntoTests {
        public static void TestValuesInsertIntoWebVisits() {
            HelpMethods.LogEvent("Test insert into statements with values on web visit data model");
            Db.Transaction(delegate {
                String query = "INSERT INTO WebPage (Title, uRL, PageValue, PersonalPageValue, TrackingCode, Located, deleted)" +
                    "Values ('MyCompany, AboutUs', '168.12.147.2/AboutUs', 100, 90, '', false, false)";
                Db.SQL(query);
            });
            HelpMethods.LogEvent("Finished testing insert into statements with values on web visit data model");
        }
    }
}
