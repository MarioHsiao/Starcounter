﻿
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
internal class SortNode : IOptimizationNode
{
    IOptimizationNode subNode;
    SortSpecification sortSpec;
    VariableArray variableArr;
    String query;

    internal SortNode(IOptimizationNode subNode, SortSpecification sortSpec, VariableArray varArr, String query)
    {
        if (subNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subNode.");
        if (sortSpec == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect sortSpec.");

        this.subNode = subNode;
        this.sortSpec = sortSpec;
        variableArr = varArr;
        this.query = query;
    }

    public void InstantiateExtentOrder(List<Int32> extentOrder)
    {
        subNode.InstantiateExtentOrder(extentOrder);
    }

    public void InstantiateNodesByExtentNumber(ExtentNode[] nodesByExtentNumber)
    {
        subNode.InstantiateNodesByExtentNumber(nodesByExtentNumber);
    }

    public List<IOptimizationNode> CreateAllPermutations()
    {
        List<IOptimizationNode> subPermutationList = subNode.CreateAllPermutations();
        List<IOptimizationNode> permutationList = new List<IOptimizationNode>();
        for (Int32 i = 0; i < subPermutationList.Count; i++)
        {
            permutationList.Add(new SortNode(subPermutationList[i], sortSpec, variableArr, query));
        }
        return permutationList;
    }

    public IOptimizationNode Clone()
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Int32 EstimateCost()
    {
        Int32 subCost = subNode.EstimateCost();
        return (subCost * Convert.ToInt32(Math.Log(Convert.ToDouble(subCost), 2)));
    }

    internal IOptimizationNode CreateSortOptimizedTopNode(List<Int32> extentOrder, ExtentNode[] nodesByExtentNumber)
    {
        // Save the estimated cost before sort optimization.
        Int32 estimatedCostBefore = EstimateCost();
        // Investigate if the input extent order is compatible with the sort extent order required by the sort specification.
        List<Int32> sortExtentOrder = sortSpec.CreateSortExtentOrder();
        if (sortExtentOrder == null)
        {
            return null;
        }
        if (sortExtentOrder.Count > extentOrder.Count)
        {
            return null;
        }
        for (Int32 i = 0; i < sortExtentOrder.Count; i++)
        {
            if (sortExtentOrder[i] != extentOrder[i])
            {
                return null;
            }
        }
        // Investigate if there are indexes that can be used for sort optimization.
        List<IndexUseInfo> indexUseInfoList = sortSpec.CreateIndexUseInfoList();
        if (indexUseInfoList == null || indexUseInfoList.Count != sortExtentOrder.Count)
        {
            return null;
        }
        // Investigate if there is any applied index hint which prevents sort optimization.
        ExtentNode extentNode = null;
        for (Int32 i = 0; i < sortExtentOrder.Count; i++)
        {
            extentNode = nodesByExtentNumber[sortExtentOrder[i]];
            if (extentNode.HintedIndexInfo != null && extentNode.HintedIndexInfo != indexUseInfoList[i].IndexInfo)
            {
                return null;
            }
        }
        // Distribute information about indexes to be used for sort optimization.
        for (Int32 i = 0; i < sortExtentOrder.Count; i++)
        {
            nodesByExtentNumber[sortExtentOrder[i]].SortIndexInfo = indexUseInfoList[i];
        }
        // Save the estimated cost after sort optimization.
        Int32 estimatedCostAfter = subNode.EstimateCost();
        // If the sort optimized node tree is preferable then return a node tree without sort node.
        if (estimatedCostAfter < estimatedCostBefore)
        {
            return subNode;
        }
        // If the original node tree is preferable then remove the index information for sort optimization.
        for (Int32 i = 0; i < sortExtentOrder.Count; i++)
        {
            nodesByExtentNumber[sortExtentOrder[i]].SortIndexInfo = null;
        }
        return null;
    }

    public IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        // For sort we ignore the fetch specification. Instead we return the complete result set.
        IExecutionEnumerator subEnumerator = subNode.CreateExecutionEnumerator(null, null);
        return new Sort(subEnumerator, sortSpec.CreateComparer(), variableArr, query);
    }
}
}
