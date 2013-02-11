using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class FetchTest {
        public static void RunFetchTest() {
            // Do fetch without offset
            FetchAccounts(4);
            // Do fetch with offset
            FetchAccounts(8, 5);
            // Test offset with comparison
            OffsetWithCondition(100, 20);
            // Offset with path expression
            Db.Transaction(delegate {
                foreach (User u in Db.SQL<User>("select client from account offset ?", 10)) {
                    Trace.Assert(u.UserId == DataPopulation.FakeUserId(10/3));
                    break;
                }
            });
        }

        internal static void FetchAccounts(int fetchnr) {
            int id = 0;
            Db.Transaction(delegate {
                foreach (Account a in Db.SQL<Account>("select a from account a fetch ?", fetchnr)) {
                    Trace.Assert(a.AccountId == id);
                    id++;
                }
            });
            Trace.Assert(id == fetchnr);
        }

        internal static void FetchAccounts(int fetchnr, int fetchoff) {
            int id = fetchoff;
            Db.Transaction(delegate {
                foreach (Account a in Db.SQL<Account>("select a from account a fetch ? offset ?", fetchnr, fetchoff)) {
                    Trace.Assert(a.AccountId == id);
                    id++;
                }
            });
            Trace.Assert(id == fetchoff + fetchnr);
        }

        internal static void OffsetWithCondition(int smallestId, int fetchoff) {
            Db.Transaction(delegate {
                foreach (Account a in Db.SQL<Account>("select a from account a where accountid >= ? offset ?", smallestId, fetchoff)) {
                    Trace.Assert(a.AccountId == smallestId+fetchoff);
                    break;
                }
            });
        }
    }
}
