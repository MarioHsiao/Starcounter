using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Binding;
using Starcounter.TestFramework;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {
#if ACCOUNTTEST_MODEL
        #region Populate
        static bool Populate()
        {
            bool populated = false;
            Db.Transaction(delegate
            {
                if (Db.SQL("select u from Accounttest.User u").First == null)
                {
                    accounttest.User user = new accounttest.User
                    {
                        FirstName = "Kalle",
                        LastName = "Larsson",
                        UserId = "KalLar01"
                    };
                    new accounttest.account { AccountId = 0, Amount = 10, Client = user };
                    new accounttest.account { AccountId = 1, Amount = 20, Client = user };
                    user = new accounttest.User
                    {
                        FirstName = "Oleg",
                        LastName = "Popov",
                        UserId = "OlePop02"
                    };
                    new accounttest.account { AccountId = 2, Amount = 15, Client = user };
                    new accounttest.account { AccountId = 3, Amount = 25, Client = user };
                    new accounttest.account { AccountId = 4, Amount = 15, Client = user };
                    populated = true;
                }
            });
            return populated;
        }
        #endregion

        #region Printing
        static int CountAllObjects()
        {
            int nrPrintedObjs = 0;
            String line = "";
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from User u"))
                {
                    line = "User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId;
                    nrPrintedObjs++;
                }
            });
            Trace.Assert(nrPrintedObjs == 2);
            Db.Transaction(delegate
            {
                foreach (accounttest.account a in Db.SQL("select a from Account a"))
                {
                    line = "Account " + a.AccountId + " with amount " + a.Amount + " of user " + a.Client.UserId;
                    nrPrintedObjs++;
                }
            });
            Trace.Assert(nrPrintedObjs == 2 + 5);
            return nrPrintedObjs;
        }
        
        static int PrintAllObjects()
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from User u"))
                {
                    Console.WriteLine("User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId);
                    nrPrintedObjs++;
                }
            });
            Db.Transaction(delegate
            {
                foreach (accounttest.account a in Db.SQL("select a from Account a"))
                {
                    Console.WriteLine("Account " + a.AccountId + " with amount " + a.Amount + " of user " + a.Client.UserId);
                    nrPrintedObjs++;
                }
            });
            return nrPrintedObjs;
        }

        static int CountUserByLastName(String LastName)
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from User u where LastName = ?", LastName))
                {
                    String line = "User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId;
                    nrPrintedObjs++;
                }
            });
            Trace.Assert(nrPrintedObjs == 1);
            return nrPrintedObjs;
        }
        
        static int PrintUserByLastName(String LastName)
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from User u where LastName = ?", LastName))
                {
                    Console.WriteLine("User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId);
                    nrPrintedObjs++;
                }
            });
            return nrPrintedObjs;
        }

        static int CountUsersOrderByLastName() {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate {
                foreach (accounttest.User u in Db.SQL("select u from User u order by LastName")) {
                    Trace.Assert("User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId != "User  with ID ");
                    nrPrintedObjs++;
                }
            });
            Trace.Assert(nrPrintedObjs == 2);
            return nrPrintedObjs;
        }
        static int PrintUsersOrderByLastName()
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (accounttest.User u in Db.SQL("select u from User u order by LastName"))
                {
                    Console.WriteLine("User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId);
                    nrPrintedObjs++;
                }
            });
            return nrPrintedObjs;
        }
        #endregion
#endif
    }

    public static class HelpMethods {
        internal static TestLogger logger = new TestLogger("IndexQueryTest", false);

        internal static void LogEvent(String eventString) {
            logger.Log(eventString);
        }
    }
}
