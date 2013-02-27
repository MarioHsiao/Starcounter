using System;
using Starcounter.Query.Execution;
using Starcounter.Query.Sql;

namespace Starcounter.Query {
    internal static class QueryPreparation {
        internal static IExecutionEnumerator PrepareQuery(String query) {
            Scheduler vproc = Scheduler.GetInstance();
            return PrologManager.ProcessSqlQuery(vproc, query);
        }
    }
}
