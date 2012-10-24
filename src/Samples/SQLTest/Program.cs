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
            Test1and2();
            Test3();
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
    }
}
