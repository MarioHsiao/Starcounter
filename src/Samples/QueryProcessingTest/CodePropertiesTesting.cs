using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class CodePropertiesTesting {
        public static void TestCodeProperties() {
            Db.Transaction(delegate {
                foreach (String name in Db.SQL<String>("select name from user where useridnr < ?", 5))
                    Console.WriteLine(name);
            });
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where useridnr < ?", 5))
                    Console.WriteLine(age);
            });
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where userid = ? and age < ?", DataPopulation.FakeUserId(2), 100))
                    Console.WriteLine(age);
            });
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where useridnr < ? and age < ?", 5, 100))
                    Console.WriteLine(age);
            });
            int counter = 0;
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where age < ?", 30))
                    counter++;
            });
            Console.WriteLine(counter);
            // Test sorting
            Db.Transaction(delegate {
                foreach (String name in Db.SQL<String>("select name from user where useridnr < ? order by name", 5))
                    Console.WriteLine(name);
            });
            // Test aggregate
            Db.Transaction(delegate {
                foreach (decimal ages in Db.SlowSQL<decimal>("select sum(age) from user u where age < ?", 30))
                    Console.WriteLine(ages);
            });
            // Test grouping
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SlowSQL("select age, count(name) from user u where age < ? group by age", 30))
                    Console.WriteLine(o.GetInt64(0)+" "+o.GetDecimal(1));
            });
            // Test with fetch
            Db.Transaction(delegate {
                foreach (IObjectView o in Db.SlowSQL("select age, count(name) from user u where age < ? group by age fetch ? offset ?", 35, 3, 2))
                    Console.WriteLine(o.GetInt64(0) + " " + o.GetDecimal(1));
            });
            int nrs = 0;
            Db.Transaction(delegate {
                foreach (String name in Db.SQL<String>("select name from user where name like ?", "Fn100%")) {
                    Console.WriteLine(name);
                    nrs++;
                }
            });
            Trace.Assert(nrs == 11);
        }
    }
}
