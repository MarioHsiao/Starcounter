using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryProcessingTest {
    public static class QueryProcessingPerformance {
        public static void MeasurePrepareQuery() {
            int nrIterations = 10000;
            String query = "select a from Account a, User u where a.Client = u and u.FirstName = ? fetch ? offset ?";
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                Starcounter.Query.QueryPreparation.PrepareQuery(query);
            timer.Stop();
            Console.WriteLine("It took " + timer.ElapsedMilliseconds / nrIterations + " ms to prepare the query.");
        }
    }
}
