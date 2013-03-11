using System;
using System.Collections.Generic;
using Starcounter;

namespace QueryProcessingTest {
    public static class OffsetkeyTest {
        //delegate SqlResult<dynamic> callQuery(
        static List<String> queries = new List<String>();
        //static SqlResult<dynamic> sqlResult;

        public static void Master() {
            AddQueries();
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
            IEnumerator<String> q = queries.GetEnumerator();
            String str = q.Current;
            string f = "fetch ?";
            dynamic n = 4;
            String query = "select u from user u ";
            IRowEnumerator<dynamic> e = Db.SQL(query + f, n).GetEnumerator();
            e.MoveNext();
            byte[] k = e.GetOffsetKey();
            f = "offsetkey ?";
            n = k;
            e = Db.SQL(query + f, n).GetEnumerator();
            e.MoveNext();
            //if (e.Current is User)
            //    Console.WriteLine((e.Current as User).UserId);
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
     * II.3. Fetch inside and don't iterate
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
