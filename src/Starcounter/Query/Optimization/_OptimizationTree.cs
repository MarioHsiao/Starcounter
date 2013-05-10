// ***********************************************************************
// <copyright file="_OptimizationTree.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
/// <summary>
/// Class to represent an optimization tree, which consists of optimization nodes.
/// </summary>
internal class OptimizationTree
{
    /// <summary>
    /// The top node of the optimization tree.
    /// </summary>
    IOptimizationNode topNode;

    /// <summary>
    /// A list of extent numbers corresponding to the order the current extents occur
    /// in the optimization tree. The extent order does not include any temporary extents
    /// used by aggregations.
    /// </summary>
    List<Int32> extentOrder;

    /// <summary>
    /// An array of extent nodes in the optimization tree indexed by their corresponding extent numbers.
    /// </summary>
    ExtentNode[] nodesByExtentNumber;

    /// <summary>
    /// The estimated execution cost for the execution represented by this optimization tree.
    /// </summary>
    Int32 estimatedCost;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal OptimizationTree(IOptimizationNode topNode)
    {
        if (topNode == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect topNode.");
        }
        this.topNode = topNode;
        extentOrder = new List<Int32>();
        topNode.InstantiateExtentOrder(extentOrder);
        nodesByExtentNumber = new ExtentNode[extentOrder.Count];
        topNode.InstantiateNodesByExtentNumber(nodesByExtentNumber);
        estimatedCost = -1; // The value -1 represents "unknown" (no value yet calculated).
    }

    internal List<Int32> ExtentOrder
    {
        get
        {
            return extentOrder;
        }
    }

    internal Int32 EstimatedCost
    {
        get
        {
            if (estimatedCost == -1)
            {
                estimatedCost = topNode.EstimateCost();
            }
            return estimatedCost;
        }
    }

    internal ExtentNode GetExtentNode(Int32 extentNumber)
    {
        if (extentNumber < 0 || extentNumber >= nodesByExtentNumber.Length)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentNumber.");
        }
        return nodesByExtentNumber[extentNumber];
    }

    internal void DistributeIndexHints(List<IndexHint> indexHintList)
    {
        for (Int32 i = 0; i < indexHintList.Count; i++)
        {
            var indexHint = indexHintList[i];
            ExtentNode extentNode = GetExtentNode(indexHint.ExtentNumber);
            if (extentNode.HintedIndexInfo == null) // Only the first index hint should be considered.
                extentNode.HintedIndexInfo = extentNode.GetIndexInfo(indexHint.IndexName);
        }
    }

    /// <summary>
    /// Distributes the conditions in the input condition dictionary over the extent nodes in the optimizing
    /// tree. The conditions are placed in the first extent node where they can be evaluated.
    /// </summary>
    /// <param name="conditionDict">A dictionary of conditions present in the query.</param>
    internal void DistributeConditions(ConditionDictionary conditionDict)
    {
        // TODO: Maybe implement by looping over conditions instead?
        // Add conditions without any extent reference to the first extent.
        ExtentNode extentNode = nodesByExtentNumber[extentOrder[0]];
        ExtentSet extentSet = new ExtentSet(); // An empty extent set represents no extent references.
        List<ILogicalExpression> conditionList = conditionDict.GetConditions(extentSet);
        if (conditionList != null)
        {
            extentNode.AddConditions(conditionList);
        }
        // To each extent node add the conditions referencing that extent, possibly some previuos extents
        // and no subsequent extents.
        List<ExtentSet> extentSetList = null;
        for (Int32 i = 0; i < extentOrder.Count; i++)
        {
            extentNode = nodesByExtentNumber[extentOrder[i]];
            extentSetList = ExtentSet.CreateAllExtentSets(extentOrder[i], extentOrder);
            for (Int32 j = 0; j < extentSetList.Count; j++)
            {
                conditionList = conditionDict.GetConditions(extentSetList[j]);
                if (conditionList != null)
                {
                    extentNode.AddConditions(conditionList);
                }
            }
        }
    }

    internal void EvaluateScanAlternatives()
    {
        for (Int32 i = 0; i < nodesByExtentNumber.Length; i++)
        {
            nodesByExtentNumber[i].EvaluateScanAlternatives();
        }
    }

    internal void SortOptimize()
    {
        if (!(topNode is SortNode))
        {
            return;
        }
        IOptimizationNode node = (topNode as SortNode).CreateSortOptimizedTopNode(extentOrder, nodesByExtentNumber);
        if (node != null)
        {
            topNode = node;
            estimatedCost = topNode.EstimateCost();
        }
    }

    internal IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        byte nodeId = 0;
        IExecutionEnumerator enumerator = topNode.CreateExecutionEnumerator(fetchNumExpr, fetchOffsetExpr, fetchOffsetKeyExpr, ref nodeId);
        ((ExecutionEnumerator)enumerator).TopNode = true;
        enumerator.RowTypeBinding.ExtentOrder = extentOrder;
        if (topNode is SortNode)
            enumerator.VarArray.QueryFlags = enumerator.VarArray.QueryFlags | QueryFlags.IncludesSorting;
        return enumerator;
    }
}
}

