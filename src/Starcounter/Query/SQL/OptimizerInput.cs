using System;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;

namespace Starcounter.Query.SQL {
    internal class OptimizerInput {
        internal IOptimizationNode NodeTree { get; set; }
        internal ConditionDictionary ConditionDict { get; set; }
        internal INumericalExpression FetchNumExpr { get; set; }
        internal INumericalExpression FetchOffsetExpr { get; set; }
        internal IBinaryExpression FetchOffsetKeyExpr { get; set; }
        internal HintSpecification HintSpec { get; set; }

        internal OptimizerInput() { }

        internal OptimizerInput(IOptimizationNode nodeTree, ConditionDictionary conditionDict, INumericalExpression fetchNumExpr,
            INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr, HintSpecification hintSpec) {
            NodeTree = nodeTree;
            ConditionDict = conditionDict;
            FetchNumExpr = fetchNumExpr;
            FetchOffsetExpr = fetchOffsetExpr;
            FetchOffsetKeyExpr = fetchOffsetKeyExpr;
            HintSpec = hintSpec;
        }
    }
}
