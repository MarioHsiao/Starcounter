using System;
using Starcounter;
using Starcounter.Binding;

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
                if (Db.SQL("select u from Accounttest.AUser u").First == null)
                {
                    AccountTest.AUser user = new AccountTest.AUser
                    {
                        FirstName = "Kalle",
                        LastName = "Larsson",
                        UserId = "KalLar01"
                    };
                    new AccountTest.Account { AccountId = 0, Amount = 10, Client = user };
                    new AccountTest.Account { AccountId = 1, Amount = 20, Client = user };
                    user = new AccountTest.AUser
                    {
                        FirstName = "Oleg",
                        LastName = "Popov",
                        UserId = "OlePop02"
                    };
                    new AccountTest.Account { AccountId = 2, Amount = 15, Client = user };
                    new AccountTest.Account { AccountId = 3, Amount = 25, Client = user };
                    new AccountTest.Account { AccountId = 4, Amount = 15, Client = user };
                    populated = true;
                }
            });
            return populated;
        }
        #endregion

        #region Printing
        static int PrintAllObjects()
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (AccountTest.AUser u in Db.SQL("select u from Accounttest.AUser u"))
                {
                    Console.WriteLine("User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId);
                    nrPrintedObjs++;
                }
            });
            Db.Transaction(delegate
            {
                foreach (AccountTest.Account a in Db.SQL("select a from Accounttest.Account a"))
                {
                    Console.WriteLine("Account " + a.AccountId + " with amount " + a.Amount + " of user " + a.Client.UserId);
                    nrPrintedObjs++;
                }
            });
            return nrPrintedObjs;
        }

        static int PrintUserByLastName(String LastName)
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (AccountTest.AUser u in Db.SQL("select u from AUser u where LastName = ?", LastName))
                {
                    Console.WriteLine("User " + u.FirstName + " " + u.LastName + " with ID " + u.UserId);
                    nrPrintedObjs++;
                }
            });
            return nrPrintedObjs;
        }

        static int PrintUsersOrderByLastName()
        {
            int nrPrintedObjs = 0;
            Db.Transaction(delegate
            {
                foreach (AccountTest.AUser u in Db.SQL("select u from AUser u order by LastName"))
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
}
