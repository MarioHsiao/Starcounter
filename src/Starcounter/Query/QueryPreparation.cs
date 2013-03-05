using System;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;

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
            // Call to Bison parser and type checker
#if !PROLOG_ONLY
#endif
            // Call to Prolog parser and type checker
#if !BISON_ONLY
            IExecutionEnumerator newEnum = PrologManager.ProcessSqlQuery(vproc, query);
            // Call Prolog and get answer
            // Transfer answer terms into pre-optimized structures
#endif
            // Check equality
#if !BISON_ONLY && !PROLOG_ONLY && DEBUG
#endif
            // Call to optimizer of Bison result
#if !PROLOG_ONLY
#endif
            // Call to optimizer of Prolog result
#if !BISON_ONLY
#endif
            // Check equality
#if !BISON_ONLY && !PROLOG_ONLY && DEBUG
#endif
            // Choose Bison based execution plan if available

            // Checking if its LikeExecEnumerator.
            if (newEnum is LikeExecEnumerator) {
                (newEnum as LikeExecEnumerator).CreateLikeCombinations();
            }
            return newEnum;
        }
    }
}
