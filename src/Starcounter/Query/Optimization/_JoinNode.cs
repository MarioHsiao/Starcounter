// ***********************************************************************
// <copyright file="_JoinNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
internal class JoinNode : IOptimizationNode
{
    RowTypeBinding rowTypeBind;
    JoinType joinType;
    IOptimizationNode leftNode;
    IOptimizationNode rightNode;
    ILogicalExpression postFilterCondition;
    VariableArray varArray;
    String query;

    internal JoinNode(RowTypeBinding rowTypeBind, JoinType joinType, IOptimizationNode leftNode, IOptimizationNode rightNode, 
        VariableArray varArray, String query)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (leftNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect leftNode.");
        if (rightNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rightNode.");
        if (query == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect query.");

        this.rowTypeBind = rowTypeBind;
        if (joinType != JoinType.RightOuter)
        {
            this.joinType = joinType;
            this.leftNode = leftNode;
            this.rightNode = rightNode;
        }
        else
        // Replace a right outer join with a left outer join.
        {
            this.joinType = JoinType.LeftOuter;
            this.leftNode = rightNode;
            this.rightNode = leftNode;
        }
        postFilterCondition = null;
        this.varArray = varArray;
        this.query = query;
    }

    internal void AddPostFilterCondition(ILogicalExpression cond)
    {
        if (postFilterCondition == null)
            postFilterCondition = cond;
        else if (cond != null)
            postFilterCondition = new LogicalOperation(LogicalOperator.AND, postFilterCondition, cond);
    }

    public void InstantiateExtentOrder(List<Int32> extentOrder)
    {
        leftNode.InstantiateExtentOrder(extentOrder);
        rightNode.InstantiateExtentOrder(extentOrder);
    }

    public void InstantiateNodesByExtentNumber(ExtentNode[] nodesByExtentNumber)
    {
        leftNode.InstantiateNodesByExtentNumber(nodesByExtentNumber);
        rightNode.InstantiateNodesByExtentNumber(nodesByExtentNumber);
    }

    public List<IOptimizationNode> CreateAllPermutations()
    {
        List<IOptimizationNode> leftPermutationList = leftNode.CreateAllPermutations();
        List<IOptimizationNode> rightPermutationList = rightNode.CreateAllPermutations();
        List<IOptimizationNode> permutationList = new List<IOptimizationNode>();
        IOptimizationNode currentLeftNode = null;
        IOptimizationNode currentRightNode = null;
        for (Int32 i = 0; i < leftPermutationList.Count; i++)
        {
            for (Int32 j = 0; j < rightPermutationList.Count; j++)
            {
                currentLeftNode = leftPermutationList[i].Clone();
                currentRightNode = rightPermutationList[j].Clone();
                permutationList.Add(new JoinNode(rowTypeBind, joinType, currentLeftNode, currentRightNode, varArray, query));
            }
        }
        
        // For outer joins the join order can not be switched.
        if (joinType != JoinType.Inner)
        {
            return permutationList;
        }
        
        for (Int32 i = 0; i < rightPermutationList.Count; i++)
        {
            for (Int32 j = 0; j < leftPermutationList.Count; j++)
            {
                currentRightNode = rightPermutationList[i].Clone();
                currentLeftNode = leftPermutationList[j].Clone();
                permutationList.Add(new JoinNode(rowTypeBind, joinType, currentRightNode, currentLeftNode, varArray, query));
            }
        }
        return permutationList;
    }

    public void MoveConditionsWithRespectToOuterJoins(JoinNode parentNode)
    {
        leftNode.MoveConditionsWithRespectToOuterJoins(this);
        rightNode.MoveConditionsWithRespectToOuterJoins(this);
    }

    public IOptimizationNode Clone()
    {
        return new JoinNode(rowTypeBind, joinType, leftNode.Clone(), rightNode.Clone(), varArray, query);
    }

    public Int32 EstimateCost()
    {
        return leftNode.EstimateCost() * rightNode.EstimateCost();
    }

    public IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr,
        Boolean topNode, ref Byte nodeId)
    {
        IExecutionEnumerator leftEnumerator = leftNode.CreateExecutionEnumerator(null, null, fetchOffsetKeyExpr, false, ref nodeId);
        IExecutionEnumerator rightEnumerator = rightNode.CreateExecutionEnumerator(null, null, fetchOffsetKeyExpr, false, ref nodeId);
        return new Join(nodeId++, rowTypeBind, joinType, leftEnumerator, rightEnumerator, postFilterCondition, 
            fetchNumExpr, fetchOffsetExpr, fetchOffsetKeyExpr, varArray, query, topNode);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IOptimizationNode other) {
        JoinNode otherNode = other as JoinNode;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(JoinNode other) {
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
        Debug.Assert(this.joinType == other.joinType);
        if (this.joinType != other.joinType)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.leftNode == null) {
            Debug.Assert(other.leftNode == null);
            areEquals = other.leftNode == null;
        } else
            areEquals = this.leftNode.AssertEquals(other.leftNode);
        if (areEquals)
            if (this.rightNode == null) {
                Debug.Assert(other.rightNode == null);
                areEquals = other.rightNode == null;
            } else
                areEquals = this.rightNode.AssertEquals(other.rightNode);
        if (areEquals)
            if (this.rowTypeBind == null) {
                Debug.Assert(other.rowTypeBind == null);
                areEquals = other.rowTypeBind == null;
            } else
                areEquals = this.rowTypeBind.AssertEquals(other.rowTypeBind);
        if (areEquals)
            if (this.varArray == null) {
                Debug.Assert(other.varArray == null);
                areEquals = other.varArray == null;
            } else
                areEquals = this.varArray.AssertEquals(other.varArray);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
