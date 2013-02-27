using System;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;

namespace Starcounter.Query {
    internal static class QueryPreparation {
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
