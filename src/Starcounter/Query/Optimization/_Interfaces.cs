
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
internal interface IOptimizationNode
{
    IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr);

    void InstantiateExtentOrder(List<Int32> extentOrder);

    void InstantiateNodesByExtentNumber(ExtentNode[] nodesByExtentNumber);

    Int32 EstimateCost();

    List<IOptimizationNode> CreateAllPermutations();

    IOptimizationNode Clone();
}

/// <summary>
/// Interface for all types of hints.
/// </summary>
internal interface IHint
    { }
}
