// ***********************************************************************
// <copyright file="_Creator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using se.sics.prologbeans;
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Starcounter.Binding;
using Starcounter.Query.SQL;

namespace Starcounter.Query.Sql
{
    internal static class Creator
    {
        internal static OptimizerInput CreateOptimizerInput(RowTypeBinding rowTypeBind,
                                                              Term term,
                                                              VariableArray varArray,
                                                              String query)
        {
            if (term.Name == "select2" && term.Arity == 7)
            {
                return MapTermsToOptimizerInput(rowTypeBind, term.getArgument(3), term.getArgument(4), term.getArgument(5), term.getArgument(6), term.getArgument(7), 
                    varArray, query);
            }

            if (term.Name == "select3" && term.Arity == 11)
            {
                //if (!QueryModule.AggregationSupport) throw new SqlException("Aggregations are not supported.");
                varArray.QueryFlags |= QueryFlags.IncludesAggregation;
                return MapTermsToOptimizerInputWithAggregation(rowTypeBind, term.getArgument(3), term.getArgument(4), term.getArgument(5), term.getArgument(6),
                    term.getArgument(7), term.getArgument(8), term.getArgument(9), term.getArgument(10), term.getArgument(11), varArray, query);
            }

            //if (term.Name == "extentScan" && term.Arity == 2)
            //{
            //    return CreateFullTableScan(rowTypeBind, term.getArgument(1), term.getArgument(2), varArray, query);
            //}

            //if (term.Name == "indexScan" && term.Arity == 7)
            //{
            //    return CreateIndexScan(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), term.getArgument(4), term.getArgument(5), term.getArgument(6), term.getArgument(7), varArray);
            //}

            //if (term.Name == "refLookup" && term.Arity == 3)
            //{
            //    return CreateReferenceLookup(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), varArray, query);
            //}

            //if (term.Name == "joinOperation" && term.Arity == 3)
            //{
            //    return CreateJoin(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), varArray, query);
            //}

            //if (term.Name == "sort" && term.Arity == 2)
            //{
            //    return CreateSort(rowTypeBind, term.getArgument(1), term.getArgument(2), varArray, query);
            //}

            //if (term.Name == "aggregation" && term.Arity == 5)
            //{
            //    //if (!QueryModule.AggregationSupport) throw new SqlException("Aggregations are not supported.");
            //    varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesAggregation;
            //    return CreateAggregation(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), term.getArgument(4), term.getArgument(5), varArray, query);
            //}

            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expression: " + term);
        }

