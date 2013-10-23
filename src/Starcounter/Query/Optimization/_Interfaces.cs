// ***********************************************************************
// <copyright file="_Interfaces.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
internal interface IOptimizationNode
{
    IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr, 
        Boolean topNode, ref Byte nodeId);

    void InstantiateExtentOrder(List<Int32> extentOrder);

    void InstantiateNodesByExtentNumber(ExtentNode[] nodesByExtentNumber);

    Int32 EstimateCost();

    List<IOptimizationNode> CreateAllPermutations();

    IOptimizationNode Clone();

    void MoveConditionsWithRespectToOuterJoins(JoinNode parentNode);

#if DEBUG
    bool AssertEquals(IOptimizationNode other);
#endif
}

/// <summary>
/// Interface for all types of hints.
/// </summary>
internal interface IHint
{ }
}
