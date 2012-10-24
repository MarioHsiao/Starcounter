using System;
using Starcounter;
using Starcounter.Binding;

namespace SQLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started SQLTest.");
#if true
            Test1and2();
            Test3();
#else
            TempTest();
#endif
            Console.WriteLine("Finished SQLTest.");
        }

        static Boolean Test1and2()
        {
            TestRunner.Initialize("SqlTest2", false, true, false);
            SQLTest.EmployeeDb.EmployeeData.CreateIndexes();
            SQLTest.EmployeeDb.EmployeeData.CreateData();
            TestRunner.RunTest();
            SQLTest.EmployeeDb.EmployeeData.DropIndexes();
            TestRunner.Initialize("SqlTest1", false, true, false);
            TestRunner.RunTest();
            SQLTest.EmployeeDb.EmployeeData.DeleteData();
            return true;
        }

        static Boolean Test3()
        {
            TestRunner.Initialize("SqlTest3", false, true, false);
            SQLTest.PointDb.PointData.CreateData();
            SQLTest.PointDb.PointData.CreateIndexes();
            TestRunner.RunTest();
            SQLTest.PointDb.PointData.DeleteData();
            SQLTest.PointDb.PointData.DropIndexes();
            return true;
        }

        static void TempTest()
        {
            Db.Transaction(delegate
            {
#if false
                if (Db.SQL("select e from SalaryEmployee e where e.Manager = ?","object 25").First != null)
                    Console.WriteLine("Not null.    ");
                if (Db.SQL("select e from SalaryEmployee e where e.Manager = object 25").First != null)
                    Console.WriteLine("Not null.    ");
#endif
            });
        }
    }
}
