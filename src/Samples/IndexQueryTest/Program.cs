using Starcounter;
using Starcounter.Binding;
using System;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Test of CREATE/DROP INDEX and DROP TABLE.");
#if ACCOUNTTEST_MODEL
            Console.WriteLine("Test with loading model");
            TestDelete();
            Populate();
            PrintAllObjects();
            // See a query plan
            Db.Transaction(delegate
            {
                ISqlEnumerator sqlEnum = (ISqlEnumerator)Db.SQL("select u from user u").GetEnumerator();
                Console.WriteLine(sqlEnum.ToString());
            });
            TestCreateDropIndex();
            TestOrderBy();
            TestHint();
            //CreateDropIndexParallelTest();
#endif
#if ACCOUNTTEST_MODEL_NO
            Console.WriteLine("Test without loading model.");
            Db.SlowSQL("DROP TABLE AccountTest.Account");
            Db.SlowSQL("DROP TABLE AccountTest.User");
#endif
            Console.WriteLine("Test completed.");
        }
    }
}
