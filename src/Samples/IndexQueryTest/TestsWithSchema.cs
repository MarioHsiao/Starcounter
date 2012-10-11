using System;
using Starcounter;
using Starcounter.Binding;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {
#if ACCOUNTTEST_MODEL
        static void TestDelete()
        {
            Db.Transaction(delegate
            {
                if (Db.SQL("select u from user u").First == null)
                {
                    Console.WriteLine("It seems that User table was deleted");
                    PrintAllObjects();
                }
                Db.SlowSQL("DELETE FROM Account");
                Db.SlowSQL("DELETE FROM User");
            });
        }

        static void QueryIndexUserLN()
        {
            PrintUserByLastName("Popov");
            Db.Transaction(delegate
            {
                Console.WriteLine(((ISqlEnumerator)Db.SQL("select u from User u where LastName = ?", "Popov").GetEnumerator()).ToString());
            });
        }

        static void CreateIndexUserLN()
        {
            Db.Transaction(delegate
            {
                try
                {
                    Db.SlowSQL("CREATE INDEX userLN ON AccountTest.User (LastName ASC)");
                    Console.WriteLine("Created index userLN ON AccountTest.User (LastName ASC)");
                }
                catch (Starcounter.DbException ex)
                {
                    if (ex.ErrorCode == Starcounter.Error.SCERRNAMEDINDEXALREADYEXISTS)
                        Console.WriteLine("Index userLN already exists.");
                    else
                        throw ex;
                }
            });
        }

        static void DropIndexUserLN()
        {
            Db.SlowSQL("DROP INDEX userLN ON AccountTest.User");
            Console.WriteLine("Dropped index userLN ON AccountTest.User");
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
                Console.WriteLine(((ISqlEnumerator)Db.SQL("select u from User u order by LastName").GetEnumerator()).ToString());
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
                foreach (AccountTest.User u in Db.SQL("select u from user u where userid = ? option index (u userLN)", "KalLar01"))
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

#endif
    }
}
