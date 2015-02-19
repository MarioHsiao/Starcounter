using System;
using System.Threading;
using Starcounter;
using Starcounter.Binding;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {
#if ACCOUNTTEST_MODEL
        #region Workers
        static bool queryStarted = false;
        static bool indexCreated = false;
        static void QueryTheIndexSync()
        {
            Db.Transact(delegate
            {
                queryStarted = true;
                while (!indexCreated) ;
                accounttest.User user = (accounttest.User)Db.SQL("SELECT u FROM User u WHERE UserId = ?", "KalLar01").First;
                Console.WriteLine(Db.SQL("SELECT u FROM User u WHERE UserId = ?", "KalLar01").GetEnumerator().ToString());
            });
        }
        static void QueryTheIndex()
        {
            Db.Transact(delegate
            {
                accounttest.User user = (accounttest.User)Db.SQL("SELECT u FROM User u WHERE UserId = ?", "KalLar01").First;
                Console.WriteLine(Db.SQL("SELECT u FROM User u WHERE UserId = ?", "KalLar01").GetEnumerator().ToString());
            });
        }
        static void CreateTheIndexSync()
        {
            Db.SlowSQL("CREATE INDEX userPK ON accounttest.User(UserId)");
            indexCreated = true;
            Console.WriteLine("Index created");
        }
        static void DropTheIndex()
        {
            Db.SlowSQL("DROP INDEX userPk ON  accounttest.User");
            Console.WriteLine("Index dropped");
        }
        #endregion

        static void CreateDropIndexParallelTest()
        {
            Thread queryThread = new Thread(QueryTheIndexSync);
            queryThread.Start();
            while (!queryStarted) ;
            Thread createThread = new Thread(CreateTheIndexSync);
            createThread.Start();
            QueryTheIndex();
            DropTheIndex();
            QueryTheIndex();
            // run cached version first
            // run uncached version
        }

#endif
    }
}
