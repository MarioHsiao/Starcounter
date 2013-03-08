using Starcounter;
using Starcounter.Binding;
using System;
using System.Collections;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Test of CREATE/DROP INDEX and DROP TABLE.");
#if ACCOUNTTEST_MODEL
            Console.WriteLine("Test with loading model");
            TestCreateIndexWithoutQuery();
            TestDelete();
            Populate();
            PrintAllObjects();
            // See a query plan
            Db.Transaction(delegate
            {
                IEnumerator sqlEnum = (IEnumerator)Db.SQL("select u from accounttest.user u").GetEnumerator();
                Console.WriteLine(sqlEnum.ToString());
            });
            TestCreateDropIndex();
            TestOrderBy();
            TestHint();
            TestJoinWIndex();
            TestAggregate();

            InheritedIndex.InheritedIndexTest.RunInheritedIndexTest();
            IsTypePredicateTest.RunIsTypePredicateTest();
            //CreateDropIndexParallelTest();
#endif
#if ACCOUNTTEST_MODEL_NO
            Console.WriteLine("Test without loading model.");
            Db.SlowSQL("DROP TABLE AccountTest.Account");
            Db.SlowSQL("DROP TABLE AccountTest.User");
#endif
            Console.WriteLine("Test completed.");
            Environment.Exit(0);
        }
    }
}
