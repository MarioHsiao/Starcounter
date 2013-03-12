using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.RawParserAnalyzer;
using Starcounter.Query.Sql;
using Starcounter.Query.SQL;

namespace QueryProcessingTest {
    public static class QueryProcessingPerformance {
        public static void MeasurePrepareQuery() {
            int nrIterations = 100;
            String query = "select a from Account a, User u where a.Client = u and u.FirstName = ? fetch ? offset ?";
            Stopwatch timer = new Stopwatch();
            timer.Start();
            try {
                Starcounter.Query.QueryPreparation.PrepareQuery(query);
            } catch (Exception e) {
                if ((uint)e.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRSQLINTERNALERROR)
                    throw e;
            }
            timer.Stop();
            Console.WriteLine("First call to prepare query took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs.");
            timer.Reset();
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                try {
                    Starcounter.Query.QueryPreparation.PrepareQuery(query);
                } catch (Exception e) { 
                    if ((uint)e.Data[ErrorCode.EC_TRANSPORT_KEY] != Error.SCERRSQLINTERNALERROR)
                        throw e;
                }
            timer.Stop();
            Console.WriteLine("Preparing query took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs.");

            timer.Reset();
            se.sics.prologbeans.QueryAnswer answer = null;
            timer.Start();
            for (int i = 0; i <nrIterations; i++)
                answer = PrologManager.CallProlog(query);
            timer.Stop();
            Console.WriteLine("Call Prolog (parse and type check) took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs.");
            timer.Reset();
            OptimizerInput optArgsProlog = null;
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                optArgsProlog = PrologManager.ProcessPrologAnswer(answer, query);
            timer.Stop();
            Console.WriteLine("Map Prolog answer to optimizer input took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs.");
            timer.Reset();
            IExecutionEnumerator prologParsedQueryPlan = null;
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                prologParsedQueryPlan = Optimizer.Optimize(optArgsProlog);
            timer.Stop();
            Console.WriteLine("Optimizing the query tree took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs.");

            timer.Reset();
            ParserTreeWalker treeWalker = null;
            timer.Start();
            for (int i = 0; i < nrIterations; i++) {
                treeWalker = new ParserTreeWalker();
                treeWalker.ParseQuery(query);
            }
            timer.Stop();
            Console.WriteLine("Parsing query in Bison took " + timer.ElapsedMilliseconds * 1000 / nrIterations + " mcs.");
        }
    }
}
