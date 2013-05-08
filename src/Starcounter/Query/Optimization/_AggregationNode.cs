// ***********************************************************************
// <copyright file="_AggregationNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
internal class AggregationNode : IOptimizationNode
{
    RowTypeBinding rowTypeBind;
    Int32 extentNumber;
    IOptimizationNode subNode;
    SortSpecification sortSpec;
    List<ISetFunction> setFunctionList;
    ILogicalExpression havingCondition; // The condition in the HAVING-clause of the query.
    VariableArray variableArr;
    String query;

    internal AggregationNode(RowTypeBinding rowTypeBind, Int32 extentNumber, IOptimizationNode subNode, SortSpecification sortSpec, 
        List<ISetFunction> setFunctionList, ILogicalExpression havingCondition, VariableArray varArr, String query)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (subNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subNode.");
        if (sortSpec == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect sortSpec.");
        if (setFunctionList == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFunctionList.");
        if (havingCondition == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect havingCondition.");

        this.rowTypeBind = rowTypeBind;
        this.extentNumber = extentNumber;
        this.subNode = subNode;
        this.sortSpec = sortSpec;
        this.setFunctionList = setFunctionList;
        this.havingCondition = havingCondition;
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
            permutationList.Add(new AggregationNode(rowTypeBind, extentNumber, subPermutationList[i], sortSpec,
                setFunctionList, havingCondition, variableArr, query));
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

    public IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr, ref byte nodeId)
    {
        // For aggregations we ignore the fetch specification. Instead we return the complete result set.
        IExecutionEnumerator subEnumerator = subNode.CreateExecutionEnumerator(null, null, null, ref nodeId);
        IQueryComparer comparer = sortSpec.CreateComparer();
        return new Aggregation(nodeId++, rowTypeBind, extentNumber, subEnumerator, comparer, setFunctionList, havingCondition, 
            variableArr, query, fetchNumExpr, fetchOffsetExpr, fetchOffsetKeyExpr);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IOptimizationNode other) {
        AggregationNode otherNode = other as AggregationNode;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(AggregationNode other) {
        Debug.Assert(other != null);
        if (other == null)
            return false;
        // Check if there are not cyclic references
        Debug.Assert(!this.AssertEqualsVisited);
        if (this.AssertEqualsVisited)
            return false;
        Debug.Assert(!other.AssertEqualsVisited);
        if (other.AssertEqualsVisited)
            return false;
        // Check basic types
        Debug.Assert(this.query == other.query);
        if (this.query != other.query)
            return false;
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        // Check cardinalities of collections
        Debug.Assert(this.setFunctionList.Count == other.setFunctionList.Count);
        if (this.setFunctionList.Count != other.setFunctionList.Count)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.rowTypeBind == null) {
            Debug.Assert(other.rowTypeBind == null);
            areEquals = other.rowTypeBind == null;
        } else
            areEquals = this.rowTypeBind.AssertEquals(other.rowTypeBind);
        if (areEquals)
            if (this.subNode == null) {
                Debug.Assert(other.subNode == null);
                areEquals = other.subNode == null;
            } else
                areEquals = this.subNode.AssertEquals(other.subNode);
        if (areEquals)
            if (this.sortSpec == null) {
                Debug.Assert(other.sortSpec == null);
                areEquals = other.sortSpec == null;
            } else
                areEquals = this.sortSpec.AssertEquals(other.sortSpec);
        if (areEquals)
            if (this.havingCondition == null) {
                Debug.Assert(other.havingCondition == null);
                areEquals = other.havingCondition == null;
            } else
                areEquals = this.havingCondition.AssertEquals(other.havingCondition);
        if (areEquals)
            if (this.variableArr == null) {
                Debug.Assert(other.variableArr == null);
                areEquals = other.variableArr == null;
            } else
                areEquals = this.variableArr.AssertEquals(other.variableArr);
        // Check collections of objects
        for (int i = 0; i < this.setFunctionList.Count && areEquals; i++)
            areEquals = this.setFunctionList[i].AssertEquals(other.setFunctionList[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
