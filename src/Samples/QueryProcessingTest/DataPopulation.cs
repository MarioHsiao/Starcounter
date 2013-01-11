using System;
using Starcounter;

namespace QueryProcessingTest {
    public class DataPopulation {
        public static string FakeUserId(int n) { return syllabels[n & 7] + syllabels[(n >> 3) & 7] + (n >> 6).ToString(); }
        static string[] syllabels = new string[8] { "ka", "ti", "mo", "le", "pu", "va", "ro", "se" };

        public static void PopulateAccounts(Int64 nrUsers, Int64 nrAccountPerUser) {
            DeleteAccounts();
            Db.Transaction(delegate {
                for (int i = 0; i < nrUsers; i++) {
                    User newUser = new User { UserId = FakeUserId(i)};
                    for (int j = 0; j < nrAccountPerUser; j++)
                        new Account { AccountId = i * nrAccountPerUser + j, Amount = 100.0m, Client = newUser };
                }
            });
        }

        public static void DeleteAccounts() {
            Db.SlowSQL("DELETE FROM Account");
            Db.SlowSQL("DELETE FROM QueryProcessingTest.User");
        }
    }
}
