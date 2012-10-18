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
            Test2();
            Test1();
            Test3();
            Console.WriteLine("Finished SQLTest.");
        }

        static Boolean Test1()
        {
            TestRunner.Initialize("SqlTest1", false, true, false);
            SQLTest.EmployeeDb.EmployeeData.CreateData();
            TestRunner.RunTest();
            SQLTest.EmployeeDb.EmployeeData.DeleteData();
            return true;
        }

        static Boolean Test2()
        {
            TestRunner.Initialize("SqlTest2", false, true, false);
            SQLTest.EmployeeDb.EmployeeData.CreateIndexes();
            SQLTest.EmployeeDb.EmployeeData.CreateData();
            TestRunner.RunTest();
            SQLTest.EmployeeDb.EmployeeData.DeleteData();
            SQLTest.EmployeeDb.EmployeeData.DropIndexes();
            return true;
        }
        
        static Boolean Test3()
        {
            TestRunner.Initialize("SqlTest3", false, true, false);
            SQLTest.PointDb.PointData.CreateIndexes();
            SQLTest.PointDb.PointData.CreateData();
            TestRunner.RunTest();
            SQLTest.PointDb.PointData.DeleteData();
            SQLTest.PointDb.PointData.DropIndexes();
            return true;
        }
    }
}
