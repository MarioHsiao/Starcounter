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
#endif
            // Check equality
#if !BISON_ONLY && !PROLOG_ONLY
#endif
            // Call to Bison optimizer
#if !PROLOG_ONLY
#endif
            // Call to Prolog optimizer
#if !BISON_ONLY
#endif
            // Return Bison based execution plan if available
            return PrologManager.ProcessSqlQuery(vproc, query);
        }
    }
}
