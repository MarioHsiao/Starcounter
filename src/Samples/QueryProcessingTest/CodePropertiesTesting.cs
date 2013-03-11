using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class CodePropertiesTesting {
        public static void TestCodeProperties() {
            HelpMethods.LogEvent("Test code properties");
            int nrs = 0;
            Db.Transaction(delegate {
                foreach (String name in Db.SQL<String>("select name from user where useridnr < ?", 5)) {
                    Trace.Assert(name.IndexOf("Fn") == 0);
                    Trace.Assert(name.IndexOf("Ln") == 4);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 5);
            nrs = 0;
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where useridnr < ?", 5)) {
                    Trace.Assert(age >= DataPopulation.YoungestAge && age <= DataPopulation.OldestAge);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 5);
            nrs = 0;
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where userid = ? and age < ?", DataPopulation.FakeUserId(2), 100)) {
                    Trace.Assert(age >= DataPopulation.YoungestAge && age < 100);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 1);
            nrs = 0;
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where useridnr < ? and age < ?", 5, 100)) {
                    Trace.Assert(age >= DataPopulation.YoungestAge && age < 100);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 5);
            nrs = 0;
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where age < ?", 30)) {
                    Trace.Assert(age >= DataPopulation.YoungestAge && age < 30);
                    nrs++;
                }
            });
            Trace.Assert(nrs > 0 && nrs < 10000);
            // Test sorting
            nrs = 0;
            Db.Transaction(delegate {
                foreach (String name in Db.SQL<String>("select name from user where useridnr < ? order by name", 5)) {
                    Trace.Assert(name == "Fn" + nrs + " Ln" + nrs);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 5);
            // Test aggregate
            nrs = 0;
            Db.Transaction(delegate {
                foreach (decimal ages in Db.SlowSQL<decimal>("select sum(age) from user u where age < ?", 30)) {
                    Trace.Assert(ages > DataPopulation.YoungestAge  && ages < 30 * 10000);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 1);
            // Test grouping
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SlowSQL("select age, count(name) from user u where age < ? group by age", 30)) {
                    Trace.Assert(o.GetInt64(0) == nrs + 28);
                    Trace.Assert(o.GetDecimal(1) > 0 && o.GetDecimal(1) < 10000);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 2);
            // Test with fetch
            nrs = 0;
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SlowSQL("select age, count(name) from user u where age < ? group by age fetch ? offset ?", 35, 3, 2)) {
                    Trace.Assert(o.GetInt64(0) == nrs + 30);
                    Trace.Assert(o.GetDecimal(1) > 0);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 3);
            nrs = 0;
            Db.Transaction(delegate {
                foreach (String name in Db.SQL<String>("select name from user where name like ?", "Fn100%")) {
                    Trace.Assert(name.IndexOf("Fn100") == 0 && (name.IndexOf("Ln100") == 7 || name.IndexOf("Ln100") == 6));
                    nrs++;
                }
            });
            Trace.Assert(nrs == 11);
            HelpMethods.LogEvent("Finished testing code properties");
        }
    }
}
