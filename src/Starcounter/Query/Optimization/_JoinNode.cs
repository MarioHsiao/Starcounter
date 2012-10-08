
using Starcounter.Query.Execution;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
internal class JoinNode : IOptimizationNode
{
    CompositeTypeBinding compTypeBind;
    JoinType joinType;
    IOptimizationNode leftNode;
    IOptimizationNode rightNode;
    VariableArray varArray;
    String query;

    internal JoinNode(CompositeTypeBinding compTypeBind, JoinType joinType, IOptimizationNode leftNode, IOptimizationNode rightNode, 
        VariableArray varArray, String query)
    {
        if (compTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect compTypeBind.");
        if (leftNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect leftNode.");
        if (rightNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rightNode.");
        if (query == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect query.");

        this.compTypeBind = compTypeBind;
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
        this.varArray = varArray;
        this.query = query;
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
                permutationList.Add(new JoinNode(compTypeBind, joinType, currentLeftNode, currentRightNode, varArray, query));
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
                permutationList.Add(new JoinNode(compTypeBind, joinType, currentRightNode, currentLeftNode, varArray, query));
            }
        }
        return permutationList;
    }

    public IOptimizationNode Clone()
    {
        return new JoinNode(compTypeBind, joinType, leftNode.Clone(), rightNode.Clone(), varArray, query);
    }

    public Int32 EstimateCost()
    {
        return leftNode.EstimateCost() * rightNode.EstimateCost();
    }

    public IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        IExecutionEnumerator leftEnumerator = leftNode.CreateExecutionEnumerator(null, fetchOffsetKeyExpr);
        IExecutionEnumerator rightEnumerator = rightNode.CreateExecutionEnumerator(null, fetchOffsetKeyExpr);
        return new Join(compTypeBind, joinType, leftEnumerator, rightEnumerator, fetchNumExpr, varArray, query);
    }
}
}
