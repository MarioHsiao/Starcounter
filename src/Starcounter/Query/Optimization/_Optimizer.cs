// ***********************************************************************
// <copyright file="_Optimizer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Query.SQL;

namespace Starcounter.Query.Optimization
{
internal static class Optimizer
{
    /// <summary>
    /// Creates an execution enumerator corresponding to an optimized query execution plan.
    /// </summary>
    /// <param name="nodeTree">An initial node-tree corresponding to the structure of the query.</param>
    /// <param name="conditionDict">A dictionary of conditions present in the query.</param>
    /// <param name="fetchNumExpr">A specification of maximal number of objects/rows to be returned.</param>
    /// <param name="fetchOffsetExpr">An offset expression.</param>
    /// <param name="fetchOffsetKeyExpr">An offset-key expression.</param>
    /// <param name="hintSpec">A hint specification given by the user on how to execute the query.</param>
    /// <returns>An execution enumerator corresponding to an optimized query execution plan.</returns>
    internal static IExecutionEnumerator Optimize(OptimizerInput args) {
        IOptimizationNode nodeTree = args.NodeTree;
        ConditionDictionary conditionDict = args.ConditionDict;
        INumericalExpression fetchNumExpr = args.FetchNumExpr;
        INumericalExpression fetchOffsetExpr = args.FetchOffsetExpr;
        IBinaryExpression fetchOffsetKeyExpr = args.FetchOffsetKeyExpr;
        HintSpecification hintSpec = args.HintSpec;

        if (nodeTree == null) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect nodeTree.");
        }
        if (conditionDict == null) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect conditionDict.");
        }
        if (hintSpec == null) {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect hintSpec.");
        }
        List<OptimizationTree> optimizationTreeList = CreateAllPermutations(nodeTree, hintSpec);
        OptimizationTree bestOptimizationTree = null;
        for (Int32 i = 0; i < optimizationTreeList.Count; i++) {
            optimizationTreeList[i].DistributeIndexHints(hintSpec.IndexHintList);
            optimizationTreeList[i].DistributeConditions(conditionDict);
            optimizationTreeList[i].EvaluateScanAlternatives();
            optimizationTreeList[i].SortOptimize();
            if (bestOptimizationTree == null || optimizationTreeList[i].EstimatedCost < bestOptimizationTree.EstimatedCost) {
                bestOptimizationTree = optimizationTreeList[i];
            }
            // TODO: Aggregation optimize tree.
        }
        FlagInnermostExtent(bestOptimizationTree);
        // Return the corresponding execution enumerator.
        IExecutionEnumerator createdEnumerator = bestOptimizationTree.CreateExecutionEnumerator(fetchNumExpr, fetchOffsetExpr, fetchOffsetKeyExpr);

        // The special case where query includes "LIKE ?" is handled by special class LikeExecEnumerator.
        if (((createdEnumerator.VarArray.QueryFlags & QueryFlags.IncludesLIKEvariable) != QueryFlags.None) && (createdEnumerator.QueryString[0] != ' '))
            createdEnumerator = new LikeExecEnumerator(createdEnumerator.QueryString, null, null);
        return createdEnumerator;
    }

    internal static List<OptimizationTree> CreateAllPermutations(IOptimizationNode nodeTree, 
        HintSpecification hintSpec)
    {
        List<OptimizationTree> permutationList = new List<OptimizationTree>();
        //if (hintSpec.JoinOrderFixed)
        //{
        //    permutationList.Add(new OptimizationTree(nodeTree));
        //    return permutationList;
        //}
        List<IOptimizationNode> subPermutationList = nodeTree.CreateAllPermutations();
        for (Int32 i = 0; i < subPermutationList.Count; i++)
        {
            OptimizationTree optimizationTree = new OptimizationTree(subPermutationList[i]);

            if (hintSpec.SatisfiesHintedJoinOrder(optimizationTree.ExtentOrder))
                permutationList.Add(optimizationTree);
        }
        if (permutationList.Count > 0)
            return permutationList;

        // If no optimization-tree satisfies the hinted join order then disregard the hinted join order.
        for (Int32 i = 0; i < subPermutationList.Count; i++)
            permutationList.Add(new OptimizationTree(subPermutationList[i]));
        return permutationList;
    }

    internal static void FlagInnermostExtent(OptimizationTree optimizationTree)
    {
        List<Int32> extentOrder = optimizationTree.ExtentOrder;
        Int32 innermostExtent = extentOrder.Count - 1;
        ExtentNode extentNode = optimizationTree.GetExtentNode(innermostExtent);
        extentNode.InnermostExtent = true;
    }

    /// <summary>
    /// Investigates if the input comparison operator can be used in a specification of a range or not.
    /// </summary>
    /// <param name="compOp">An input comparison operator.</param>
    /// <returns>True, if the input comparison operator can be used in a specification of a range, otherwise false.</returns>
    internal static Boolean RangeOperator(ComparisonOperator compOp)
    {
        switch (compOp)
        {
            case ComparisonOperator.Equal:
            case ComparisonOperator.GreaterThan:
            case ComparisonOperator.GreaterThanOrEqual:
            case ComparisonOperator.LessThan:
            case ComparisonOperator.LessThanOrEqual:
            case ComparisonOperator.IS:
            case ComparisonOperator.ISNOT:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Investigates if the input comparison operator is a reversable operator.
    /// </summary>
    /// <param name="compOp">An input comparison operator.</param>
    /// <returns>True, if the input comparison operator is reversable, otherwise false.</returns>
    internal static Boolean ReversableOperator(ComparisonOperator compOp)
    {
        switch (compOp)
        {
            case ComparisonOperator.Equal:
            case ComparisonOperator.NotEqual:
            case ComparisonOperator.GreaterThan:
            case ComparisonOperator.GreaterThanOrEqual:
            case ComparisonOperator.LessThan:
            case ComparisonOperator.LessThanOrEqual:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Creates the reverse operator of the input comparison operator.
    /// </summary>
    /// <param name="compOp">An input comparison operator.</param>
    /// <returns>The reverse operator of the input comparison operator if there exists one, otherwise an exception.</returns>
    internal static ComparisonOperator ReverseOperator(ComparisonOperator compOp)
    {
        switch (compOp)
        {
            case ComparisonOperator.Equal:
                return ComparisonOperator.Equal;
            case ComparisonOperator.NotEqual:
                return ComparisonOperator.NotEqual;
            case ComparisonOperator.GreaterThan:
                return ComparisonOperator.LessThan;
            case ComparisonOperator.GreaterThanOrEqual:
                return ComparisonOperator.LessThanOrEqual;
            case ComparisonOperator.LessThan:
                return ComparisonOperator.GreaterThan;
            case ComparisonOperator.LessThanOrEqual:
                return ComparisonOperator.GreaterThanOrEqual;
            default:
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Non-reversable operator: " + compOp);
        }
    }
}
}
