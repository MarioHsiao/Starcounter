using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;

namespace QueryProcessingTest {
    public static class QueryProcessingPerformance {
        public static void MeasurePrepareQuery() {
            int nrIterations = 100;
            String query = "select a from Account a, User u where a.Client = u and u.FirstName = ? fetch ? offset ?";
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                try {
                    Starcounter.Query.QueryPreparation.PrepareQuery(query);
                } catch (Exception e) { 
                    if ((uint)e.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRSQLINTERNALERROR)
                        throw e;
                }
            timer.Stop();
            Console.WriteLine("It took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs to prepare the query.");
        }
    }
}