        private static OptimizerInput MapTermsToOptimizerInputWithAggregation(RowTypeBinding rowTypeBind, Term nodeTreeTerm,
                Term whereCondTerm, Term groupbyComparerListTerm, Term setFuncListTerm, Term havingCondTerm, Term tempExtentTerm,
                Term orderbyComparerListTerm, Term fetchTerm, Term hintListTerm, VariableArray varArray, String query)
        {
            if (tempExtentTerm.Name != "extent" || tempExtentTerm.Arity != 2)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect tempExtentTerm: " + tempExtentTerm);
            }
            Term extNumTerm = tempExtentTerm.getArgument(1);
            if (!extNumTerm.Integer)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extent number in tempExtentTerm: " + tempExtentTerm);
            }
            Int32 extNum = extNumTerm.intValue();
            // Create the optimization tree.
            IOptimizationNode nodeTree = CreateNodeTree(rowTypeBind, nodeTreeTerm, varArray, query);
            // Add aggregation node.
            SortSpecification groupbySortSpec = CreateSortSpecification(rowTypeBind, groupbyComparerListTerm, varArray);
            List<ISetFunction> setFuncList = CreateSetFunctionList(rowTypeBind, setFuncListTerm, varArray);
            ILogicalExpression havingCond = CreateLogicalExpression(rowTypeBind, havingCondTerm, varArray);
            nodeTree = new AggregationNode(rowTypeBind, extNum, nodeTree, groupbySortSpec, setFuncList, havingCond, varArray, query);
            // Add sort node when appropriate.
            SortSpecification orderbySortSpec = CreateSortSpecification(rowTypeBind, orderbyComparerListTerm, varArray);
            if (!orderbySortSpec.IsEmpty())
            {
                nodeTree = new SortNode(nodeTree, orderbySortSpec, varArray, query);
            }
            // Create condition dictionary.
            ConditionDictionary conditionDict = new ConditionDictionary();
            ILogicalExpression whereCond = CreateLogicalExpression(rowTypeBind, whereCondTerm, varArray);
            conditionDict.AddCondition(whereCond);
            // Create fetch specification (number and offset expressions).
            INumericalExpression fetchNumExpr = null;
            INumericalExpression fetchOffsetExpr = null;
            IBinaryExpression fetchOffsetKeyExpr = null;
            CreateFetchSpecification(rowTypeBind, fetchTerm, varArray, out fetchNumExpr, out fetchOffsetExpr, out fetchOffsetKeyExpr);
            // Create hint specification.
            HintSpecification hintSpec = CreateHintSpecification(rowTypeBind, hintListTerm, varArray);
            return new OptimizerInput(nodeTree, conditionDict, fetchNumExpr, fetchOffsetExpr, fetchOffsetKeyExpr, hintSpec);
        }

        private static OptimizerInput MapTermsToOptimizerInput(RowTypeBinding rowTypeBind, Term nodeTreeTerm, Term whereCondTerm,
                Term orderbyComparerListTerm, Term fetchTerm, Term hintListTerm, VariableArray varArray, String query)
        {
            // Create tree of optimizing nodes.
            IOptimizationNode nodeTree = CreateNodeTree(rowTypeBind, nodeTreeTerm, varArray, query);
            // Add sort node when appropriate.
            SortSpecification orderbySortSpec = CreateSortSpecification(rowTypeBind, orderbyComparerListTerm, varArray);
            if (!orderbySortSpec.IsEmpty())
            {
                nodeTree = new SortNode(nodeTree, orderbySortSpec, varArray, query);
            }
            // Create condition dictionary.
            ConditionDictionary conditionDict = new ConditionDictionary();
            ILogicalExpression whereCond = CreateLogicalExpression(rowTypeBind, whereCondTerm, varArray);
            conditionDict.AddCondition(whereCond);
            // Create fetch specification (number and offset expressions).
            INumericalExpression fetchNumExpr = null;
            INumericalExpression fetchOffsetExpr = null;
            IBinaryExpression fetchOffsetKeyExpr = null;
            CreateFetchSpecification(rowTypeBind, fetchTerm, varArray, out fetchNumExpr, out fetchOffsetExpr, out fetchOffsetKeyExpr);
            // Create hint specification.
            HintSpecification hintSpec = CreateHintSpecification(rowTypeBind, hintListTerm, varArray);
            return new OptimizerInput(nodeTree, conditionDict, fetchNumExpr, fetchOffsetExpr, fetchOffsetKeyExpr, hintSpec);
        }

        // Output is returned in arguments fetchNumExpr and fetchOffsetKeyExpr.
        private static void CreateFetchSpecification(RowTypeBinding rowTypeBind, Term fetchTerm, VariableArray varArray,
            out INumericalExpression fetchNumExpr, out INumericalExpression fetchOffsetExpr, out IBinaryExpression fetchOffsetKeyExpr)
        {
            if (fetchTerm.Name != "fetch" || fetchTerm.Arity != 2)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect fetchTerm: " + fetchTerm);

            Term argument1 = fetchTerm.getArgument(1);
            if (argument1.Name == "noNumber")
                fetchNumExpr = null;
            else
            {
                IValueExpression expr1 = CreateValueExpression(rowTypeBind, argument1, varArray);
                if (expr1 is INumericalExpression)
                    fetchNumExpr = expr1 as INumericalExpression;
                else
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect fetchTerm: " + fetchTerm);
                if (expr1 is ILiteral)
                    varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesFetchLiteral;
                if (expr1 is IVariable)
                    varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesFetchVariable;
            }

            Term argument2 = fetchTerm.getArgument(2);
            fetchOffsetExpr = null;
            fetchOffsetKeyExpr = null;
            
            if (argument2.Name != "noOffset")
            {
                IValueExpression expr2 = CreateValueExpression(rowTypeBind, argument2, varArray);
                if (expr2 is IBinaryExpression)
                {
                    fetchOffsetKeyExpr = expr2 as IBinaryExpression;

                    if (expr2 is ILiteral)
                        varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesOffsetKeyLiteral;
                    if (expr2 is IVariable)
                        varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesOffsetKeyVariable;
                }
                else if (expr2 is INumericalExpression)
                    fetchOffsetExpr = expr2 as INumericalExpression;
                else
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect fetchTerm: " + fetchTerm);
            }
        }

        private static IOptimizationNode CreateNodeTree(RowTypeBinding rowTypeBind, Term treeTerm, VariableArray varArray, String query)
        {
            if (treeTerm.Name == "join2" && treeTerm.Arity == 3)
            {
                JoinType joinType = JoinType.Inner;
                String strJoinType = treeTerm.getArgument(1).Name;
                if (strJoinType == "inner" || strJoinType == "cross")
                {
                    joinType = JoinType.Inner;
                }
                else if (strJoinType == "left")
                {
                    joinType = JoinType.LeftOuter;
                }
                else if (strJoinType == "right")
                {
                    joinType = JoinType.RightOuter;
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect strJoinType: " + strJoinType);
                }
                IOptimizationNode leftNode = CreateNodeTree(rowTypeBind, treeTerm.getArgument(2), varArray, query);
                IOptimizationNode rightNode = CreateNodeTree(rowTypeBind, treeTerm.getArgument(3), varArray, query);
                JoinNode joinNode = new JoinNode(rowTypeBind, joinType, leftNode, rightNode, varArray, query);
                return joinNode;
            }
            if (treeTerm.Name == "extent" && treeTerm.Arity == 2)
            {
                Term extNumTerm = treeTerm.getArgument(1);
                if (!extNumTerm.Integer)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extent number in treeTerm: " + treeTerm);
                }
                Int32 extNum = extNumTerm.intValue();
                return new ExtentNode(rowTypeBind, extNum, varArray, query);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect treeTerm: " + treeTerm);
        }

        private static HintSpecification CreateHintSpecification(RowTypeBinding rowTypeBind, Term hintListTerm, VariableArray varArray)
        {
            if (!hintListTerm.List)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect hintListTerm: " + hintListTerm);
            }
            HintSpecification hintSpec = new HintSpecification();
            Term cursorTerm = hintListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term hintTerm = cursorTerm.getArgument(1);
                IHint hint = CreateHint(rowTypeBind, hintTerm, varArray);
                hintSpec.Add(hint);
                cursorTerm = cursorTerm.getArgument(2);
            }
            return hintSpec;
        }

        private static IHint CreateHint(RowTypeBinding rowTypeBind, Term hintTerm, VariableArray varArray)
        {
            if (hintTerm.Name == "joinOrderHint" && hintTerm.Arity == 1)
            {
                List<Int32> extNumList = CreateExtentNumList(hintTerm.getArgument(1));
                ControlJoinOrderHint(rowTypeBind, extNumList);
                return new JoinOrderHint(extNumList);
            }
            if (hintTerm.Name == "indexHint" && hintTerm.Arity == 2)
            {
                Int32 extentNum = CreateExtentNum(hintTerm.getArgument(1));
                Term indexNameTerm = hintTerm.getArgument(2);
                if (indexNameTerm.Name != "indexName" || indexNameTerm.Arity != 1)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect indexNameTerm: " + indexNameTerm);
                }
                String indexName = indexNameTerm.getArgument(1).Name;
                return new IndexHint(extentNum, indexName);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect hintTerm: " + hintTerm);
        }

        private static void ControlJoinOrderHint(RowTypeBinding rowTypeBind, List<Int32> extNumList)
        {
            if (extNumList.Count == 0)
            {
                return;
            }
            Int32 counter;
            for (Int32 extNum = 0; extNum < rowTypeBind.TypeBindingCount; extNum++)
            {
                counter = 0;
                for (Int32 i = 0; i < extNumList.Count; i++)
                    if (extNum == extNumList[i])
                    {
                        counter++;
                    }
                if (counter != 1)
                {
                    throw new SqlException("Incorrect extent list in join order hint.");
                }
            }
        }

        // An empty list represents a fixed join order.
        private static List<Int32> CreateExtentNumList(Term extNumListTerm)
        {
            List<Int32> extNumList = new List<Int32>();
            if (extNumListTerm.Name == "fixed")
            {
                return extNumList;
            }
            Term cursorTerm = extNumListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                extNumList.Add(CreateExtentNum(cursorTerm.getArgument(1)));
                cursorTerm = cursorTerm.getArgument(2);
            }
            return extNumList;
        }

        private static Int32 CreateExtentNum(Term typeTerm)
        {
            if (typeTerm.Name != "extent" || typeTerm.Arity != 2)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
            }
            Term extNumTerm = typeTerm.getArgument(1);
            if (!extNumTerm.Integer)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extent number in typeTerm: " + typeTerm);
            }
            return extNumTerm.intValue();
        }

        private static List<IPath> CreatePathList(RowTypeBinding rowTypeBind, Term pathListTerm, VariableArray varArray)
        {
            List<IPath> pathList = new List<IPath>();
            if (pathListTerm.Name == "path" && pathListTerm.Arity == 2)
            {
                IPath path = CreatePath(rowTypeBind, pathListTerm.getArgument(2), varArray);
                pathList.Add(path);
                return pathList;
            }
            Term cursorTerm = pathListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pathTerm = cursorTerm.getArgument(1);
                if (pathTerm.Name != "path" || pathTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pathTerm: " + pathTerm);
                }
                IPath path = CreatePath(rowTypeBind, pathTerm.getArgument(2), varArray);
                pathList.Add(path);
                cursorTerm = cursorTerm.getArgument(2);
            }
            return pathList;
        }

        internal static RowTypeBinding CreateRowTypeBinding(Term typeDefTerm, VariableArray varArray)
        {
            if (typeDefTerm.Name != "typeDef" || typeDefTerm.Arity != 2)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeDefTerm: " + typeDefTerm);
            }
            Term typeListTerm = typeDefTerm.getArgument(1);
            Term mapListTerm = typeDefTerm.getArgument(2);
            RowTypeBinding rowTypeBind = CreateInitialRowTypeBinding(typeListTerm);
            AddPropertyMappings(rowTypeBind, mapListTerm, varArray);

            //if ((rowTypeBind.PropertyCount == 1) && (rowTypeBind.GetPropertyBinding(0).TypeCode == DbTypeCode.Object))
            //    varArray.QueryFlags = varArray.QueryFlags | QueryFlags.SingletonProjection;
            if (rowTypeBind.PropertyCount == 1)
                varArray.QueryFlags = varArray.QueryFlags | QueryFlags.SingletonProjection;

            return rowTypeBind;
        }

        private static RowTypeBinding CreateInitialRowTypeBinding(Term typeListTerm)
        {
            RowTypeBinding rowTypeBind = new RowTypeBinding();
            Term cursorTerm = typeListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term typeTerm = cursorTerm.getArgument(1);
                if (typeTerm.Name != "extent" || typeTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
                }
                Term extNumTerm = typeTerm.getArgument(1);
                Term typeSpecTerm = typeTerm.getArgument(2);
                if (!extNumTerm.Integer)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extent number in typeTerm: " + typeTerm);
                }
                if (typeSpecTerm.Atom)
                {
                    String typeName = typeSpecTerm.Name;
                    Int32 extNum = extNumTerm.intValue();
                    if (extNum != rowTypeBind.TypeBindingCount)
                    {
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extent number in typeTerm: " + typeTerm);
                    }
                    rowTypeBind.AddTypeBinding(typeName);
                }
                else if (typeSpecTerm.List && typeSpecTerm.Name != "[]")
                {
                    TemporaryTypeBinding tempTypeBind = CreateTemporaryTypeBinding(typeSpecTerm);
                    rowTypeBind.AddTypeBinding(tempTypeBind);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect specification in typeTerm: " + typeTerm);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return rowTypeBind;
        }

        private static TemporaryTypeBinding CreateTemporaryTypeBinding(Term typeSpecTerm)
        {
            TemporaryTypeBinding tempTypeBind = new TemporaryTypeBinding();
            Term cursorTerm = typeSpecTerm;
            Int32 propNum = 0;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term propTypeTerm = cursorTerm.getArgument(1);
                DbTypeCode typeCode = DbTypeCode.Object;
                String propType = null;
                if (propTypeTerm.Name == "binary")
                {
                    typeCode = DbTypeCode.Binary;
                }
                else if (propTypeTerm.Name == "boolean")
                {
                    typeCode = DbTypeCode.Boolean;
                }
                else if (propTypeTerm.Name == "datetime")
                {
                    typeCode = DbTypeCode.DateTime;
                }
                else if (propTypeTerm.Name == "decimal")
                {
                    typeCode = DbTypeCode.Decimal;
                }
                else if (propTypeTerm.Name == "double")
                {
                    typeCode = DbTypeCode.Double;
                }
                else if (propTypeTerm.Name == "integer")
                {
                    typeCode = DbTypeCode.Int64;
                }
                else if (propTypeTerm.Name == "string")
                {
                    typeCode = DbTypeCode.String;
                }
                else if (propTypeTerm.Name == "uinteger")
                {
                    typeCode = DbTypeCode.UInt64;
                }
                else if (propTypeTerm.Name == "object" && propTypeTerm.Arity == 1)
                {
                    propType = propTypeTerm.getArgument(1).Name;
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect propTypeTerm: " + propTypeTerm);
                }
                if (typeCode != DbTypeCode.Object)
                {
                    tempTypeBind.AddTemporaryProperty(propNum.ToString(), typeCode);
                }
                else
                {
                    tempTypeBind.AddTemporaryProperty(propNum.ToString(), propType);
                }
                cursorTerm = cursorTerm.getArgument(2);
                propNum++;
            }
            return tempTypeBind;
        }

        private static void AddPropertyMappings(RowTypeBinding rowTypeBind, Term mapListTerm, VariableArray varArray)
        {
            if (!mapListTerm.List)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect mapListTerm:" + mapListTerm);
            }
            Term cursorTerm = mapListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term mapTerm = cursorTerm.getArgument(1);
                if (mapTerm.Name != "map" || mapTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect mapTerm: " + mapTerm);
                }
                Term nameTerm = mapTerm.getArgument(1);
                Term exprTerm = mapTerm.getArgument(2);
                if (!nameTerm.Atom)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect mapTerm: " + mapTerm);
                }
                String propName = nameTerm.Name;
                IValueExpression propExpr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                rowTypeBind.AddPropertyMapping(propName, propExpr);
                cursorTerm = cursorTerm.getArgument(2);
            }
        }

        private static IExecutionEnumerator CreateIndexScan(RowTypeBinding rowTypeBind, Term typeTerm, Term extNumTerm, Term pathTerm,
                                                            Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
        }

