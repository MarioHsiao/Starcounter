using System;
using System.Diagnostics;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Starcounter.Query.SQL;

namespace Starcounter.Query {
    internal static class QueryPreparation {
        /// <summary>
        /// Prepares query by first parsing it and then optimizing. The preparation can be done in Prolog or Bison.
        /// By default the preparation is done in both and if debug the results are compares before and after optimization.
        /// LIKE case requires special treatment.
        /// </summary>
        /// <param name="query">Input query string to prepare</param>
        /// <returns>The result enumerated with the execution plan.</returns>
        internal static IExecutionEnumerator PrepareQuery(String query) {
            Scheduler vproc = Scheduler.GetInstance();
            Starcounter.Query.RawParserAnalyzer.MapParserTree newAnalyzer = null;
            OptimizerInput optArgsProlog = null;
            IExecutionEnumerator prologParsedQueryPlan = null;
            Exception prologException = null;
            // Call to Bison parser and type checker
            // Call to Prolog parser and type checker
#if !BISON_ONLY
            se.sics.prologbeans.QueryAnswer answer = null;
            try {
                answer = PrologManager.CallProlog(vproc, query);
            } catch (SqlException e) {
                prologException = e;
            }
            if (prologException == null)
                optArgsProlog = PrologManager.ProcessPrologAnswer(answer, query);
            // Call Prolog and get answer
            // Transfer answer terms into pre-optimized structures
#endif
#if !PROLOG_ONLY
            try {
#if HELLOTEST
                Starcounter.Query.RawParserAnalyzer.ParserAnalyzerHelloTest newAnalyzer = new Starcounter.Query.RawParserAnalyzer.ParserAnalyzerHelloTest();
                newAnalyzer.ParseAndAnalyzeQuery(query);
#else
                newAnalyzer = new Starcounter.Query.RawParserAnalyzer.MapParserTree();
                Starcounter.Query.RawParserAnalyzer.ParserTreeWalker treeWalker = new Starcounter.Query.RawParserAnalyzer.ParserTreeWalker();
                treeWalker.ParseQueryAndWalkTree(query, newAnalyzer);
#endif
            } catch (Starcounter.Query.RawParserAnalyzer.SQLParserAssertException) {
                newAnalyzer = null;
            } catch (SqlException) {
                Debug.Assert(prologException != null);
                //throw e;
                throw prologException;
            }
            if (prologException != null)
                throw prologException;
#endif
            // Check equality
#if DEBUG
            if (optArgsProlog != null && newAnalyzer != null) {
                Debug.Assert(newAnalyzer.JoinTree.AssertEquals(optArgsProlog.NodeTree), "Join trees are not the same!");
                Debug.Assert(newAnalyzer.WhereCondition.AssertEquals(optArgsProlog.ConditionDict), "Logical conditions are not the same!");
                if (newAnalyzer.FetchNumExpr == null)
                    Debug.Assert(optArgsProlog.FetchNumExpr == null, "Fetch limit expression is expected to be null.");
                else
                    Debug.Assert(newAnalyzer.FetchNumExpr.AssertEquals(optArgsProlog.FetchNumExpr), "Fetch limit expression is not the same!");
                if (newAnalyzer.FethcOffsetExpr == null)
                    Debug.Assert(optArgsProlog.FetchOffsetExpr == null, "Fetch offset expression is expected to be null.");
                else
                    Debug.Assert(newAnalyzer.FethcOffsetExpr.AssertEquals(optArgsProlog.FetchOffsetExpr), "Fetch limit expression is not the same!");
                if (newAnalyzer.FetchOffsetKeyExpr == null)
                    Debug.Assert(optArgsProlog.FetchOffsetKeyExpr == null, "Fetch offset key expression is expected to be null.");
                else
                    Debug.Assert(newAnalyzer.FetchOffsetKeyExpr.AssertEquals(optArgsProlog.FetchOffsetKeyExpr), "Fetch offset key expression is not the same");
                Debug.Assert(newAnalyzer.HintSpec.AssertEquals(optArgsProlog.HintSpec), "Hint expressions are not the same");
            }
#endif
            // Call to optimizer of Prolog result
            if (optArgsProlog != null)
                prologParsedQueryPlan = Optimizer.Optimize(optArgsProlog);

            // Call to optimizer of Bison result
            if (newAnalyzer != null) {
                newAnalyzer.Optimize();
                LogSources.Sql.LogNotice("Using Bison-based parser");
                Console.WriteLine("Using Bison-based parser");
            }

            // Check equality
#if DEBUG
            if (prologParsedQueryPlan != null && newAnalyzer != null) {
                String prologParsedQueryPlanStr = prologParsedQueryPlan.ToString();
                String bisonParsedQueryPlanStr = newAnalyzer.OptimizedPlan.ToString();
                Debug.Assert(bisonParsedQueryPlanStr == prologParsedQueryPlanStr, "Strings of executions plans should be equally");
                //Debug.Assert(newAnalyzer.CompareTo(prologParsedQueryPlan),"Query plans produces by Prolog-based and Bison-based optimizers should be the same.");
            }
#endif
            // Choose Bison based execution plan if available
            IExecutionEnumerator newEnum = newAnalyzer != null ? newAnalyzer.OptimizedPlan : prologParsedQueryPlan;

            // Checking if its LikeExecEnumerator.
            if (newEnum is LikeExecEnumerator) {
                (newEnum as LikeExecEnumerator).CreateLikeCombinations();
            }

            return newEnum;
        }
    }
}
