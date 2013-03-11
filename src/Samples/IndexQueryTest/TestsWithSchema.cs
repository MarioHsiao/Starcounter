using System;
using System.Collections;
using System.Diagnostics;
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
#if false
                if (Db.SQL("select u from Accounttest.user u").First == null)
                {
                    Console.WriteLine("It seems that User table was deleted");
                    PrintAllObjects();
                }
#endif
                Db.SlowSQL("DELETE FROM Account");
                Db.SlowSQL("DELETE FROM Accounttest.User");
            });
        }

        static void QueryIndexUserLN()
        {
            CountUserByLastName("Popov");
            Db.Transaction(delegate
            {
                Trace.Assert(((IEnumerator)Db.SQL("select u from User u where LastName = ?", "Popov").GetEnumerator()).ToString() != "");
            });
        }

        static Boolean CreateIndexUserLN()
        {
            try
            {
                Db.SlowSQL("CREATE INDEX userLN ON Accounttest.UsEr (Lastname ASC)");
                //Console.WriteLine("Created index userLN ON accounttest.User (LastName ASC)");
                return true;
            }
            catch (Starcounter.DbException ex)
            {
                if (ex.ErrorCode == Starcounter.Error.SCERRNAMEDINDEXALREADYEXISTS)
                    //Console.WriteLine("Index userLN already exists.");
                    return false;
                else
                    throw ex;
            }
        }

        static Boolean DropIndexUserLN()
        {
            Db.SlowSQL("DROP INDEX UserLN ON accounttest.user");
            return true;
            //Console.WriteLine("Dropped index userLN ON accounttest.User");
        }

        static void TestCreateDropIndex()
        {
            HelpMethods.LogEvent("Test create and drop index with WHERE query");
            // Test that query plan before creating index
            QueryIndexUserLN();
            // Create index if necessary
            CreateIndexUserLN();
            // Test that query plan uses the created index
            QueryIndexUserLN();
            DropIndexUserLN();
            QueryIndexUserLN();
            HelpMethods.LogEvent("Finished testing create and drop index with WHERE query");
        }

        static void OrderByQueryIndexUserLN()
        {
            CountUsersOrderByLastName();
            Db.Transaction(delegate
            {
                Trace.Assert(((IEnumerator)Db.SQL("select u from User u order by LastName").GetEnumerator()).ToString() != "");
            });
        }

        static void TestOrderBy()
        {
            HelpMethods.LogEvent("Test create and drop index with ORDER BY query");
            OrderByQueryIndexUserLN();
            CreateIndexUserLN();
            OrderByQueryIndexUserLN();
            DropIndexUserLN();
            OrderByQueryIndexUserLN();
            HelpMethods.LogEvent("Finished testing create and drop index with ORDER BY query");
        }

        static void HintQueryIndexUserLN()
        {
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from user u where userid = ? option index (u userLN)", "KalLar01"))
                    Trace.Assert(u.UserId == "KalLar01");
                Trace.Assert(Db.SQL("select u from user u where userid = ? option index (u userLN)", "KalLar01").GetEnumerator().ToString() != "");
            });
        }

        static void TestHint()
        {
            HelpMethods.LogEvent("Test create and drop index with HINT query");
            HintQueryIndexUserLN();
            CreateIndexUserLN();
            HintQueryIndexUserLN();
            DropIndexUserLN();
            HintQueryIndexUserLN();
            HelpMethods.LogEvent("Finished testing create and drop index with HINT query");
        }

        static void TestJoinWIndex() {
            HelpMethods.LogEvent("Test path expression as join with index");
            CreateIndexUserLN();
            int nrs = 0;
            Db.Transaction(delegate {
                foreach (accounttest.account a in Db.SQL("select a from account a where a.Client.lastname = ?", "Popov")) {
                    Trace.Assert(a.Client.LastName == "Popov");
                    nrs++;
                }
            });
            Trace.Assert(nrs == 3);
            DropIndexUserLN();
            HelpMethods.LogEvent("Finished testing path expression as join with index");
        }

        static void TestCreateIndexWithoutQuery()
        {
            HelpMethods.LogEvent("Test create/drop index without doing query");
            Trace.Assert(CreateIndexUserLN() == true);
            Trace.Assert(CreateIndexUserLN() == false);
            Trace.Assert(DropIndexUserLN() == true);
            HelpMethods.LogEvent("Finished testing create/drop index");
        }

        static void TestSumTransaction() {
            Decimal? sum = Db.SlowSQL("select sum(amount*(amount - amount +2)) from account").First;
            Trace.Assert(sum == 170);
        }

        static void TestSumTransaction(String name) {
            Decimal? sum = Db.SlowSQL("select sum(amount) from account where Client.FirstName = ?", 
                name).First;
            if (name == "Oleg")
                Trace.Assert(sum == 55);
            if (name == "Kalle")
                Trace.Assert(sum == 30);
        }

        static void TestAggregate() {
            HelpMethods.LogEvent("Test Aggregate");
            Db.Transaction(delegate {
                TestSumTransaction();
            });
            TestSumTransaction();
            Db.Transaction(delegate {
                TestSumTransaction("Oleg");
            });
            TestSumTransaction("Oleg");
            HelpMethods.LogEvent("Finished testing Aggregate");
        }
#endif
    }
}
