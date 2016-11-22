using System;
using Starcounter;
using System.Collections.Generic;

namespace QueryProcessingTest {
    public static class DataPopulation {
        public static string FakeUserId(int n) { return syllabels[n & 7] + syllabels[(n >> 3) & 7] + (n >> 6).ToString(); }
        static string[] syllabels = new string[8] { "ka", "ti", "mo", "le", "pu", "va", "ro", "se" };
        internal readonly static int OldestBirthYear = 1950;
        internal readonly static int YoungestBirthYear = 1985;
        internal readonly static int CurrentYear = 2013;
        internal readonly static DateTime CurrentDate = new DateTime(2013, 1, 12);

        public static int OldestAge { get { return CurrentYear - DataPopulation.OldestBirthYear; } }
        public static int YoungestAge { get { return CurrentYear - DataPopulation.YoungestBirthYear; } }

        public static string DAILYACCOUNT = "Daily";
        public static string SAVINGACCOUNT = "Saving";

        public static void PopulateAccounts(Int64 nrUsers, Int64 nrAccountPerUser) {
            DeleteAccounts();
            Random rnd = new Random(1);
            Db.Transact(delegate {
                for (int i = 0; i < nrUsers; i++) {
                    User newUser = new User {
                        UserId = FakeUserId(i),
                        UserIdNr = i,
                        FirstName = "Fn" + i,
                        LastName = "Ln" + i,
                        NickName = "Nk" + i,
                        BirthDay = new DateTime(rnd.Next(OldestBirthYear, YoungestBirthYear), rnd.Next(1, 12), rnd.Next(1, 28))
                    };
                    for (int j = 0; j < nrAccountPerUser; j++)
                        new Account {
                            AccountId = i * nrAccountPerUser + j,
                            Amount = 100.0m * j,
                            Client = newUser,
                            When = DateTime.Now,
                            AccountType = j < 3 ? DAILYACCOUNT : SAVINGACCOUNT,
                            NotActive = false
                        };
                }
            });
        }

        public static void PopulateUsers(Int64 nrUsers, Int64 nrAccountPerUser) {
            DeleteAccounts();
            Random rnd = new Random(1);
            var tl = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < nrUsers; i++)
            {
                tl.Add(Db.TransactAsync(delegate
                {
                    User newUser = new User
                    {
                        UserId = FakeUserId(i),
                        UserIdNr = i,
                        FirstName = "Fn" + i,
                        LastName = "Ln" + i,
                        NickName = "Nk" + i,
                        BirthDay = new DateTime(rnd.Next(1950, 1985), rnd.Next(1, 12), rnd.Next(1, 28))
                    };
                    for (int j = 0; j < nrAccountPerUser; j++)
                        new Account
                        {
                            AccountId = i * nrAccountPerUser + j,
                            Amount = 100.0m * j,
                            Client = newUser,
                            When = DateTime.Now,
                            AccountType = j < 3 ? DAILYACCOUNT : SAVINGACCOUNT,
                            NotActive = false
                        };
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tl.ToArray());
        }

        public static void DeleteAccounts() {
            Db.Transact(delegate {
                Db.SlowSQL("DELETE FROM Account where Accountid >= 0");
                Db.SlowSQL("DELETE FROM QueryProcessingTest.User");
            });
        }
    }
}
