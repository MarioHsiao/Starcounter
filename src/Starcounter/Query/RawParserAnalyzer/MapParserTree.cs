using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.SQL;

namespace Starcounter.Query.RawParserAnalyzer {
    /// <summary>
    /// Maps raw parsed tree, produced by bison-based parser, to the structures used by current optimizer, 
    /// i.e., structures used for prolog-based parser.
    /// </summary>
    internal class MapParserTree : IParserTreeAnalyzer {

        /// <summary>
        /// Contains joins of relations. It is constructed from all clauses of select statement.
        /// It consists of relations mentioned in FROM clause and path expressions - in all clauses.
        /// </summary>
        internal IOptimizationNode JoinTree { get; private set; }

        /// <summary>
        /// Contains logical condition of the query. Cannot be null for optimizer. At least TRUE.
        /// </summary>
        internal ConditionDictionary WhereCondition { get; private set; }

        internal INumericalExpression FetchNumExpr { get; private set; }
        internal INumericalExpression FethcOffsetExpr { get; private set; }
        internal IBinaryExpression FetchOffsetKeyExpr { get; private set; }
        internal HintSpecification HintSpec { get; private set; }
        internal VariableArray VarArray { get; private set; }

        /// <summary>
        /// The execution plan produced by optimizer from the given parsed and analyzed tree.
        /// </summary>
        internal IExecutionEnumerator OptimizedPlan { get; private set; }

        /// <summary>
        /// Calls original optimizer on results of analyzer.
        /// </summary>
        internal void Optimize() {
            Debug.Assert(JoinTree != null && WhereCondition != null && HintSpec != null, "Query should parsed and analyzed before optimization");
            OptimizedPlan = Optimizer.Optimize(new OptimizerInput(JoinTree, WhereCondition, FetchNumExpr, FethcOffsetExpr, FetchOffsetKeyExpr, HintSpec));
            ((ExecutionEnumerator)OptimizedPlan).IsBisonParserUsed = true;
        }
        
        /// <summary>
        /// Compares this optimized plan with given optimized plan (e.g., produced by original parser and optimizer)
        /// </summary>
        /// <param name="otherOptimizedPlan">Other optimized plan</param>
        /// <returns></returns>
        internal bool CompareTo(IExecutionEnumerator otherOptimizedPlan) {
            String thisOptimizedPlanStr = Regex.Replace(this.OptimizedPlan.ToString(), "\\s", "");
            String otherOptimizedPlanStr = Regex.Replace(otherOptimizedPlan.ToString(), "\\s", "");
            return thisOptimizedPlanStr.Equals(otherOptimizedPlanStr);
        }
    }
}
