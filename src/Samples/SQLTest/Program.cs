using System;
using Starcounter;
using Starcounter.Binding;

namespace SqlTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started SQLTest.");
            Test1();
            Test2();
            Test3();
            Console.WriteLine("Finished SQLTest.");
        }

        static Boolean Test1()
        {
            SqlTest.EmployeeDb.EmployeeData.CreateData();
            SqlTest.EmployeeDb.EmployeeData.DeleteData();
            return true;
        }

        static Boolean Test2()
        {
            SqlTest.EmployeeDb.EmployeeData.CreateIndexes();
            SqlTest.EmployeeDb.EmployeeData.CreateData();
            SqlTest.EmployeeDb.EmployeeData.DeleteData();
            SqlTest.EmployeeDb.EmployeeData.DropIndexes();
            return true;
        }
        
        static Boolean Test3()
        {
            SqlTest.PointDb.PointData.CreateIndexes();
            SqlTest.PointDb.PointData.CreateData();
            SqlTest.PointDb.PointData.DeleteData();
            SqlTest.PointDb.PointData.DropIndexes();
            return true;
        }
    }
}
