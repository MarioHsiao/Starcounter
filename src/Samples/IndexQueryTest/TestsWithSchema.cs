using System;
using System.Collections;
using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Execution;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {
#if ACCOUNTTEST_MODEL
        static void TestDelete()
        {
            Db.Transaction(delegate
            {
                if (Db.SQL("select u from Accounttest.user u").First == null)
                {
                    Console.WriteLine("It seems that User table was deleted");
                    PrintAllObjects();
                }
                Db.SlowSQL("DELETE FROM Account");
                Db.SlowSQL("DELETE FROM Accounttest.User");
            });
        }

        static void QueryIndexUserLN()
        {
            PrintUserByLastName("Popov");
            Db.Transaction(delegate
            {
                Console.WriteLine(((IEnumerator)Db.SQL("select u from User u where LastName = ?", "Popov").GetEnumerator()).ToString());
            });
        }

        static void CreateIndexUserLN()
        {
            try
            {
                Db.SlowSQL("CREATE INDEX userLN ON Accounttest.UsEr (Lastname ASC)");
                Console.WriteLine("Created index userLN ON accounttest.User (LastName ASC)");
            }
            catch (Starcounter.DbException ex)
            {
                if (ex.ErrorCode == Starcounter.Error.SCERRNAMEDINDEXALREADYEXISTS)
                    Console.WriteLine("Index userLN already exists.");
                else
                    throw ex;
            }
        }

        static void DropIndexUserLN()
        {
            Db.SlowSQL("DROP INDEX UserLN ON accounttest.user");
            Console.WriteLine("Dropped index userLN ON accounttest.User");
        }

        static void TestCreateDropIndex()
        {
            Console.WriteLine("Test create and drop index with WHERE query");
            // Test that query plan before creating index
            QueryIndexUserLN();
            // Create index if necessary
            CreateIndexUserLN();
            // Test that query plan uses the created index
            QueryIndexUserLN();
            DropIndexUserLN();
            QueryIndexUserLN();
        }

        static void OrderByQueryIndexUserLN()
        {
            PrintUsersOrderByLastName();
            Db.Transaction(delegate
            {
                Console.WriteLine(((IEnumerator)Db.SQL("select u from User u order by LastName").GetEnumerator()).ToString());
            });
        }

        static void TestOrderBy()
        {
            Console.WriteLine("Test create and drop index with ORDER BY query");
            OrderByQueryIndexUserLN();
            CreateIndexUserLN();
            OrderByQueryIndexUserLN();
            DropIndexUserLN();
            OrderByQueryIndexUserLN();
        }

        static void HintQueryIndexUserLN()
        {
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from user u where userid = ? option index (u userLN)", "KalLar01"))
                    Console.WriteLine(u.ToString());
                Console.WriteLine(Db.SQL("select u from user u where userid = ? option index (u userLN)", "KalLar01").GetEnumerator().ToString());
            });
        }

        static void TestHint()
        {
            Console.WriteLine("Test create and drop index with HINT query");
            HintQueryIndexUserLN();
            CreateIndexUserLN();
            HintQueryIndexUserLN();
            DropIndexUserLN();
            HintQueryIndexUserLN();
        }

        static void TestJoinWIndex() {
            Console.WriteLine("Test path expression as join with index");
            CreateIndexUserLN();
            Db.Transaction(delegate {
                foreach (accounttest.account a in Db.SQL("select a from account a where a.Client.lastname = ?", "Popov")) {
                    Console.WriteLine(a.Client.ToString());
                    Console.WriteLine(a.ToString());
                }
            });
            DropIndexUserLN();
        }

        static void TestCreateIndexWithoutQuery()
        {
            Console.WriteLine("Test create/drop index without doing query");
            CreateIndexUserLN();
            DropIndexUserLN();
        }

        static void TestSumTransaction() {
            Decimal? sum = Db.SlowSQL("select sum(amount*(amount - amount +2)) from account").First;
            if (sum == null)
                Console.WriteLine("The sum is null");
            else Console.WriteLine("The sum is " + sum);
        }

        static void TestSumTransaction(String name) {
            Decimal? sum = Db.SlowSQL("select sum(amount) from account where Client.FirstName = ?", 
                name).First;
            if (sum == null)
                Console.WriteLine("The sum is null");
            else Console.WriteLine("The sum is " + sum);
        }

        static void TestAggregate() {
            Console.WriteLine("Test Aggregate");
            Db.Transaction(delegate {
                TestSumTransaction();
            });
            TestSumTransaction();
            Db.Transaction(delegate {
                TestSumTransaction("Oleg");
            });
            TestSumTransaction("Oleg");
            Console.WriteLine("Test finished");
        }
#endif
    }
}
