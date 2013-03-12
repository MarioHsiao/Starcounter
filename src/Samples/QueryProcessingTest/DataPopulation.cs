using System;
using Starcounter;

namespace QueryProcessingTest {
    public static class DataPopulation {
        public static string FakeUserId(int n) { return syllabels[n & 7] + syllabels[(n >> 3) & 7] + (n >> 6).ToString(); }
        static string[] syllabels = new string[8] { "ka", "ti", "mo", "le", "pu", "va", "ro", "se" };
        internal readonly static int OldestBirthYear = 1950;
        internal readonly static int YoungestBirthYear = 1985;

        public static int OldestAge { get { return DateTime.Now.Year - DataPopulation.OldestBirthYear; } }
        public static int YoungestAge { get { return DateTime.Now.Year - DataPopulation.YoungestBirthYear; } }

        public static void PopulateAccounts(Int64 nrUsers, Int64 nrAccountPerUser) {
            DeleteAccounts();
            Random rnd = new Random();
            Db.Transaction(delegate {
                for (int i = 0; i < nrUsers; i++) {
                    User newUser = new User {
                        UserId = FakeUserId(i),
                        UserIdNr = i,
                        FirstName = "Fn" + i,
                        LastName = "Ln" + i,
                        BirthDay = new DateTime(rnd.Next(OldestBirthYear, YoungestBirthYear), rnd.Next(1, 12), rnd.Next(1, 28))
                    };
                    for (int j = 0; j < nrAccountPerUser; j++)
                        new Account { AccountId = i * nrAccountPerUser + j, Amount = 100.0m * j, Client = newUser, Updated = DateTime.Now };
                }
            });
        }

        public static void PopulateUsers(Int64 nrUsers, Int64 nrAccountPerUser) {
            DeleteAccounts();
            Random rnd = new Random();
            for (int i = 0; i < nrUsers; i++)
                Db.Transaction(delegate {
                    User newUser = new User {
                        UserId = FakeUserId(i),
                        UserIdNr = i,
                        FirstName = "Fn" + i,
                        LastName = "Ln" + i,
                        BirthDay = new DateTime(rnd.Next(1950, 1985), rnd.Next(1, 12), rnd.Next(1, 28))
                    };
                    for (int j = 0; j < nrAccountPerUser; j++)
                        new Account { AccountId = i * nrAccountPerUser + j, Amount = 100.0m * j, Client = newUser, Updated = DateTime.Now };
                });
        }

        public static void DeleteAccounts() {
            Db.Transaction(delegate {
                Db.SlowSQL("DELETE FROM Account");
                Db.SlowSQL("DELETE FROM QueryProcessingTest.User");
            });
        }
    }
}
