using System;
using System.Collections;
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
            // Do fetch with offset on join with two join conditions (one non-equal)
            FetchJoinedAccounts(19, 114);
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

        internal static void FetchJoinedAccounts(int fetchnr, int fetchoff) {
            int rows = fetchoff;
            //PrintQueryPlan("select a1 from account a1, account a2 where a1.accountid >= a2.accountid and a1.amount >= a2.amount and a1.client = a2.client fetch ? offset ?");
            Db.Transaction(delegate {
                foreach (Account a in Db.SQL<Account>("select a1 from account a1, account a2 where a1.accountid >= a2.accountid and a1.amount >= a2.amount and a1.client = a2.client fetch ? offset ?", 
                    fetchnr, fetchoff)) {
                    Trace.Assert(a.Client.UserId == DataPopulation.FakeUserId(rows / 6));
                    rows++;
                }
            });
            Trace.Assert(rows == fetchoff + fetchnr);
        }

        internal static void FetchJoinedUsers(int fetchnr, int fetchoff) {
            int rows = fetchoff;
            Db.Transaction(delegate {
                foreach (User u in Db.SQL<User>("select a1.client from account a1, account a2 where a1.client = a2.client and a1.amount > ? and a1.amount >= a2.amount + ? order by a1.client fetch ? offset ?",
                    0, 100, fetchnr, fetchoff)) {
                    Trace.Assert(u.UserId == DataPopulation.FakeUserId(rows / 2));
                    rows++;
                }
            });
            Trace.Assert(rows == fetchoff + fetchnr);
        }

        internal static void PrintQueryPlan(String query) {
            Console.WriteLine(((IEnumerator)Db.SQL(query, null).GetEnumerator()).ToString());
        }
    }
}
