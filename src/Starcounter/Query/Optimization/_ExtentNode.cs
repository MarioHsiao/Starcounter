// ***********************************************************************
// <copyright file="_ExtentNode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Optimization
{
internal class ExtentNode : IOptimizationNode
{
    /// <summary>
    /// The estimated cost for a reference lookup.
    /// </summary>
    const Int32 REFERENCE_LOOKUP_COST = 1;

    /// <summary>
    /// The estimated cost for an index scan.
    /// </summary>
    const Int32 INDEX_SCAN_COST = 2;

    /// <summary>
    /// The estimated cost for an extent scan.
    /// </summary>
    const Int32 EXTENT_SCAN_COST = 100;

    /// <summary>
    /// The type binding of the resulting objects of the current query.
    /// </summary>
    RowTypeBinding rowTypeBind;

    /// <summary>
    /// The extent number of the extent represented by this extent node.
    /// </summary>
    Int32 extentNumber;

    /// <summary>
    /// Conditions that for a particular permutation are attached to this extent node.
    /// </summary>
    List<ILogicalExpression> conditionList;

    /// <summary>
    /// Expression to be used for reference lookup.
    /// </summary>
    IObjectExpression refLookUpExpression;

    /// <summary>
    /// Index to be used for an index scan specified by an index hint in the query.
    /// </summary>
    IndexInfo hintedIndexInfo;

    /// <summary>
    /// Index to be used for an index scan selected by the optimizer.
    /// </summary>
    IndexInfo bestIndexInfo;

    /// <summary>
    /// The arity used of the selected index. It might be the case that not all the
    /// paths in a selected combined index can be used in the execution of a query.
    /// </summary>
    Int32 bestIndexInfoUsedArity;

    /// <summary>
    /// Index to be used for an index scan to make the removal of sorting possible.
    /// </summary>
    IndexUseInfo sortIndexInfo;

    /// <summary>
    /// Index to be used for an extent scan (index scan over the whole extent).
    /// </summary>
    IndexInfo extentIndexInfo;

    VariableArray variableArr;
    String query;

    /// <summary>
    /// True, if this extent-node represents the innermost extent, otherwise false.
    /// </summary>
    internal Boolean InnermostExtent = false;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowTypeBind">The comp type bind.</param>
    /// <param name="extentNumber">The extent number.</param>
    /// <param name="varArr">The var arr.</param>
    /// <param name="query">The query.</param>
    internal ExtentNode(RowTypeBinding rowTypeBind, Int32 extentNumber, VariableArray varArr, String query)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");

        this.rowTypeBind = rowTypeBind;
        this.extentNumber = extentNumber;
        conditionList = new List<ILogicalExpression>();
        refLookUpExpression = null;
        hintedIndexInfo = null;
        bestIndexInfo = null;
        bestIndexInfoUsedArity = 0;
        sortIndexInfo = null;
        extentIndexInfo = null;

        variableArr = varArr;
        this.query = query;
    }

    internal IndexInfo HintedIndexInfo
    {
        get
        {
            return hintedIndexInfo;
        }
        set
        {
            hintedIndexInfo = value;
        }
    }

    internal IndexUseInfo SortIndexInfo
    {
        get
        {
            return sortIndexInfo;
        }
        set
        {
            sortIndexInfo = value;
        }
    }

    public void InstantiateExtentOrder(List<Int32> extentOrder)
    {
        extentOrder.Add(extentNumber);
    }

    public void InstantiateNodesByExtentNumber(ExtentNode[] nodesByExtentNumber)
    {
        nodesByExtentNumber[extentNumber] = this;
    }

    private ILogicalExpression GetCondition()
    {
        if (conditionList.Count == 0)
        {
            return new LogicalLiteral(TruthValue.TRUE);
        }
        ILogicalExpression condition = conditionList[0];
        for (Int32 i = 1; i < conditionList.Count; i++)
        {
            condition = new LogicalOperation(LogicalOperator.AND, condition, conditionList[i]);
        }
        return condition;
    }

    public List<IOptimizationNode> CreateAllPermutations()
    {
        List<IOptimizationNode> permutationList = new List<IOptimizationNode>();
        permutationList.Add(this);
        return permutationList;
    }

    public IOptimizationNode Clone()
    {
        return new ExtentNode(rowTypeBind, extentNumber, variableArr, query);
    }

    internal void AddConditions(List<ILogicalExpression> condList)
    {
        if (condList == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect condList.");
        }
        conditionList.AddRange(condList);
    }

    internal void EvaluateScanAlternatives()
    {
        // If there is a hinted index then it should be used.
        if (hintedIndexInfo != null)
        {
            return;
        }
        // Try to find a reference lookup.
        Int32 i = 0;
        while (refLookUpExpression == null && i < conditionList.Count)
        {
            if (conditionList[i] is ComparisonObject)
            {
                refLookUpExpression = (conditionList[i] as ComparisonObject).GetReferenceLookUpExpression(extentNumber);
            }
            if (refLookUpExpression != null)
            {
                conditionList.RemoveAt(i);
                return;
            }
            i++;
        }
        // Try to find an appropriate index for an index scan.
        // Collect all comparisons with paths that might be used for index scans.
        List<IComparison> comparisonList = new List<IComparison>();
        for (Int32 j = 0; j < conditionList.Count; j++)
        {
            if (conditionList[j] is IComparison &&
                (conditionList[j] as IComparison).GetPathTo(extentNumber) != null &&
                Optimizer.RangeOperator((conditionList[j] as IComparison).Operator))
            {
                comparisonList.Add(conditionList[j] as IComparison);
            }
        }
        // Get all index infos for the current type.
        IndexInfo[] indexInfoArr = (rowTypeBind.GetTypeBinding(extentNumber) as TypeBinding).GetAllIndexInfos();

        // Select an index determined by the order the conditions occur in the query.
        Int32 bestValue = 0;
        Int32 currentValue = 0;
        Int32 usedArity = 0;
        for (Int32 k = 0; k < indexInfoArr.Length; k++)
        {
            currentValue = EvaluateIndex(indexInfoArr[k], comparisonList, out usedArity);
            if (currentValue > bestValue)
            {
                bestIndexInfo = indexInfoArr[k];
                bestIndexInfoUsedArity = usedArity;
                bestValue = currentValue;
            }
        }
        // Save an index to be used for an extent scan (index scan over the whole extent).
        if (indexInfoArr.Length > 0)
        {
            extentIndexInfo = indexInfoArr[0];
        }
    }

    private Int32 EvaluateIndex(IndexInfo indexInfo, List<IComparison> comparisonList, out Int32 usedArity)
    {
        Int32 value = 0;
        ComparisonOperator compOperator = ComparisonOperator.Equal;
        usedArity = 0;
        // Looping over columns of compound index
        while (usedArity < indexInfo.AttributeCount && compOperator == ComparisonOperator.Equal)
        {
            value += EvaluateIndexPath(indexInfo.GetColumnName(usedArity), comparisonList, out compOperator);
            usedArity++;
        }
        if (compOperator == ComparisonOperator.NotEqual)
        {
            usedArity--;
        }
        return value;
    }

    // A value "NotEqual" on output comparison operator (compOperator) indicates no match has been found.
    // A match on an earlier comparison gives a higher return value than a match on a later comparison.
    private Int32 EvaluateIndexPath(String path, List<IComparison> comparisonList, out ComparisonOperator compOperator)
    {
        compOperator = ComparisonOperator.NotEqual;
        Int32 value = 0;
        for (Int32 i = 0; i < comparisonList.Count; i++)
        {
            value = value * 2;
            if (comparisonList[i].GetPathTo(extentNumber).FullName == path)
            {
                value++;
                if (compOperator != ComparisonOperator.Equal)
                {
                    compOperator = comparisonList[i].Operator;
                }
            }
        }
        return value;
    }

    public Int32 EstimateCost()
    {
        if (hintedIndexInfo != null || sortIndexInfo != null)
        {
            return INDEX_SCAN_COST;
        }
        if (refLookUpExpression != null)
        {
            return REFERENCE_LOOKUP_COST;
        }
        if (bestIndexInfo != null)
        {
            return INDEX_SCAN_COST;
        }
        return EXTENT_SCAN_COST;
    }

    public IExecutionEnumerator CreateExecutionEnumerator(INumericalExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        if (hintedIndexInfo != null)
        {
            return CreateIndexScan(hintedIndexInfo, SortOrder.Ascending, fetchNumExpr, fetchOffsetKeyExpr);
        }

        if (sortIndexInfo != null)
        {
            return CreateIndexScan(sortIndexInfo.IndexInfo, sortIndexInfo.SortOrdering, fetchNumExpr, fetchOffsetKeyExpr);
        }

        if (refLookUpExpression != null)
        {
            return new ReferenceLookup(rowTypeBind, extentNumber, refLookUpExpression, GetCondition(), fetchNumExpr, variableArr, query);
        }

        if (bestIndexInfo != null)
        {
            return CreateIndexScan(bestIndexInfo, SortOrder.Ascending, fetchNumExpr, fetchOffsetKeyExpr);
        }

        if (extentIndexInfo != null)
        {
            if (conditionList.Count > 0)
            {
                // Trying to create a scan which uses native filter code generation.
                try
                {
                    IExecutionEnumerator exec_enum = new FullTableScan(rowTypeBind,
                        extentNumber,
                        extentIndexInfo,
                        GetCondition(),
                        SortOrder.Ascending,
                        fetchNumExpr, 
                        fetchOffsetKeyExpr,
                        InnermostExtent, 
                        null, variableArr, query);

                    if (exec_enum != null)
                    {
                        return exec_enum;  // Returning result only on successful execution
                    }
                }
                catch
                {
                    //Console.WriteLine("Filter code generation for the query \"" + query + "\" has failed. Launching managed-level full table scan...");
                }
            }

            // Proceeding with the worst case: full table scan on managed code level.
            return CreateIndexScan(extentIndexInfo, SortOrder.Ascending, fetchNumExpr, fetchOffsetKeyExpr);
        }
        ITypeBinding typeBind = rowTypeBind.GetTypeBinding(extentNumber);
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "There is no index for type: " + typeBind.Name);
    }

    private IExecutionEnumerator CreateIndexScan(IndexInfo indexInfo, SortOrder sortOrdering, INumericalExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        List<String> strPathList = new List<String>();
        String strPath = null;
        List<IDynamicRange> dynamicRangeList = new List<IDynamicRange>();
        IDynamicRange dynamicRange = null;

        // Saving length of original condition list.
        int origCondListLen = conditionList.Count;

        // Creating dynamic range lists.
        for (Int32 i = 0; i < indexInfo.AttributeCount; i++)
        {
            strPath = indexInfo.GetColumnName(i);
            strPathList.Add(strPath);
            switch (indexInfo.GetTypeCode(i))
            {
                case DbTypeCode.Binary:
                    dynamicRange = new BinaryDynamicRange();
                    break;
                case DbTypeCode.Boolean:
                    dynamicRange = new BooleanDynamicRange();
                    break;
                case DbTypeCode.DateTime:
                    dynamicRange = new DateTimeDynamicRange();
                    break;
                case DbTypeCode.Decimal:
                    dynamicRange = new DecimalDynamicRange();
                    break;
                case DbTypeCode.Int64:
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                    dynamicRange = new IntegerDynamicRange();
                    break;
                case DbTypeCode.Object:
                    dynamicRange = new ObjectDynamicRange();
                    break;
                case DbTypeCode.String:
                    dynamicRange = new StringDynamicRange();
                    break;
                case DbTypeCode.UInt64:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    dynamicRange = new UIntegerDynamicRange();
                    break;
                default:
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeCode.");
            }
            // Instantiate the dynamic range and remove the used conditions from the condition list.
            if (i < bestIndexInfoUsedArity)
            {
                dynamicRange.CreateRangePointList(conditionList, extentNumber, strPath);
            }
            dynamicRangeList.Add(dynamicRange);
        }

        // Creating index scan enumerator.
        return new IndexScan(rowTypeBind,
                             extentNumber,
                             indexInfo,
                             strPathList,
                             dynamicRangeList, GetCondition(),
                             sortOrdering, 
                             fetchNumExpr, fetchOffsetKeyExpr, 
                             InnermostExtent, 
                             variableArr, query);
    }

    private IExecutionEnumerator CreateFullTableScan(IndexInfo indexInfo, IIntegerExpression fetchNumExpr, IBinaryExpression fetchOffsetKeyExpr)
    {
        return new FullTableScan(rowTypeBind,
                                 extentNumber,
                                 indexInfo,
                                 GetCondition(),
                                 SortOrder.Ascending,
                                 fetchNumExpr, 
                                 fetchOffsetKeyExpr,
                                 InnermostExtent, 
                                 null, variableArr, query);
    }

    internal IndexInfo GetIndexInfo(String indexName)
    {
        return (rowTypeBind.GetTypeBinding(extentNumber) as TypeBinding).GetIndexInfo(indexName);
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IOptimizationNode other) {
        ExtentNode otherNode = other as ExtentNode;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(ExtentNode other) {
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
        Debug.Assert(this.InnermostExtent == other.InnermostExtent);
        if (this.InnermostExtent != other.InnermostExtent)
            return false;
        Debug.Assert(this.extentNumber == other.extentNumber);
        if (this.extentNumber != other.extentNumber)
            return false;
        Debug.Assert(this.bestIndexInfoUsedArity == other.bestIndexInfoUsedArity);
        if (this.bestIndexInfoUsedArity != other.bestIndexInfoUsedArity)
            return false;
        // Check cardinalities of collections
        Debug.Assert(this.conditionList.Count == other.conditionList.Count);
        if (this.conditionList.Count != other.conditionList.Count)
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
            if (this.refLookUpExpression == null) {
                Debug.Assert(other.refLookUpExpression == null);
                areEquals = other.refLookUpExpression == null;
            } else
                areEquals = this.refLookUpExpression.AssertEquals(other.refLookUpExpression);
        if (areEquals)
            if (this.hintedIndexInfo == null) {
                Debug.Assert(other.hintedIndexInfo == null);
                areEquals = other.hintedIndexInfo == null;
            } else
                areEquals = this.hintedIndexInfo.AssertEquals(other.hintedIndexInfo);
        if (areEquals)
            if (this.bestIndexInfo == null) {
                Debug.Assert(other.bestIndexInfo == null);
                areEquals = other.bestIndexInfo == null;
            } else
                areEquals = this.bestIndexInfo.AssertEquals(other.bestIndexInfo);
        if (areEquals)
            if (this.sortIndexInfo == null) {
                Debug.Assert(other.sortIndexInfo == null);
                areEquals = other.sortIndexInfo == null;
            } else
                areEquals = this.sortIndexInfo.AssertEquals(other.sortIndexInfo);
        if (areEquals)
            if (this.extentIndexInfo == null) {
                Debug.Assert(other.extentIndexInfo == null);
                areEquals = other.extentIndexInfo == null;
            } else
                areEquals = this.extentIndexInfo.AssertEquals(other.extentIndexInfo);
        if (areEquals)
            if (this.variableArr == null) {
                Debug.Assert(other.variableArr == null);
                areEquals = other.variableArr == null;
            } else
                areEquals = this.variableArr.AssertEquals(other.variableArr);
        // Check collections of objects
        for (int i = 0; i < this.conditionList.Count && areEquals; i++)
            areEquals = this.conditionList[i].AssertEquals(other.conditionList[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
