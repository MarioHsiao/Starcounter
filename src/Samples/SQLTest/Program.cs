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
            SqlTest.Test1.DataCreator.CreateData();
            SqlTest.Test1.DataCreator.DeleteData();
            //SqlTest.Test2.Indexes.CreateIndexes();
            //SqlTest.Test1.DataCreator.CreateData();
            //SqlTest.Test1.DataCreator.DeleteData();
            //SqlTest.Test2.Indexes.DropIndexes();
            //SqlTest.Test3.Indexes.CreateIndexes();
            //SqlTest.Test3.DataCreator.CreateData();
            //SqlTest.Test3.DataCreator.DeleteData();
            //SqlTest.Test3.Indexes.DropIndexes();
            Console.WriteLine("Finished SQLTest.");
        }
    }
}
