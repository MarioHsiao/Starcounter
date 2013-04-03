﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class OffsetkeyTest {
        public static void Master() {
            HelpMethods.LogEvent("Test offset key");
            ErrorCases();
            // Simple query
            TestDataModification("select a from account a where accountid > ?");
            Db.Transaction(delegate {
                TestDataModification("select a from account a where accountid > ?");
            });
            // Join with index scan only
            TestDataModification("select a from account a1, Account a2 where a1.accountid > ? and a1.Client = a2.client and a1.Amount = a2.Amount");
            // Join with index scan and full table scan
            // Join with proceedings
            // Reference look up
            // Multiple joins
            // With operators
#if false
            foreach query
                foreach interator
                    foreach data_update
                        Transaction1
                        Transaction2
                        Transaction3
#endif
            // Call the query with fetch
            // Iterate and get offset key
            // Modify data
            // If offset key is not null, query with offset key
            // Iterate over it
            HelpMethods.LogEvent("Finished testing offset key");
        }

        static void ErrorCases() {
            // Test getting offset key outside enumerator
            string f = "fetch ?";
            dynamic n = 4;
            String query = "select u from user u ";
            IRowEnumerator<dynamic> e = Db.SQL(query + f, n).GetEnumerator();
            byte[] k = e.GetOffsetKey();
            Trace.Assert(k == null);
            e.Dispose();
            e = Db.SQL(query + f, n).GetEnumerator();
            while (e.MoveNext())
                Trace.Assert(e.Current is User);
            k = e.GetOffsetKey();
            Trace.Assert(k == null);
            e.Dispose();
            // Test correct example
            f = "fetch ?";
            n = 4;
            e = Db.SQL(query + f, n).GetEnumerator();
            e.MoveNext();
            k = e.GetOffsetKey();
            e.Dispose();
            f = "offsetkey ?";
            n = k;
            e = Db.SQL(query + f, n).GetEnumerator();
            e.MoveNext();
            Trace.Assert(e.Current is User);
            e.Dispose();
            // Test offsetkey on the query with the offset key from another query
            Boolean isException = false;
            e = Db.SQL("select u from user u fetch ?", 4).GetEnumerator();
            e.MoveNext();
            Trace.Assert(e.Current is User);
            k = e.GetOffsetKey();
            e.Dispose();
            try {
                e = Db.SQL("select u from user u where useridnr < ? offsetkey ?", 5, k).GetEnumerator();
                e.MoveNext();
                Trace.Assert(e.Current is User);
            } catch (InvalidOperationException) {
                isException = true;
            } finally {
                e.Dispose();
            }
            Trace.Assert(isException);

#if false
            isException = false;
            e = Db.SQL("select u from user u where useridnr < ? fetch ?", 5, 4).GetEnumerator();
            e.MoveNext();
            Trace.Assert(e.Current is User);
            k = e.GetOffsetKey();
            e.Dispose();
            try {
                e = Db.SQL("select u from user u offsetkey ?", k).GetEnumerator();
                e.MoveNext();
                Trace.Assert(e.Current is User);
            } catch (InvalidOperationException) {
                isException = true;
            } finally {
                e.Dispose();
            };
            Trace.Assert(isException);

            isException = false;
            e = Db.SQL("select u from user u fetch ?", 4).GetEnumerator();
            while (e.MoveNext())
                Trace.Assert(e.Current is User);
            k = e.GetOffsetKey();
            e.Dispose();
            Trace.Assert(k != null);
            try {
                e = Db.SQL("select u from user u where useridnr < ? offsetkey ?", 3, k).GetEnumerator();
                e.MoveNext();
            } catch (InvalidOperationException) {
                isException = true;
            } finally {
                e.Dispose();
            }
            Trace.Assert(isException);
#endif
        }

        static void TestDataModification(String query) {
            // Populate data
            User client = PopulateForTest();
            // Do set of tests
            byte[] key;
            // Do nothing
            key = DoFetch(query);
            DoOffsetkey(query, key, new int[] { GetAccountId(3), GetAccountId(4), GetAccountId(5) }); // offsetkey does not move forward
            // Drop inside fetched
            key = DoFetch(query);
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(1)).First.Delete();
            });
            DoOffsetkey(query, key, new int[] {GetAccountId(3), GetAccountId(4), GetAccountId(5)}); // offsetkey does not move forward
            InsertAccount(GetAccountId(1), client);
            // Insert inside fetched
            key = DoFetch(query);
            InsertAccount(GetAccountId(1) + 1, client);
            DoOffsetkey(query, key, new int[] { GetAccountId(3), GetAccountId(4), GetAccountId(5) }); // offsetkey does not move forward
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(1) + 1).First.Delete();
            });
            // Insert later after offsetkey
            key = DoFetch(query);
            InsertAccount(GetAccountId(3) + 1, client);
            DoOffsetkey(query, key, new int[] { GetAccountId(3), GetAccountId(3) + 1, GetAccountId(4), GetAccountId(5) });
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(3) + 1).First.Delete();
            });
            // Insert after offsetkey
            key = DoFetch(query);
            InsertAccount(GetAccountId(2) + 1, client);
            DoOffsetkey(query, key, new int[] { GetAccountId(2) + 1, GetAccountId(3), GetAccountId(4), GetAccountId(5) });
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(2) + 1).First.Delete();
            });
            // Delete after offsetkey
            key = DoFetch(query);
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(3)).First.Delete();
            });
            DoOffsetkey(query, key, new int[] { GetAccountId(4), GetAccountId(5) });
            InsertAccount(GetAccountId(3), client);
            // Delete the offset key
            key = DoFetch(query);
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(2)).First.Delete();
            });
            DoOffsetkey(query, key, new int[] { GetAccountId(3), GetAccountId(4), GetAccountId(5) });
            InsertAccount(GetAccountId(2), client);
            // Delete and insert the offset key
            key = DoFetch(query);
            Db.Transaction(delegate {
                Db.SQL<Account>("select a from account a where accountid = ?", GetAccountId(2)).First.Delete();
            });
            InsertAccount(GetAccountId(2), client);
            DoOffsetkey(query, key, new int[] { GetAccountId(2), GetAccountId(3), GetAccountId(4), GetAccountId(5) });
            // Drop data
            DropAfterTest();
        }

        static byte[] DoFetch(String query) {
            int nrs = 0;
            byte[] key = null;
            Db.Transaction(delegate {
                using (IRowEnumerator<Account> res = Db.SQL<Account>(query + " fetch ?", AccountIdLast, 3).GetEnumerator())
                    while (res.MoveNext()) {
                        Trace.Assert(res.Current.AccountId == GetAccountId(nrs));
                        nrs++;
                        if (nrs == 3)
                            key = res.GetOffsetKey();
                    }
            });
            Trace.Assert(key != null);
            Trace.Assert(nrs == 3);
            return key;
        }

        static void DoOffsetkey(String query, byte[] key, int[] expectedResult) {
            int nrs = 0;
            Db.Transaction(delegate {
            foreach (Account a in Db.SQL<Account>(query + " fetch ? offsetkey ?", AccountIdLast, expectedResult.Length, key)) {
                Trace.Assert(a.AccountId == expectedResult[nrs]);
                nrs++;
            }
            });
            Trace.Assert(nrs == expectedResult.Length);
        }

        static User PopulateForTest() {
            User client = null;
            Db.Transaction(delegate {
                Trace.Assert(Db.SQL<Account>("select a from account a order by accountid desc").First.AccountId == AccountIdLast);
                Trace.Assert(Db.SlowSQL<long>("select count(u) from user u").First == 10000);
                client = new User {
                    UserIdNr = 10005,
                    BirthDay = new DateTime(1983, 03, 23),
                    FirstName = "Test",
                    LastName = "User",
                    UserId = DataPopulation.FakeUserId(10005)
                };
                for (int i = 0; i < 6; i++)
                    InsertAccount(GetAccountId(i), client);
            });
            return client;
        }

        static void DropAfterTest() {
            User client = null;
            Db.Transaction(delegate {
                int nrs = 0;
                foreach (Account a in Db.SQL<Account>("select a from account a where accountid > ?", AccountIdLast)) {
                    client = a.Client;
                    a.Delete();
                    nrs++;
                }
                client.Delete();
                Trace.Assert(nrs == 6);
                Trace.Assert(Db.SQL<Account>("select a from account a order by accountid desc").First.AccountId == AccountIdLast);
                Trace.Assert(Db.SlowSQL<long>("select count(u) from user u").First == 10000);
            });
        }

        static int GetAccountId(int i) {
            return 10000 * 3 + (i + 1) * 3;
        }
        static int AccountIdLast = 10000 * 3 - 1;

        static Account InsertAccount(int accountId, User client) {
            Account a = null;
            Db.Transaction(delegate {
                a = new Account { AccountId = accountId, Amount = 100.0m - (accountId - AccountIdLast) * 3, Client = client, Updated = DateTime.Now };
            });
            return a;
        }
    }
    /*
     * I. Queries (index, non-index, codegen)
     * I.1. Simple select
     * I.2. With where clause
     * I.3. With arithmetic expression
     * I.4. With path expression
     * I.5. With equi-join
     * I.6. With join
     * I.7. With multiple join
     * I.8. With outer join
     * I.9. With fetch
     * 
     * II. Iterations and offset key fetching
     * II.1. Fetch inside and iterate to the end
     * II.2. Fetch inside and iterate to the middle
     * II.4. Fetch to the last
     * II.5. Fetch outside
     * 
     * III. Data
     * III.1. No updates
     * III.2/3. Insert/Delete later
     * III.4/5. Insert/Delete before
     * III.6. Delete offset key
     * III.7. Delete the next
     * III.8. Insert the next after the offset key
     * III.9. Insert next after the next
     * III.10. Delete and insert the offset key
     * III.11. Delete and insert the next
     * III.12. Delete the offset key and insert next
     * III.13. Non-unique inserts (with deletes) around the offset key
     * 
     * IV. Transactions
     * IV.1. No transaction scope
     * IV.2. One transaction scope insert inside/outside
     * IV.3. One transaction scope with snapshot isolation inside/outside
     * IV.4. Two separate transaction scopes
     */
}
