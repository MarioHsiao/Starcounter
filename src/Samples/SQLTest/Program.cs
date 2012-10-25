using System;
using Starcounter;
using Starcounter.Binding;

namespace SQLTest
{
    class Program
    {
        static int Main(string[] args)
        {
            int nrFailedQueries = 0;
            String outputPath = null;
            if (args != null)
                outputPath = args[0];
            Console.WriteLine("Started SQLTest.");
            nrFailedQueries += Test1and2(outputPath);
            nrFailedQueries += Test3(outputPath);
            Console.WriteLine("Finished SQLTest.");
            return nrFailedQueries;
        }

        static int Test1and2(String outputPath)
        {
            int nrFailedQueries = 0;
            TestRunner.Initialize("SqlTest2", outputPath, false, true, false);
            SQLTest.EmployeeDb.EmployeeData.CreateIndexes();
            SQLTest.EmployeeDb.EmployeeData.CreateData();
            nrFailedQueries += TestRunner.RunTest();
            SQLTest.EmployeeDb.EmployeeData.DropIndexes();
            TestRunner.Initialize("SqlTest1", outputPath, false, true, false);
            nrFailedQueries += TestRunner.RunTest();
            SQLTest.EmployeeDb.EmployeeData.DeleteData();
            return nrFailedQueries;
        }

        static int Test3(String outputPath)
        {
            int nrFailedQueries = 0;
            TestRunner.Initialize("SqlTest3", outputPath, false, true, false);
            SQLTest.PointDb.PointData.CreateData();
            SQLTest.PointDb.PointData.CreateIndexes();
            nrFailedQueries += TestRunner.RunTest();
            SQLTest.PointDb.PointData.DeleteData();
            SQLTest.PointDb.PointData.DropIndexes();
            return nrFailedQueries;
        }
    }
}
