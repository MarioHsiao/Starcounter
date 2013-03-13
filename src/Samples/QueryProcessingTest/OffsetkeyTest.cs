using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class OffsetkeyTest {
        delegate SqlResult<dynamic> CallQuery(String addition, dynamic arg);
        static List<CallQuery> queries = new List<CallQuery>();
        //static SqlResult<dynamic> sqlResult;

        public static void Master() {
            AddQueries();
            ErrorCases();
            SimpleTestsAllQueries();
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
        }

        static void AddQueries() {
            queries.Add((addition, arg) => Db.SQL("select u from user u"+addition, arg));
            queries.Add((addition, arg) => Db.SQL("select u from user u where userid < ?" + addition, 5, arg));
        }

        static void ErrorCases() {
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
            if (e.Current is User)
                Console.WriteLine((e.Current as User).UserId);
        }

        static void SimpleTestsAllQueries() {
            string f = "fetch ?";
            dynamic n = 4;
            foreach (CallQuery q in queries)
                Db.Transaction(delegate {
                    using (IRowEnumerator<dynamic> e = q(f, n).GetEnumerator()) {
                        e.MoveNext();
                        Trace.Assert(e.Current is User);
                        Trace.Assert((e.Current as User).UserIdNr == 0);
                        n = e.GetOffsetKey();
                    }
                    f = "offsetkey ?";
                    using (IRowEnumerator<dynamic> e = queries[1](f, n).GetEnumerator()) {
                        e.MoveNext();
                        Trace.Assert(e.Current is User);
                        Trace.Assert((e.Current as User).UserIdNr == 4);
                    }
                });
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
     * 
     * IV. Transactions
     * IV.1. No transaction scope
     * IV.2. One transaction scope insert inside/outside
     * IV.3. One transaction scope with snapshot isolation inside/outside
     * IV.4. Two separate transaction scopes
     */
}