# if false // Creation of different index scan.

    private static IExecutionEnumerator CreateIndexScan(RowTypeBinding rowTypeBind, Term typeTerm, Term extNumTerm, Term pathTerm,
                                                        Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        if (typeTerm.Name == "string")
        {
            return CreateStringIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        if (typeTerm.Name == "object")
        {
            return CreateObjectIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        if (typeTerm.Name == "integer")
        {
            return CreateIntegerIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        if (typeTerm.Name == "uinteger")
        {
            return CreateUIntegerIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        if (typeTerm.Name == "datetime")
        {
            return CreateDateTimeIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        if (typeTerm.Name == "binary")
        {
            return CreateBinaryIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        if (typeTerm.Name == "boolean")
        {
            return CreateBooleanIndexScan(rowTypeBind, extNumTerm, pathTerm, sortOrderTerm, handleTerm, pointListTerm, condTerm, varArray);
        }
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
    }

    private static StringIndexScan CreateStringIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                         Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        StringDynamicRange dynamicRange = CreateStringDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new StringIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

    private static ObjectIndexScan CreateObjectIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                         Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        ObjectDynamicRange dynamicRange = CreateObjectDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new ObjectIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

    private static IntegerIndexScan CreateIntegerIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                           Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        IntegerDynamicRange dynamicRange = CreateIntegerDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new IntegerIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

    private static UIntegerIndexScan CreateUIntegerIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                             Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        UIntegerDynamicRange dynamicRange = CreateUIntegerDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new UIntegerIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

    private static DateTimeIndexScan CreateDateTimeIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                             Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        DateTimeDynamicRange dynamicRange = CreateDateTimeDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new DateTimeIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

    private static BinaryIndexScan CreateBinaryIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                         Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        BinaryDynamicRange dynamicRange = CreateBinaryDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new BinaryIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

    private static BooleanIndexScan CreateBooleanIndexScan(RowTypeBinding rowTypeBind, Term extNumTerm, Term pathTerm,
                                                           Term sortOrderTerm, Term handleTerm, Term pointListTerm, Term condTerm, VariableArray varArray)
    {
        // Extent number
        if (!extNumTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        }
        Int32 extNum = extNumTerm.intValue();
        // Path
        String path = pathTerm.Name;
        // SortOrder
        SortOrdering sortOrder = CreateSortOrdering(sortOrderTerm);
        // Index handle
        if (!handleTerm.Integer)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect handleTerm: " + handleTerm);
        }
        Int64 handle = handleTerm.longValue();
        // Dynamic range.
        BooleanDynamicRange dynamicRange = CreateBooleanDynamicRange(rowTypeBind, pointListTerm, varArray);
        // Condition.
        ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        return new BooleanIndexScan(rowTypeBind, extNum, handle, path, dynamicRange, cond, sortOrder);
    }

# endif

        private static StringDynamicRange CreateStringDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            StringDynamicRange dynamicRange = new StringDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is IStringExpression)
                {
                    StringRangePoint rangePoint = new StringRangePoint(op, expr as IStringExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        private static ObjectDynamicRange CreateObjectDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            ObjectDynamicRange dynamicRange = new ObjectDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is IObjectExpression)
                {
                    ObjectRangePoint rangePoint = new ObjectRangePoint(op, expr as IObjectExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        private static IntegerDynamicRange CreateIntegerDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            IntegerDynamicRange dynamicRange = new IntegerDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is INumericalExpression)
                {
                    NumericalRangePoint rangePoint = new NumericalRangePoint(op, expr as INumericalExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        private static UIntegerDynamicRange CreateUIntegerDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            UIntegerDynamicRange dynamicRange = new UIntegerDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is INumericalExpression)
                {
                    NumericalRangePoint rangePoint = new NumericalRangePoint(op, expr as INumericalExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        private static DateTimeDynamicRange CreateDateTimeDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            DateTimeDynamicRange dynamicRange = new DateTimeDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is IDateTimeExpression)
                {
                    DateTimeRangePoint rangePoint = new DateTimeRangePoint(op, expr as IDateTimeExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        private static BinaryDynamicRange CreateBinaryDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            BinaryDynamicRange dynamicRange = new BinaryDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is IBinaryExpression)
                {
                    BinaryRangePoint rangePoint = new BinaryRangePoint(op, expr as IBinaryExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        private static BooleanDynamicRange CreateBooleanDynamicRange(RowTypeBinding rowTypeBind, Term pointListTerm, VariableArray varArray)
        {
            BooleanDynamicRange dynamicRange = new BooleanDynamicRange();
            Term cursorTerm = pointListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term pointTerm = cursorTerm.getArgument(1);
                if (pointTerm.Name != "point" || pointTerm.Arity != 2)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pointTerm: " + pointTerm);
                }
                Term opTerm = pointTerm.getArgument(1);
                Term exprTerm = pointTerm.getArgument(2);
                ComparisonOperator op = CreateComparisonOperator(opTerm);
                IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
                if (expr is IBooleanExpression)
                {
                    BooleanRangePoint rangePoint = new BooleanRangePoint(op, expr as IBooleanExpression);
                    dynamicRange.AddRangePoint(rangePoint);
                }
                else
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
                }
                cursorTerm = cursorTerm.getArgument(2);
            }
            return dynamicRange;
        }

        //private static FullTableScan CreateFullTableScan(RowTypeBinding rowTypeBind,
        //                                                 Term extNumTerm, Term condTerm,
        //                                                 VariableArray varArray,
        //                                                 String query)
        //{
        //    if (!extNumTerm.Integer)
        //    {
        //        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        //    }
        //    Int32 extNum = extNumTerm.intValue();
        //    ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        //    return new FullTableScan(rowTypeBind, extNum, 0, cond, SortOrder.Ascending, null, varArray, query);
        //}

        //private static Join CreateJoin(RowTypeBinding rowTypeBind,
        //                               Term joinTypeTerm,
        //                               Term enumTerm1,
        //                               Term enumTerm2,
        //                               VariableArray varArray,
        //                               String query)
        //{
        //    JoinType joinType = CreateJoinType(joinTypeTerm);
        //    IExecutionEnumerator enum1 = CreateEnumerator(rowTypeBind, enumTerm1, varArray, query);
        //    IExecutionEnumerator enum2 = CreateEnumerator(rowTypeBind, enumTerm2, varArray, query);
        //    return new Join(rowTypeBind, joinType, enum1, enum2, varArray, query);
        //}

        private static JoinType CreateJoinType(Term term)
        {
            if (term.Name == "inner" || term.Name == "cross")
            {
                return JoinType.Inner;
            }
            if (term.Name == "left")
            {
                return JoinType.LeftOuter;
            }
            if (term.Name == "right")
            {
                return JoinType.RightOuter;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }

        //private static ReferenceLookup CreateReferenceLookup(RowTypeBinding rowTypeBind, Term extNumTerm, Term exprTerm, Term condTerm,
        //    VariableArray varArray, String query)
        //{
        //    if (!extNumTerm.Integer)
        //    {
        //        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
        //    }
        //    Int32 extNum = extNumTerm.intValue();
        //    ITypeExpression expr = CreateTypeExpression(rowTypeBind, exprTerm, varArray);
        //    IObjectExpression objExpr;
        //    if (expr is IObjectExpression)
        //    {
        //        objExpr = expr as IObjectExpression;
        //    }
        //    else
        //    {
        //        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr: " + expr);
        //    }
        //    ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        //    return new ReferenceLookup(rowTypeBind, extNum, objExpr, cond, varArray, query);
        //}

        //private static Sort CreateSort(RowTypeBinding rowTypeBind, Term enumTerm, Term compListTerm, VariableArray varArray, String query)
        //{
        //    IExecutionEnumerator inEnum = CreateEnumerator(rowTypeBind, enumTerm, varArray, query);
        //    IQueryComparer comparer = CreateComparer(rowTypeBind, compListTerm, varArray);
        //    return new Sort(rowTypeBind, inEnum, comparer, varArray, query);
        //}

        //private static Aggregation CreateAggregation(RowTypeBinding rowTypeBind, Term extNumTerm, Term enumTerm, Term compListTerm,
        //    Term setFuncListTerm, Term condTerm, VariableArray varArray, String query)
        //{
        //    if (!extNumTerm.Integer)
        //    {
        //        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extent number: " + extNumTerm);
        //    }
        //    Int32 extNum = extNumTerm.intValue();
        //    IExecutionEnumerator inEnum = CreateEnumerator(rowTypeBind, enumTerm, varArray, query);
        //    // compListTerm represents group-by columns.
        //    MultiComparer comparer = CreateMultiComparer(rowTypeBind, compListTerm, varArray);
        //    // setFuncListTerm represents set-function currentLogExprList.
        //    List<ISetFunction> setFuncList = CreateSetFunctionList(rowTypeBind, setFuncListTerm, varArray);
        //    // condTerm represents having condition.
        //    ILogicalExpression cond = CreateLogicalExpression(rowTypeBind, condTerm, varArray);
        //    return new Aggregation(rowTypeBind, extNum, inEnum, comparer, setFuncList, cond, varArray, query);
        //}

        private static ILogicalExpression CreateLogicalExpression(RowTypeBinding rowTypeBind, Term term, VariableArray varArray)
        {
            if (term.Name == "literal" && term.Arity == 2 && term.getArgument(1).Name == "logical")
            {
                return CreateLogicalLiteral(term.getArgument(2));
            }
            // else
            if (term.Name == "operation" && term.Arity == 4 && term.getArgument(1).Name == "logical")
            {
                return CreateLogicalOperation(rowTypeBind, term.getArgument(2), term.getArgument(3), term.getArgument(4), varArray);
            }
            // else
            if (term.Name == "operation" && term.Arity == 3 && term.getArgument(1).Name == "logical")
            {
                return CreateLogicalOperation(rowTypeBind, term.getArgument(2), term.getArgument(3), varArray);
            }
            // else
            if (term.Name == "comparison" && term.Arity == 4)
            {
                return CreateComparison(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), term.getArgument(4), varArray);
            }
            // else
            if (term.Name == "comparison" && term.Arity == 5)
            {
                return CreateComparison(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), term.getArgument(4),
                                        term.getArgument(5), varArray);
            }
            // else
            if (term.Name == "isTypePredicate" && term.Arity == 3)
            {
                return CreateIsTypePredicate(rowTypeBind, term.getArgument(1), term.getArgument(2), term.getArgument(3), varArray);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }

        private static ILogicalExpression CreateIsTypePredicate(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2, VariableArray varArray)
        {
            ComparisonOperator compOp = CreateComparisonOperator(opTerm);

            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            if ((expr1 is IObjectExpression) == false)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr1: " + expr1);

            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if ((expr2 is ITypeExpression) == false)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect expr2: " + expr2);

            return new IsTypePredicate(compOp, expr1 as IObjectExpression, expr2 as ITypeExpression);
        }

        private static ILogicalExpression CreateComparison(RowTypeBinding rowTypeBind, Term typeTerm, Term opTerm, Term exprTerm1,
                                                           Term exprTerm2, VariableArray varArray)
        {
            if (typeTerm.Name == "binary")
            {
                return CreateComparisonBinary(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "boolean")
            {
                return CreateComparisonBoolean(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "datetime")
            {
                return CreateComparisonDateTime(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "decimal")
            {
                return CreateComparisonDecimal(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "double")
            {
                return CreateComparisonDouble(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "integer")
            {
                return CreateComparisonInteger(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "numerical")
            {
                return CreateComparisonNumerical(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "object")
            {
                return CreateComparisonObject(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "string")
            {
                return CreateComparisonString(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "uinteger")
            {
                return CreateComparisonUInteger(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static ILogicalExpression CreateComparison(RowTypeBinding rowTypeBind, Term typeTerm, Term opTerm, Term exprTerm1,
                                                           Term exprTerm2, Term exprTerm3, VariableArray varArray)
        {
            if (typeTerm.Name == "string")
            {
                return CreateComparisonString(rowTypeBind, opTerm, exprTerm1, exprTerm2, exprTerm3, varArray);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static LogicalLiteral CreateLogicalLiteral(Term literalTerm)
        {
            if (literalTerm.Name == "true")
            {
                return new LogicalLiteral(TruthValue.TRUE);
            }
            // else
            if (literalTerm.Name == "false")
            {
                return new LogicalLiteral(TruthValue.FALSE);
            }
            // else
            if (literalTerm.Name == "unknown")
            {
                return new LogicalLiteral(TruthValue.UNKNOWN);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect literalTerm: " + literalTerm);
        }

        private static LogicalOperation CreateLogicalOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               VariableArray varArray)
        {
            LogicalOperator op = CreateLogicalOperator(opTerm);
            ILogicalExpression expr1 = CreateLogicalExpression(rowTypeBind, exprTerm1, varArray);
            ILogicalExpression expr2 = CreateLogicalExpression(rowTypeBind, exprTerm2, varArray);
            return new LogicalOperation(op, expr1, expr2);
        }

        private static LogicalOperation CreateLogicalOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm, VariableArray varArray)
        {
            LogicalOperator op = CreateLogicalOperator(opTerm);
            ILogicalExpression expr = CreateLogicalExpression(rowTypeBind, exprTerm, varArray);
            return new LogicalOperation(op, expr);
        }

        private static ComparisonBinary CreateComparisonBinary(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IBinaryExpression && expr2 is IBinaryExpression)
            {
                return new ComparisonBinary(op, expr1 as IBinaryExpression, expr2 as IBinaryExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonBoolean CreateComparisonBoolean(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                                 VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IBooleanExpression && expr2 is IBooleanExpression)
            {
                return new ComparisonBoolean(op, expr1 as IBooleanExpression, expr2 as IBooleanExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonDateTime CreateComparisonDateTime(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                                   VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IDateTimeExpression && expr2 is IDateTimeExpression)
            {
                return new ComparisonDateTime(op, expr1 as IDateTimeExpression, expr2 as IDateTimeExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonDecimal CreateComparisonDecimal(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                                 VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IDecimalExpression && expr2 is IDecimalExpression)
            {
                return new ComparisonDecimal(op, expr1 as IDecimalExpression, expr2 as IDecimalExpression);
            }
            if (expr1 is IDecimalExpression && expr2 is IIntegerExpression)
            {
                return new ComparisonDecimal(op, expr1 as IDecimalExpression, expr2 as IIntegerExpression);
            }
            if (expr1 is IDecimalExpression && expr2 is IUIntegerExpression)
            {
                return new ComparisonDecimal(op, expr1 as IDecimalExpression, expr2 as IUIntegerExpression);
            }
            if (expr1 is IIntegerExpression && expr2 is IDecimalExpression)
            {
                return new ComparisonDecimal(op, expr1 as IIntegerExpression, expr2 as IDecimalExpression);
            }
            if (expr1 is IIntegerExpression && expr2 is IUIntegerExpression)
            {
                return new ComparisonDecimal(op, expr1 as IIntegerExpression, expr2 as IUIntegerExpression);
            }
            if (expr1 is IUIntegerExpression && expr2 is IDecimalExpression)
            {
                return new ComparisonDecimal(op, expr1 as IUIntegerExpression, expr2 as IDecimalExpression);
            }
            if (expr1 is IUIntegerExpression && expr2 is IIntegerExpression)
            {
                return new ComparisonDecimal(op, expr1 as IUIntegerExpression, expr2 as IIntegerExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonDouble CreateComparisonDouble(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IDoubleExpression && expr2 is IDoubleExpression)
            {
                return new ComparisonDouble(op, expr1 as IDoubleExpression, expr2 as IDoubleExpression);
            }
            if (expr1 is IDoubleExpression && expr2 is IDecimalExpression)
            {
                return new ComparisonDouble(op, expr1 as IDoubleExpression, expr2 as IDecimalExpression);
            }
            if (expr1 is IDoubleExpression && expr2 is IIntegerExpression)
            {
                return new ComparisonDouble(op, expr1 as IDoubleExpression, expr2 as IIntegerExpression);
            }
            if (expr1 is IDoubleExpression && expr2 is IUIntegerExpression)
            {
                return new ComparisonDouble(op, expr1 as IDoubleExpression, expr2 as IUIntegerExpression);
            }
            if (expr1 is IDecimalExpression && expr2 is IDoubleExpression)
            {
                return new ComparisonDouble(op, expr1 as IDecimalExpression, expr2 as IDoubleExpression);
            }
            if (expr1 is IIntegerExpression && expr2 is IDoubleExpression)
            {
                return new ComparisonDouble(op, expr1 as IIntegerExpression, expr2 as IDoubleExpression);
            }
            if (expr1 is IUIntegerExpression && expr2 is IDoubleExpression)
            {
                return new ComparisonDouble(op, expr1 as IUIntegerExpression, expr2 as IDoubleExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonInteger CreateComparisonInteger(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                                 VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IIntegerExpression && expr2 is IIntegerExpression)
            {
                return new ComparisonInteger(op, expr1 as IIntegerExpression, expr2 as IIntegerExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonNumerical CreateComparisonNumerical(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is INumericalExpression && expr2 is INumericalExpression)
            {
                return new ComparisonNumerical(op, expr1 as INumericalExpression, expr2 as INumericalExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonObject CreateComparisonObject(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IObjectExpression && expr2 is IObjectExpression)
            {
                return new ComparisonObject(op, expr1 as IObjectExpression, expr2 as IObjectExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static ComparisonString CreateComparisonString(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);

            //if (op == ComparisonOperator.LIKEdynamic)
            //{
            //    if (ExtentSet.IncludesNoExtentReference(expr2))
            //        op = ComparisonOperator.LIKEstatic;
            //}

            if (expr1 is IStringExpression && expr2 is IStringExpression)
            {
                return new ComparisonString(op, expr1 as IStringExpression, expr2 as IStringExpression);
            }

            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        //private static Boolean IsStringLiteralStartsWith(ITypeExpression expr)
        //{
        //    if (!(expr is StringLiteral))
        //        return false;

        //    String value = (expr as StringLiteral).EvaluateToString(null);
        //    if (value != null && value.IndexOf('_') == -1 && value.IndexOf('%') == value.Length - 1)
        //        return true;

        //    return false;
        //}

        private static ComparisonString CreateComparisonString(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               Term exprTerm3, VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            // TODO: Replace this temporary fix for QueryFlag with something better.
            QueryFlags flags = varArray.QueryFlags;
            IValueExpression expr3 = CreateValueExpression(rowTypeBind, exprTerm3, varArray);
            varArray.QueryFlags = flags;

            if (op == ComparisonOperator.LIKEdynamic)
            {
                if (expr2 is StringVariable && expr3 is StringLiteral && String.IsNullOrEmpty((expr3 as StringLiteral).EvaluateToString(null)))
                    varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesLIKEvariable;

                if (ExtentSet.IncludesNoExtentReference(expr2) && ExtentSet.IncludesNoExtentReference(expr3))
                    op = ComparisonOperator.LIKEstatic;
            }

            if (expr1 is IStringExpression && expr2 is IStringExpression && expr3 is IStringExpression)
            {
                return new ComparisonString(op, expr1 as IStringExpression, expr2 as IStringExpression, expr3 as IStringExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + ", " + expr2.DbTypeCode + " and " +
                                  expr3.DbTypeCode);
        }

        private static ComparisonUInteger CreateComparisonUInteger(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                                   VariableArray varArray)
        {
            ComparisonOperator op = CreateComparisonOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IUIntegerExpression && expr2 is IUIntegerExpression)
            {
                return new ComparisonUInteger(op, expr1 as IUIntegerExpression, expr2 as IUIntegerExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect types: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static LogicalOperator CreateLogicalOperator(Term term)
        {
            if (term.Name == "and")
            {
                return LogicalOperator.AND;
            }
            if (term.Name == "is")
            {
                return LogicalOperator.IS;
            }
            if (term.Name == "not")
            {
                return LogicalOperator.NOT;
            }
            if (term.Name == "or")
            {
                return LogicalOperator.OR;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }

        private static ComparisonOperator CreateComparisonOperator(Term term)
        {
            if (term.Name == "equal")
            {
                return ComparisonOperator.Equal;
            }
            // else
            if (term.Name == "greaterThan")
            {
                return ComparisonOperator.GreaterThan;
            }
            // else
            if (term.Name == "greaterThanOrEqual")
            {
                return ComparisonOperator.GreaterThanOrEqual;
            }
            // else
            if (term.Name == "lessThan")
            {
                return ComparisonOperator.LessThan;
            }
            // else
            if (term.Name == "lessThanOrEqual")
            {
                return ComparisonOperator.LessThanOrEqual;
            }
            // else
            if (term.Name == "notEqual")
            {
                return ComparisonOperator.NotEqual;
            }
            // else
            if (term.Name == "is")
            {
                return ComparisonOperator.IS;
            }
            // else
            if (term.Name == "isNot")
            {
                return ComparisonOperator.ISNOT;
            }
            // else
            if (term.Name == "like")
            {
                return ComparisonOperator.LIKEdynamic;
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }

        private static List<ISetFunction> CreateSetFunctionList(RowTypeBinding rowTypeBind, Term setFuncListTerm, VariableArray varArray)
        {
            if (!setFuncListTerm.List)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncListTerm:" + setFuncListTerm);
            }
            List<ISetFunction> setFunctionList = new List<ISetFunction>();
            Term cursorTerm = setFuncListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term setFuncTerm = cursorTerm.getArgument(1);
                if (setFuncTerm.Name != "setFunction" || setFuncTerm.Arity != 4)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncTerm: " + setFuncTerm);
                }
                ISetFunction setFunc = CreateSetFunction(rowTypeBind, setFuncTerm.getArgument(1), setFuncTerm.getArgument(2),
                                                         setFuncTerm.getArgument(3), setFuncTerm.getArgument(4), varArray);
                setFunctionList.Add(setFunc);
                cursorTerm = cursorTerm.getArgument(2);
            }
            return setFunctionList;
        }

        private static ISetFunction CreateSetFunction(RowTypeBinding rowTypeBind, Term typeTerm, Term setFuncTypeTerm, Term quantTerm,
                                                      Term exprTerm, VariableArray varArray)
        {
            if (typeTerm.Name == "binary")
            {
                return CreateBinarySetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "boolean")
            {
                return CreateBooleanSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "datetime")
            {
                return CreateDateTimeSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "decimal")
            {
                return CreateDecimalSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "double")
            {
                return CreateDoubleSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "integer")
            {
                return CreateIntegerSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "numerical")
            {
                return CreateNumericalSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "object")
            {
                return CreateObjectSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "uinteger")
            {
                return CreateUIntegerSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "string")
            {
                return CreateStringSetFunction(rowTypeBind, setFuncTypeTerm, quantTerm, exprTerm, varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static BinarySetFunction CreateBinarySetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                 Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IBinaryExpression)
            {
                return new BinarySetFunction(setFuncType, expr as IBinaryExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static BooleanSetFunction CreateBooleanSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                   Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IBooleanExpression)
            {
                return new BooleanSetFunction(setFuncType, expr as IBooleanExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static DateTimeSetFunction CreateDateTimeSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IDateTimeExpression)
            {
                return new DateTimeSetFunction(setFuncType, expr as IDateTimeExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static DecimalSetFunction CreateDecimalSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                   Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (setFuncType == SetFunctionType.COUNT)
            {
                return new DecimalSetFunction(expr);
            }
            if (expr is IDecimalExpression)
            {
                return new DecimalSetFunction(setFuncType, expr as IDecimalExpression);
            }
            if (expr is IIntegerExpression)
            {
                return new DecimalSetFunction(setFuncType, expr as IIntegerExpression);
            }
            if (expr is IUIntegerExpression)
            {
                return new DecimalSetFunction(setFuncType, expr as IUIntegerExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static DoubleSetFunction CreateDoubleSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                 Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IDoubleExpression)
            {
                return new DoubleSetFunction(setFuncType, expr as IDoubleExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static IntegerSetFunction CreateIntegerSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                   Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IIntegerExpression)
            {
                return new IntegerSetFunction(setFuncType, expr as IIntegerExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static NumericalSetFunction CreateNumericalSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is INumericalExpression)
            {
                return new NumericalSetFunction(setFuncType, expr as INumericalExpression);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static ObjectSetFunction CreateObjectSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                 Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IObjectExpression)
            {
                return new ObjectSetFunction(setFuncType, expr as IObjectExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static StringSetFunction CreateStringSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                                                                 Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IStringExpression)
            {
                return new StringSetFunction(setFuncType, expr as IStringExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static UIntegerSetFunction CreateUIntegerSetFunction(RowTypeBinding rowTypeBind, Term setFuncTypeTerm, Term quantTerm,
                Term exprTerm, VariableArray varArray)
        {
            SetFunctionType setFuncType = CreateSetFunctionType(setFuncTypeTerm);
            if (quantTerm.Name != "all")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect quantTerm: " + quantTerm);
            }
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IUIntegerExpression)
            {
                return new UIntegerSetFunction(setFuncType, expr as IUIntegerExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static SetFunctionType CreateSetFunctionType(Term term)
        {
            if (term == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term.");
            }
            if (term.Name == "avg")
            {
                return SetFunctionType.AVG;
            }
            if (term.Name == "count")
            {
                return SetFunctionType.COUNT;
            }
            if (term.Name == "max")
            {
                return SetFunctionType.MAX;
            }
            if (term.Name == "min")
            {
                return SetFunctionType.MIN;
            }
            if (term.Name == "sum")
            {
                return SetFunctionType.SUM;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }

        private static IQueryComparer CreateComparer(RowTypeBinding rowTypeBind, Term comparerListTerm, VariableArray varArray)
        {
            if (!comparerListTerm.List || comparerListTerm.Name == "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparerListTerm:" + comparerListTerm);
            }
            Term comparerTerm = comparerListTerm.getArgument(1);
            Term listTerm = comparerListTerm.getArgument(2);
            if (listTerm.Name != "[]")
            {
                return CreateMultiComparer(rowTypeBind, comparerListTerm, varArray);
            }
            if (comparerTerm.Name == "random")
            {
                return new RandomComparer();
            }
            if (comparerTerm.Name == "sortSpec" && comparerTerm.Arity == 3)
                return CreateSingleComparer(rowTypeBind, comparerTerm.getArgument(1), comparerTerm.getArgument(2), comparerTerm.getArgument(3),
                                            varArray);
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparerTerm: " + comparerTerm);
        }

        private static ISingleComparer CreateSingleComparer(RowTypeBinding rowTypeBind, Term typeTerm, Term exprTerm, Term sortOrdTerm,
                                                            VariableArray varArray)
        {
            if (typeTerm.Name == "binary")
            {
                return CreateBinaryComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "boolean")
            {
                return CreateBooleanComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "datetime")
            {
                return CreateDateTimeComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "decimal")
            {
                return CreateDecimalComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "double")
            {
                return CreateDoubleComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "integer")
            {
                return CreateIntegerComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "string")
            {
                return Starcounter.Query.Sql.Creator.CreateStringComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "object")
            {
                return CreateObjectComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            if (typeTerm.Name == "uinteger")
            {
                return CreateUIntegerComparer(rowTypeBind, exprTerm, sortOrdTerm, varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static MultiComparer CreateMultiComparer(RowTypeBinding rowTypeBind, Term comparerListTerm, VariableArray varArray)
        {
            if (!comparerListTerm.List)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparerListTerm: " + comparerListTerm);
            }
            MultiComparer multiComparer = new MultiComparer();
            Term cursorTerm = comparerListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term comparerTerm = cursorTerm.getArgument(1);
                if (comparerTerm.Name != "sortSpec" || comparerTerm.Arity != 3)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparerTerm: " + comparerTerm);
                }
                ISingleComparer comparer = CreateSingleComparer(rowTypeBind, comparerTerm.getArgument(1), comparerTerm.getArgument(2),
                                                                comparerTerm.getArgument(3), varArray);
                multiComparer.AddComparer(comparer);
                cursorTerm = cursorTerm.getArgument(2);
            }
            return multiComparer;
        }

        private static SortSpecification CreateSortSpecification(RowTypeBinding rowTypeBind, Term comparerListTerm, VariableArray varArray)
        {
            List<ISingleComparer> comparerList = CreateSingleComparerList(rowTypeBind, comparerListTerm, varArray);
            return new SortSpecification(rowTypeBind, comparerList);
        }

        private static List<ISingleComparer> CreateSingleComparerList(RowTypeBinding rowTypeBind, Term comparerListTerm, VariableArray varArray)
        {
            if (!comparerListTerm.List)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparerListTerm: " + comparerListTerm);
            }
            List<ISingleComparer> comparerList = new List<ISingleComparer>();
            Term cursorTerm = comparerListTerm;
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                Term comparerTerm = cursorTerm.getArgument(1);
                if (comparerTerm.Name != "sortSpec" || comparerTerm.Arity != 3)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparerTerm: " + comparerTerm);
                }
                ISingleComparer comparer = CreateSingleComparer(rowTypeBind, comparerTerm.getArgument(1), comparerTerm.getArgument(2),
                                                                comparerTerm.getArgument(3), varArray);
                comparerList.Add(comparer);
                cursorTerm = cursorTerm.getArgument(2);
            }
            return comparerList;
        }

        private static BinaryComparer CreateBinaryComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is BinaryProperty)
            {
                return new BinaryPathComparer(expr as BinaryProperty, sortOrd);
            }
            if (expr is BinaryPath)
            {
                return new BinaryPathComparer(expr as BinaryPath, sortOrd);
            }
            if (expr is IBinaryExpression)
            {
                return new BinaryComparer(expr as IBinaryExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static BooleanComparer CreateBooleanComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is BooleanProperty)
            {
                return new BooleanPathComparer(expr as BooleanProperty, sortOrd);
            }
            if (expr is BooleanPath)
            {
                return new BooleanPathComparer(expr as BooleanPath, sortOrd);
            }
            if (expr is IBooleanExpression)
            {
                return new BooleanComparer(expr as IBooleanExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static DateTimeComparer CreateDateTimeComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is DateTimeProperty)
            {
                return new DateTimePathComparer(expr as DateTimeProperty, sortOrd);
            }
            if (expr is DateTimePath)
            {
                return new DateTimePathComparer(expr as DateTimePath, sortOrd);
            }
            if (expr is IDateTimeExpression)
            {
                return new DateTimeComparer(expr as IDateTimeExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static DecimalComparer CreateDecimalComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is DecimalProperty)
            {
                return new DecimalPathComparer(expr as DecimalProperty, sortOrd);
            }
            if (expr is DecimalPath)
            {
                return new DecimalPathComparer(expr as DecimalPath, sortOrd);
            }
            if (expr is IDecimalExpression)
            {
                return new DecimalComparer(expr as IDecimalExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static DoubleComparer CreateDoubleComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is DoubleProperty)
            {
                return new DoublePathComparer(expr as DoubleProperty, sortOrd);
            }
            if (expr is DoublePath)
            {
                return new DoublePathComparer(expr as DoublePath, sortOrd);
            }
            if (expr is IDoubleExpression)
            {
                return new DoubleComparer(expr as IDoubleExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static IntegerComparer CreateIntegerComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IntegerProperty)
            {
                return new IntegerPathComparer(expr as IntegerProperty, sortOrd);
            }
            if (expr is IntegerPath)
            {
                return new IntegerPathComparer(expr as IntegerPath, sortOrd);
            }
            if (expr is IIntegerExpression)
            {
                return new IntegerComparer(expr as IIntegerExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static ObjectComparer CreateObjectComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is ObjectProperty)
            {
                return new ObjectPathComparer(expr as ObjectProperty, sortOrd);
            }
            if (expr is ObjectPath)
            {
                return new ObjectPathComparer(expr as ObjectPath, sortOrd);
            }
            if (expr is IObjectExpression)
            {
                return new ObjectComparer(expr as IObjectExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static Starcounter.Query.Execution.StringComparer CreateStringComparer(RowTypeBinding rowTypeBind, Term exprTerm,
                Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is StringProperty)
            {
                return new StringPathComparer(expr as StringProperty, sortOrd);
            }
            if (expr is StringPath)
            {
                return new StringPathComparer(expr as StringPath, sortOrd);
            }
            if (expr is IStringExpression)
            {
                return new Starcounter.Query.Execution.StringComparer(expr as IStringExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static UIntegerComparer CreateUIntegerComparer(RowTypeBinding rowTypeBind, Term exprTerm, Term sortOrdTerm, VariableArray varArray)
        {
            SortOrder sortOrd = CreateSortOrdering(sortOrdTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is UIntegerProperty)
            {
                return new UIntegerPathComparer(expr as UIntegerProperty, sortOrd);
            }
            if (expr is UIntegerPath)
            {
                return new UIntegerPathComparer(expr as UIntegerPath, sortOrd);
            }
            if (expr is IUIntegerExpression)
            {
                return new UIntegerComparer(expr as IUIntegerExpression, sortOrd);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of expression: " + expr.DbTypeCode);
        }

        private static SortOrder CreateSortOrdering(Term sortOrdTerm)
        {
            if (sortOrdTerm.Name == "asc")
            {
                return SortOrder.Ascending;
            }
            // else
            if (sortOrdTerm.Name == "desc")
            {
                return SortOrder.Descending;
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect sortOrdTerm: " + sortOrdTerm);
        }

        private static IValueExpression CreateValueExpression(RowTypeBinding rowTypeBind, Term exprTerm, VariableArray varArray)
        {
            // variable(Type,Number)
            if (exprTerm.Name == "variable" && exprTerm.Arity == 2)
            {
                return CreateVariable(exprTerm.getArgument(1), exprTerm.getArgument(2), varArray);
            }
            // literal(Type,Literal)
            if (exprTerm.Name == "literal" && exprTerm.Arity == 2)
            {
                ILiteral literal = CreateLiteral(exprTerm.getArgument(1), exprTerm.getArgument(2));
                if (!literal.EvaluatesToNull(null) && !(literal is TypeLiteral))
                    varArray.QueryFlags = varArray.QueryFlags | QueryFlags.IncludesLiteral;
                return literal;
            }
            // path(Type,ExtNum,Path)
            if (exprTerm.Name == "path" && exprTerm.Arity == 2)
            {
                return CreatePath(rowTypeBind, exprTerm.getArgument(2), varArray);
            }
            // operation(Type,Op,Expr1,Expr2)
            if (exprTerm.Name == "operation" && exprTerm.Arity == 4)
                return CreateOperation(rowTypeBind, exprTerm.getArgument(1), exprTerm.getArgument(2), exprTerm.getArgument(3),
                                       exprTerm.getArgument(4), varArray);
            // operation(Type,Op,Expr)
            if (exprTerm.Name == "operation" && exprTerm.Arity == 3)
            {
                return CreateOperation(rowTypeBind, exprTerm.getArgument(1), exprTerm.getArgument(2), exprTerm.getArgument(3), varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect exprTerm: " + exprTerm);
        }

        private static IVariable CreateVariable(Term typeTerm, Term numTerm, VariableArray varArray)
        {
            if (numTerm.Integer == false)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect numTerm: " + numTerm);
            }
            Int32 number = numTerm.intValue();

            if (typeTerm.Name == "any")
            {
                return CreateStringVariable(number, varArray);
            }
            if (typeTerm.Name == "binary")
            {
                return CreateBinaryVariable(number, varArray);
            }
            if (typeTerm.Name == "boolean")
            {
                return CreateBooleanVariable(number, varArray);
            }
            if (typeTerm.Name == "datetime")
            {
                return CreateDateTimeVariable(number, varArray);
            }
            if (typeTerm.Name == "numerical")
            {
                return CreateNumericalVariable(number, varArray);
            }
            if (typeTerm.Name == "object" && typeTerm.Arity == 1)
            {
                return CreateObjectVariable(typeTerm.getArgument(1), number, varArray);
            }
            if (typeTerm.Name == "string")
            {
                return CreateStringVariable(number, varArray);
            }
            if (typeTerm.Name == "type")
            {
                return CreateTypeVariable(number, varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static BinaryVariable CreateBinaryVariable(Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                BinaryVariable binVariable = new BinaryVariable(number);
                varArray.SetElement(number, binVariable);
                return binVariable;
            }
            if (variable is BinaryVariable)
            {
                return variable as BinaryVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static BooleanVariable CreateBooleanVariable(Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                BooleanVariable blnVariable = new BooleanVariable(number);
                varArray.SetElement(number, blnVariable);
                return blnVariable;
            }
            if (variable is BooleanVariable)
            {
                return variable as BooleanVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static DateTimeVariable CreateDateTimeVariable(Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                DateTimeVariable dtmVariable = new DateTimeVariable(number);
                varArray.SetElement(number, dtmVariable);
                return dtmVariable;
            }
            if (variable is DateTimeVariable)
            {
                return variable as DateTimeVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static NumericalVariable CreateNumericalVariable(Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                NumericalVariable numVariable = new NumericalVariable(number);
                varArray.SetElement(number, numVariable);
                return numVariable;
            }
            if (variable is NumericalVariable)
            {
                return variable as NumericalVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static ObjectVariable CreateObjectVariable(Term typeTerm, Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                ObjectVariable objVariable = new ObjectVariable(number, TypeRepository.GetTypeBinding(typeTerm.Name));
                varArray.SetElement(number, objVariable);
                return objVariable;
            }
            if (variable is ObjectVariable)
            {
                return variable as ObjectVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static StringVariable CreateStringVariable(Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                StringVariable strVariable = new StringVariable(number);
                varArray.SetElement(number, strVariable);
                return strVariable;
            }
            if (variable is StringVariable)
            {
                return variable as StringVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static TypeVariable CreateTypeVariable(Int32 number, VariableArray varArray)
        {
            IVariable variable = varArray.GetElement(number);
            if (variable == null)
            {
                TypeVariable typeVariable = new TypeVariable(number);
                varArray.SetElement(number, typeVariable);
                return typeVariable;
            }
            if (variable is TypeVariable)
            {
                return variable as TypeVariable;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        private static ILiteral CreateLiteral(Term typeTerm, Term nameTerm)
        {
            if (typeTerm.Name == "binary")
            {
                return CreateBinaryLiteral(nameTerm);
            }
            if (typeTerm.Name == "boolean")
            {
                return CreateBooleanLiteral(nameTerm);
            }
            if (typeTerm.Name == "datetime")
            {
                return CreateDateTimeLiteral(nameTerm);
            }
            if (typeTerm.Name == "decimal")
            {
                return CreateDecimalLiteral(nameTerm);
            }
            if (typeTerm.Name == "double")
            {
                return CreateDoubleLiteral(nameTerm);
            }
            if (typeTerm.Name == "integer")
            {
                return CreateIntegerLiteral(nameTerm);
            }
            if (typeTerm.Name == "object" && typeTerm.Arity == 1)
            {
                return CreateObjectLiteral(typeTerm.getArgument(1), nameTerm);
            }
            if (typeTerm.Name == "string")
            {
                return CreateStringLiteral(nameTerm);
            }
            if (typeTerm.Name == "uinteger")
            {
                return CreateUIntegerLiteral(nameTerm);
            }
            if (typeTerm.Name == "numerical") // Numerical null literal.
            {
                return CreateIntegerLiteral(nameTerm);
            }
            if (typeTerm.Name == "type")
            {
                return CreateTypeLiteral(nameTerm);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static Binary BinaryParse(String literal)
        {
            try
            {
                return Db.HexToBinary(literal);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect binary literal: " + literal, exception);
            }
        }

        private static BinaryLiteral CreateBinaryLiteral(Term literalTerm)
        {
            String literal = literalTerm.Name;
            Int32 length = literal.Length;
            literal = literal.Substring(1, length - 2);
            Nullable<Binary> value = null;
            if (literalTerm.Name != "null")
            {
                value = BinaryParse(literal);
            }
            return new BinaryLiteral(value);
        }

        private static Boolean BooleanParse(String literal)
        {
            try
            {
                return Boolean.Parse(literal);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect boolean literal: " + literal, exception);
            }
        }

        private static BooleanLiteral CreateBooleanLiteral(Term literalTerm)
        {
            Nullable<Boolean> value = null;
            if (literalTerm.Name != "null")
            {
                value = BooleanParse(literalTerm.Name);
            }
            return new BooleanLiteral(value);
        }

        private static DateTime DateTimeParse(String literal)
        {
            try
            {
                return DateTime.Parse(literal, DateTimeFormatInfo.InvariantInfo);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect datetime literal: " + literal, exception);
            }
        }

        private static DateTimeLiteral CreateDateTimeLiteral(Term literalTerm)
        {
            String literal = literalTerm.Name;
            Int32 length = literal.Length;
            literal = literal.Substring(1, length - 2);
            Nullable<DateTime> value = null;
            if (literalTerm.Name != "null")
            {
                value = DateTimeParse(literal);
            }
            return new DateTimeLiteral(value);
        }

        private static Decimal DecimalParse(String literal)
        {
            try
            {
                return Decimal.Parse(literal, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect decimal literal: " + literal, exception);
            }
        }

        private static DecimalLiteral CreateDecimalLiteral(Term literalTerm)
        {
            Nullable<Decimal> value = null;
            if (literalTerm.Name != "null")
            {
                value = DecimalParse(literalTerm.Name);
            }
            return new DecimalLiteral(value);
        }

        private static Double DoubleParse(String literal)
        {
            try
            {
                return Double.Parse(literal, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect double literal: " + literal, exception);
            }
        }

        private static DoubleLiteral CreateDoubleLiteral(Term literalTerm)
        {
            Nullable<Double> value = null;
            if (literalTerm.Name != "null")
            {
                value = DoubleParse(literalTerm.Name);
            }
            return new DoubleLiteral(value);
        }

        private static Int64 IntegerParse(String literal)
        {
            try
            {
                return Int64.Parse(literal, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect integer literal: " + literal, exception);
            }
        }

        private static IntegerLiteral CreateIntegerLiteral(Term literalTerm)
        {
            Nullable<Int64> value = null;
            if (literalTerm.Name != "null")
            {
                value = IntegerParse(literalTerm.Name);
            }
            return new IntegerLiteral(value);
        }

        private static ObjectLiteral CreateObjectLiteral(Term typeTerm, Term literalTerm)
        {
            if (literalTerm.Name != "null")
            {
                return new ObjectLiteral(UIntegerParse(literalTerm.Name)); // ObjectID
            }
            return new ObjectLiteral(TypeRepository.GetTypeBinding(typeTerm.Name));
        }

        private static StringLiteral CreateStringLiteral(Term literalTerm)
        {
            String value = null;
            if (literalTerm.Name != "null")
            {
                value = literalTerm.Name;
                value = value.Substring(1, value.Length - 2);
                value = value.Replace("''", "'");
            }
            return new StringLiteral(value);
        }

        private static UInt64 UIntegerParse(String literal)
        {
            try
            {
                return UInt64.Parse(literal, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException exception)
            {
                throw new SqlException("Incorrect uinteger literal: " + literal, exception);
            }
        }

        private static UIntegerLiteral CreateUIntegerLiteral(Term literalTerm)
        {
            Nullable<UInt64> value = null;
            if (literalTerm.Name != "null")
            {
                value = UIntegerParse(literalTerm.Name);
            }
            return new UIntegerLiteral(value);
        }

        private static TypeLiteral CreateTypeLiteral(Term literalTerm)
        {
            ITypeBinding value = null;
            if (literalTerm.Name != "null")
            {
                value = Starcounter.Binding.Bindings.GetTypeBinding(literalTerm.Name);
            }
            return new TypeLiteral(value);
        }

        private static IPath CreatePath(RowTypeBinding rowTypeBind, Term pathTerm, VariableArray varArray)
        {
            // path = [extent(Num,Type),...] or path = [cast(extent(Num,Type1),Type2),...].
            if (pathTerm.List == false || pathTerm.Name == "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pathTerm: " + pathTerm);
            }
            // Get extentTerm = extent(Num,Type1) and castTypeTerm = Type2.
            Term extentTerm = pathTerm.getArgument(1);
            Term castTypeTerm = null;
            if (extentTerm.Name == "cast" && extentTerm.Arity == 2)
            {
                castTypeTerm = extentTerm.getArgument(2);
                extentTerm = extentTerm.getArgument(1);
            }
            if (extentTerm.Name != "extent" || extentTerm.Arity != 2)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentTerm: " + extentTerm);
            }
            // Get extent number.
            Term extNumTerm = extentTerm.getArgument(1);
            if (extNumTerm.Integer == false)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
            }
            Int32 extNum = extNumTerm.intValue();
            // Cast of extent type, which requires an ObjectThis.
            ObjectCast objectCast = null;
            if (castTypeTerm != null)
            {
                objectCast = CreateObjectCast(castTypeTerm, CreateObjectThis(rowTypeBind, extNum));
            }
            // Analyze the rest of the path.
            Term cursorTerm = pathTerm.getArgument(2);
            // The rest of the path is empty (ObjectThis).
            if (cursorTerm.Name == "[]")
            {
                if (objectCast != null)
                {
                    return objectCast;
                }
                // else
                return CreateObjectThis(rowTypeBind, extNum);
            }
            // Create a path-list.
            List<IObjectPathItem> pathList = new List<IObjectPathItem>();
            // Add an eventual first cast path-item.
            if (objectCast != null)
            {
                pathList.Add(objectCast);
            }
            // Create first non-cast path-item.
            ITypeBinding tmpTypeBind;
            Int32 tmpExtNum;
            if (objectCast == null)
            {
                tmpTypeBind = rowTypeBind.GetTypeBinding(extNum);
                tmpExtNum = extNum;
            }
            else
            {
                tmpTypeBind = objectCast.TypeBinding;
                tmpExtNum = -1;
            }
            Term tmpItemTerm = cursorTerm.getArgument(1);
            IPath tmpItem = CreatePathItem(rowTypeBind, tmpExtNum, tmpTypeBind, tmpItemTerm, varArray);
            // Create the remaining path-items.
            cursorTerm = cursorTerm.getArgument(2);
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                if ((tmpItem is IObjectPathItem) == false)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect tmpItem: " + tmpItem);
                }
                pathList.Add(tmpItem as IObjectPathItem);
                tmpTypeBind = (tmpItem as IObjectPathItem).TypeBinding;
                tmpItemTerm = cursorTerm.getArgument(1);
                tmpItem = CreatePathItem(rowTypeBind, -1, tmpTypeBind, tmpItemTerm, varArray);
                cursorTerm = cursorTerm.getArgument(2);
            }
            if (cursorTerm.Name != "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cursorTerm: " + cursorTerm);
            }
            // Create result to return (a single item or a path of some type).
            if (pathList.Count == 0)
            {
                return tmpItem;
            }
            // else
            if (tmpItem is IBinaryPathItem)
            {
                return new BinaryPath(extNum, pathList, tmpItem as IBinaryPathItem);
            }
            // else
            if (tmpItem is IBooleanPathItem)
            {
                return new BooleanPath(extNum, pathList, tmpItem as IBooleanPathItem);
            }
            // else
            if (tmpItem is IDateTimePathItem)
            {
                return new DateTimePath(extNum, pathList, tmpItem as IDateTimePathItem);
            }
            // else
            if (tmpItem is IDecimalPathItem)
            {
                return new DecimalPath(extNum, pathList, tmpItem as IDecimalPathItem);
            }
            // else
            if (tmpItem is IDoublePathItem)
            {
                return new DoublePath(extNum, pathList, tmpItem as IDoublePathItem);
            }
            // else
            if (tmpItem is IIntegerPathItem)
            {
                return new IntegerPath(extNum, pathList, tmpItem as IIntegerPathItem);
            }
            // else
            if (tmpItem is IObjectPathItem)
            {
                return new ObjectPath(extNum, pathList, tmpItem as IObjectPathItem);
            }
            // else
            if (tmpItem is IStringPathItem)
            {
                return new StringPath(extNum, pathList, tmpItem as IStringPathItem);
            }
            // else
            if (tmpItem is IUIntegerPathItem)
            {
                return new UIntegerPath(extNum, pathList, tmpItem as IUIntegerPathItem);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect tmpItem: " + tmpItem);
        }

        private static IPath CreatePath_OLD(RowTypeBinding rowTypeBind, Term pathTerm, VariableArray varArray)
        {
            // path = [extent(Num,Type),...] or path = [cast(extent(Num,Type1),Type2),...].
            if (pathTerm.List == false || pathTerm.Name == "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect pathTerm: " + pathTerm);
            }
            // Get extentTerm = extent(Num,Type1) and castTypeTerm = Type2.
            Term extentTerm = pathTerm.getArgument(1);
            Term castTypeTerm = null;
            if (extentTerm.Name == "cast" && extentTerm.Arity == 2)
            {
                castTypeTerm = extentTerm.getArgument(2);
                extentTerm = extentTerm.getArgument(1);
            }
            if (extentTerm.Name != "extent" || extentTerm.Arity != 2)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentTerm: " + extentTerm);
            }
            // Get extent number.
            Term extNumTerm = extentTerm.getArgument(1);
            if (extNumTerm.Integer == false)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extNumTerm: " + extNumTerm);
            }
            Int32 extNum = extNumTerm.intValue();
            // Cast of extent type, which requires an ObjectThis.
            ObjectCast objectCast = null;
            if (castTypeTerm != null)
            {
                objectCast = CreateObjectCast(castTypeTerm, CreateObjectThis(rowTypeBind, extNum));
            }
            // Analyze the rest of the path.
            Term cursorTerm = pathTerm.getArgument(2);
            // The rest of the path is empty (ObjectThis).
            if (cursorTerm.Name == "[]")
            {
                if (objectCast != null)
                {
                    return objectCast;
                }
                // else
                return CreateObjectThis(rowTypeBind, extNum);
            }
            // Create a path-list.
            List<IObjectPathItem> pathList = new List<IObjectPathItem>();
            if (objectCast != null)
            {
                pathList.Add(objectCast);
            }
            // Create first path-item.
            ITypeBinding tmpTypeBind;
            if (objectCast == null)
            {
                tmpTypeBind = rowTypeBind.GetTypeBinding(extNum);
            }
            else
            {
                tmpTypeBind = objectCast.TypeBinding;
            }
            Term tmpItemTerm = cursorTerm.getArgument(1);
            IPath tmpItem = CreatePathItem(rowTypeBind, extNum, tmpTypeBind, tmpItemTerm, varArray);
            // Create the remaining path-items.
            cursorTerm = cursorTerm.getArgument(2);
            while (cursorTerm.List && cursorTerm.Name != "[]")
            {
                if ((tmpItem is IObjectPathItem) == false)
                {
                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect tmpItem: " + tmpItem);
                }
                pathList.Add(tmpItem as IObjectPathItem);
                tmpTypeBind = (tmpItem as IObjectPathItem).TypeBinding;
                tmpItemTerm = cursorTerm.getArgument(1);
                tmpItem = CreatePathItem(rowTypeBind, -1, tmpTypeBind, tmpItemTerm, varArray);
                cursorTerm = cursorTerm.getArgument(2);
            }
            if (cursorTerm.Name != "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect cursorTerm: " + cursorTerm);
            }
            // Create result to return (a single item or a path of some type).
            if (pathList.Count == 0)
            {
                return tmpItem;
            }
            // else
            if (tmpItem is IBinaryPathItem)
            {
                return new BinaryPath(extNum, pathList, tmpItem as IBinaryPathItem);
            }
            // else
            if (tmpItem is IBooleanPathItem)
            {
                return new BooleanPath(extNum, pathList, tmpItem as IBooleanPathItem);
            }
            // else
            if (tmpItem is IDateTimePathItem)
            {
                return new DateTimePath(extNum, pathList, tmpItem as IDateTimePathItem);
            }
            // else
            if (tmpItem is IDecimalPathItem)
            {
                return new DecimalPath(extNum, pathList, tmpItem as IDecimalPathItem);
            }
            // else
            if (tmpItem is IDoublePathItem)
            {
                return new DoublePath(extNum, pathList, tmpItem as IDoublePathItem);
            }
            // else
            if (tmpItem is IIntegerPathItem)
            {
                return new IntegerPath(extNum, pathList, tmpItem as IIntegerPathItem);
            }
            // else
            if (tmpItem is IObjectPathItem)
            {
                return new ObjectPath(extNum, pathList, tmpItem as IObjectPathItem);
            }
            // else
            if (tmpItem is IStringPathItem)
            {
                return new StringPath(extNum, pathList, tmpItem as IStringPathItem);
            }
            // else
            if (tmpItem is IUIntegerPathItem)
            {
                return new UIntegerPath(extNum, pathList, tmpItem as IUIntegerPathItem);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect tmpItem: " + tmpItem);
        }

        private static ObjectThis CreateObjectThis(RowTypeBinding rowTypeBind, Int32 extNum)
        {
            ITypeBinding typeBind = rowTypeBind.GetTypeBinding(extNum);
            return new ObjectThis(extNum, typeBind);
        }

        private static ObjectCast CreateObjectCast(Term typeTerm, IObjectExpression expr)
        {
            if (typeTerm.Name != "object" || typeTerm.Arity != 1)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
            }
            //PI110503 ITypeBinding typeBind = TypeRepository.GetTypeBindingByUpperCaseName(typeTerm.getArgument(1).Name.ToUpper());
            ITypeBinding typeBind = TypeRepository.GetTypeBinding(typeTerm.getArgument(1).Name);
            if (typeBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
            }
            return new ObjectCast(typeBind, expr);
        }

        private static IPath CreatePathItem(RowTypeBinding rowTypeBind, Int32 extNum, ITypeBinding typeBind, Term itemTerm, VariableArray varArray)
        {
            // itemTerm = cast(Member,Type) or itemTerm = Member.
            if (itemTerm.Name == "cast" && itemTerm.Arity == 2)
            {
                IMember member = CreateMember(rowTypeBind, extNum, typeBind, itemTerm.getArgument(1), varArray);
                if (member is IObjectExpression)
                {
                    return CreateObjectCast(itemTerm.getArgument(2), (member as IObjectExpression));
                }
                // else
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect member: " + member);
            }
            // else
            return CreateMember(rowTypeBind, extNum, typeBind, itemTerm, varArray);
        }

        private static IMember CreateMember(RowTypeBinding rowTypeBind, Int32 extNum, ITypeBinding typeBind, Term memberTerm,
                                            VariableArray varArray)
        {
            // memberTerm = property(Type,Name), memberTerm = method(Type,Name,ArgList), memberTerm = gmethod(Type,Name,TypeParamList,ArgList).
            if (memberTerm.Name == "property" && memberTerm.Arity == 2)
            {
                return CreateProperty(extNum, typeBind, memberTerm.getArgument(1), memberTerm.getArgument(2));
            }
            // else
            if (memberTerm.Name == "method" && memberTerm.Arity == 3)
                return CreateMethod(rowTypeBind, extNum, typeBind, memberTerm.getArgument(1), memberTerm.getArgument(2),
                                    memberTerm.getArgument(3), varArray);
            // else
            if (memberTerm.Name == "gmethod" && memberTerm.Arity == 4)
                return CreateGenericMethod(rowTypeBind, extNum, typeBind, memberTerm.getArgument(1), memberTerm.getArgument(2),
                                           memberTerm.getArgument(3), memberTerm.getArgument(4), varArray);
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect memberTerm: " + memberTerm);
        }

        private static IProperty CreateProperty(Int32 extNum, ITypeBinding typeBind, Term typeTerm, Term nameTerm)
        {
            // property(Type,Name).
            if (typeTerm.Name == "binary")
            {
                return CreateBinaryProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "boolean")
            {
                return CreateBooleanProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "datetime")
            {
                return CreateDateTimeProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "decimal")
            {
                return CreateDecimalProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "double")
            {
                return CreateDoubleProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "integer")
            {
                return CreateIntegerProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "object")
            {
                return CreateObjectProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "string")
            {
                return CreateStringProperty(extNum, typeBind, nameTerm);
            }
            // else
            if (typeTerm.Name == "uinteger")
            {
                return CreateUIntegerProperty(extNum, typeBind, nameTerm);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static BinaryProperty CreateBinaryProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.Binary)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new BinaryProperty(extNum, typeBind, propBind);
        }

        private static BooleanProperty CreateBooleanProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.Boolean)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new BooleanProperty(extNum, typeBind, propBind);
        }

        private static DateTimeProperty CreateDateTimeProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.DateTime)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new DateTimeProperty(extNum, typeBind, propBind);
        }

        private static DecimalProperty CreateDecimalProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.Decimal)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new DecimalProperty(extNum, typeBind, propBind);
        }

        private static DoubleProperty CreateDoubleProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.Double && propBind.TypeCode != DbTypeCode.Single)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new DoubleProperty(extNum, typeBind, propBind);
        }

        private static IntegerProperty CreateIntegerProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.Int64 && propBind.TypeCode != DbTypeCode.Int32 &&
                propBind.TypeCode != DbTypeCode.Int16 && propBind.TypeCode != DbTypeCode.SByte)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new IntegerProperty(extNum, typeBind, propBind);
        }

        private static ObjectProperty CreateObjectProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName + " ; typeBind: " + typeBind);
            }
            if (propBind.TypeCode != DbTypeCode.Object)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new ObjectProperty(extNum, typeBind, propBind);
        }

        private static StringProperty CreateStringProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.String)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new StringProperty(extNum, typeBind, propBind);
        }

        private static UIntegerProperty CreateUIntegerProperty(Int32 extNum, ITypeBinding typeBind, Term nameTerm)
        {
            String propName = nameTerm.Name;
            //PI110503 IPropertyBinding propBind = typeBind.GetPropertyBindingByUpperCaseName(propName.ToUpper());
            IPropertyBinding propBind = typeBind.GetPropertyBinding(propName);
            if (propBind == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown property: " + propName);
            }
            if (propBind.TypeCode != DbTypeCode.UInt64 && propBind.TypeCode != DbTypeCode.UInt32 &&
                propBind.TypeCode != DbTypeCode.UInt16 && propBind.TypeCode != DbTypeCode.Byte)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + propBind.TypeCode);
            }
            return new UIntegerProperty(extNum, typeBind, propBind);
        }

        private static IMethod CreateMethod(RowTypeBinding rowTypeBind, Int32 extNum, ITypeBinding typeBind, Term typeTerm, Term nameTerm,
                                            Term argListTerm, VariableArray varArray)
        {
            // method(Type,Name,ArgList).
            if (typeTerm.Name == "boolean")
            {
                return CreateBooleanMethod(rowTypeBind, extNum, typeBind, nameTerm, argListTerm, varArray);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static BooleanMethod CreateBooleanMethod(RowTypeBinding rowTypeBind, Int32 extNum, ITypeBinding typeBind, Term nameTerm,
                                                         Term argListTerm, VariableArray varArray)
        {
            // At the moment only support for EqualsOrIsDerivedFrom-method is implemented.
            if (nameTerm.Name != "EqualsOrIsDerivedFrom")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect nameTerm: " + nameTerm);
            }
            if (!(typeBind is TypeBinding))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind: " + typeBind);
            }
            if (!argListTerm.List || argListTerm.Name == "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect argListTerm: " + argListTerm);
            }
            Term argTerm = argListTerm.getArgument(1);
            IValueExpression expr = CreateValueExpression(rowTypeBind, argTerm, varArray);
            if (expr is IObjectExpression)
            {
                return new BooleanMethod(extNum, typeBind, expr as IObjectExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect logExpr: " + expr);
        }

        private static IMethod CreateGenericMethod(RowTypeBinding rowTypeBind, Int32 extNum, ITypeBinding typeBind, Term typeTerm,
                                                   Term nameTerm, Term argListTerm, Term typeParamListTerm, VariableArray varArray)
        {
#if false
            // gmethod(Type,Name,TypeParamList,ArgList).
            if (typeTerm.Name == "object")
            {
                return CreateObjectGenericMethod(rowTypeBind, extNum, typeBind, nameTerm, argListTerm, typeParamListTerm, varArray);
            }
#endif
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

#if false
        private static ObjectGenericMethod CreateObjectGenericMethod(RowTypeBinding rowTypeBind, Int32 extNum, ITypeBinding typeBind,
                Term nameTerm, Term typeParamListTerm, Term argListTerm, VariableArray varArray)
        {
            // At the moment only support for GetExtension-method is implemented.
            if (nameTerm.Name != "GetExtension")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect nameTerm: " + nameTerm);
            }
            if (!(typeBind is TypeBinding))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind: " + typeBind);
            }
            if (!typeParamListTerm.List || typeParamListTerm.Name == "[]")
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeParamListTerm: " + typeParamListTerm);
            }
            Term typeParamTerm = typeParamListTerm.getArgument(1);
            ExtensionBinding extBind = (typeBind as TypeBinding).GetExtensionBinding(typeParamTerm.Name);
            return new ObjectGenericMethod(extNum, typeBind, extBind);
        }
#endif

        private static IOperation CreateOperation(RowTypeBinding rowTypeBind, Term typeTerm, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                  VariableArray varArray)
        {
            if (typeTerm.Name == "decimal")
            {
                return CreateDecimalOperation(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "double")
            {
                return CreateDoubleOperation(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "numerical")
            {
                return CreateNumericalOperation(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            if (typeTerm.Name == "string")
            {
                return CreateStringOperation(rowTypeBind, opTerm, exprTerm1, exprTerm2, varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static IOperation CreateOperation(RowTypeBinding rowTypeBind, Term typeTerm, Term opTerm, Term exprTerm, VariableArray varArray)
        {
            if (typeTerm.Name == "decimal")
            {
                return CreateDecimalOperation(rowTypeBind, opTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "double")
            {
                return CreateDoubleOperation(rowTypeBind, opTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "numerical")
            {
                return CreateNumericalOperation(rowTypeBind, opTerm, exprTerm, varArray);
            }
            if (typeTerm.Name == "string")
            {
                return CreateStringOperation(rowTypeBind, opTerm, exprTerm, varArray);
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeTerm: " + typeTerm);
        }

        private static NumericalOperation CreateNumericalOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                                   VariableArray varArray)
        {
            NumericalOperator op = CreateNumericalOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (!(expr1 is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr1.DbTypeCode);
            }
            if (!(expr2 is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr2.DbTypeCode);
            }
            return new NumericalOperation(op, expr1 as INumericalExpression, expr2 as INumericalExpression);
        }

        private static NumericalOperation CreateNumericalOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm,
                                                                   VariableArray varArray)
        {
            NumericalOperator op = CreateNumericalOperator(opTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (!(expr is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr.DbTypeCode);
            }
            return new NumericalOperation(op, expr as INumericalExpression);
        }

        private static DecimalOperation CreateDecimalOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                               VariableArray varArray)
        {
            NumericalOperator op = CreateNumericalOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (!(expr1 is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr1.DbTypeCode);
            }
            if (!(expr2 is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr2.DbTypeCode);
            }
            return new DecimalOperation(op, expr1 as INumericalExpression, expr2 as INumericalExpression);
        }

        private static DecimalOperation CreateDecimalOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm,
                                                               VariableArray varArray)
        {
            NumericalOperator op = CreateNumericalOperator(opTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (!(expr is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr.DbTypeCode);
            }
            return new DecimalOperation(op, expr as INumericalExpression);
        }

        private static DoubleOperation CreateDoubleOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                             VariableArray varArray)
        {
            NumericalOperator op = CreateNumericalOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (!(expr1 is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr1.DbTypeCode);
            }
            if (!(expr2 is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr2.DbTypeCode);
            }
            return new DoubleOperation(op, expr1 as INumericalExpression, expr2 as INumericalExpression);
        }

        private static DoubleOperation CreateDoubleOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm,
                                                             VariableArray varArray)
        {
            NumericalOperator op = CreateNumericalOperator(opTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (!(expr is INumericalExpression))
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr.DbTypeCode);
            }
            return new DoubleOperation(op, expr as INumericalExpression);
        }

        private static StringOperation CreateStringOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm1, Term exprTerm2,
                                                             VariableArray varArray)
        {
            StringOperator op = CreateStringOperator(opTerm);
            IValueExpression expr1 = CreateValueExpression(rowTypeBind, exprTerm1, varArray);
            IValueExpression expr2 = CreateValueExpression(rowTypeBind, exprTerm2, varArray);
            if (expr1 is IStringExpression && expr2 is IStringExpression)
            {
                return new StringOperation(op, expr1 as IStringExpression, expr2 as IStringExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operands: " + expr1.DbTypeCode + " and " + expr2.DbTypeCode);
        }

        private static StringOperation CreateStringOperation(RowTypeBinding rowTypeBind, Term opTerm, Term exprTerm, VariableArray varArray)
        {
            StringOperator op = CreateStringOperator(opTerm);
            IValueExpression expr = CreateValueExpression(rowTypeBind, exprTerm, varArray);
            if (expr is IStringExpression)
            {
                return new StringOperation(op, expr as IStringExpression);
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type of operand: " + expr.DbTypeCode);
        }

        private static NumericalOperator CreateNumericalOperator(Term term)
        {
            if (term.Name == "addition")
            {
                return NumericalOperator.Addition;
            }
            // else
            if (term.Name == "division")
            {
                return NumericalOperator.Division;
            }
            // else
            if (term.Name == "minus")
            {
                return NumericalOperator.Minus;
            }
            // else
            if (term.Name == "multiplication")
            {
                return NumericalOperator.Multiplication;
            }
            // else
            if (term.Name == "plus")
            {
                return NumericalOperator.Plus;
            }
            // else
            if (term.Name == "subtraction")
            {
                return NumericalOperator.Subtraction;
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }

        private static StringOperator CreateStringOperator(Term term)
        {
            if (term == null)
            {
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term.");
            }
            if (term.Name == "concatenation")
            {
                return StringOperator.Concatenation;
            }
            // else
            if (term.Name == "addMaxChar")
            {
                return StringOperator.AppendMaxChar;
            }
            // else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect term: " + term);
        }
    }
}
