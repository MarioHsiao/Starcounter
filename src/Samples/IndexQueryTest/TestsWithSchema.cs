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

        static void TestCreateDropIndex()
        {
            // Test that query plan before creating index
            PrintUserByLastName("Popov");
            Db.Transaction(delegate
            {
                Console.WriteLine(((ISqlEnumerator)Db.SQL("select u from User u where LastName = ?", "Popov").GetEnumerator()).ToString());
            });
            // Create index if necessary
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
            // Test that query plan uses the created index
            PrintUserByLastName("Popov");
            Db.Transaction(delegate
            {
                Console.WriteLine(((ISqlEnumerator)Db.SQL("select u from User u where LastName = ?", "Popov").GetEnumerator()).ToString());
            });
            Db.SlowSQL("DROP INDEX userLN ON AccountTest.User");
            Console.WriteLine("Dropped index userLN ON AccountTest.User");
            PrintUserByLastName("Popov");
            Db.Transaction(delegate
            {
                Console.WriteLine(((ISqlEnumerator)Db.SQL("select u from User u where LastName = ?", "Popov").GetEnumerator()).ToString());
            });
        }
#endif
    }
}
