using System;
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
                foreach (int age in Db.SQL<int>("select age from user u where userid = ? and age < ?", DataPopulation.FakeUserId(2),100))
                    Console.WriteLine(age);
            });
#if true   // Do not work due to code gen
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where useridnr < ? and age < ?", 5, 100))
                    Console.WriteLine(age);
            });
            Db.Transaction(delegate {
                foreach (int age in Db.SQL<int>("select age from user u where age < ?", 20))
                    Console.WriteLine(age);
            });
#endif
        }
    }
}
