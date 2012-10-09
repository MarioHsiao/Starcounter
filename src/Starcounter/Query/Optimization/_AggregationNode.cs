
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Optimization
{
internal class AggregationNode : IOptimizationNode
{
    CompositeTypeBinding compTypeBind;
    Int32 extentNumber;
    IOptimizationNode subNode;
    SortSpecification sortSpec;
    List<ISetFunction> setFunctionList;
    ILogicalExpression havingCondition; // The condition in the HAVING-clause of the query.
    VariableArray variableArr;
    String query;

    internal AggregationNode(CompositeTypeBinding compTypeBind, Int32 extentNumber, IOptimizationNode subNode, SortSpecification sortSpec, 
        List<ISetFunction> setFunctionList, ILogicalExpression havingCondition, VariableArray varArr, String query)
    {
        if (compTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect resultTypeBind.");
        if (subNode == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subNode.");
        if (sortSpec == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect sortSpec.");
        if (setFunctionList == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFunctionList.");
        if (havingCondition == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect havingCondition.");

        this.compTypeBind = compTypeBind;
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
            permutationList.Add(new AggregationNode(compTypeBind, extentNumber, subPermutationList[i], sortSpec,
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

    public IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        // For aggregations we ignore the fetch specification. Instead we return the complete result set.
        IExecutionEnumerator subEnumerator = subNode.CreateExecutionEnumerator(null, null);
        IQueryComparer comparer = sortSpec.CreateComparer();
        return new Aggregation(compTypeBind, extentNumber, subEnumerator, comparer, setFunctionList, havingCondition, 
            variableArr, query);
    }
}
}
