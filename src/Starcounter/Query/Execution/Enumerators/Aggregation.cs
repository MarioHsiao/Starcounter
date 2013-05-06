// ***********************************************************************
// <copyright file="Aggregation.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Sql;
using Starcounter.Query.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class Aggregation : ExecutionEnumerator, IExecutionEnumerator
{
    TemporaryTypeBinding tempTypeBinding;
    Int32 extentNumber;

    IExecutionEnumerator subEnumerator;
    MultiComparer comparer; // Group-by columns.
    List<ISetFunction> setFunctionList;
    IExecutionEnumerator enumerator;
    Row cursorObject;
    Row contextObject;
    ILogicalExpression condition;
    Boolean firstCallOfMoveNext;

    internal Aggregation(byte nodeId, RowTypeBinding rowTypeBind,
        Int32 extNum,
        IExecutionEnumerator subEnum,
        IQueryComparer comparer,
        List<ISetFunction> setFuncList,
        ILogicalExpression condition,
        VariableArray varArr,
        String query,
        INumericalExpression fetchNumExpr, INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr)
        : base(nodeId, EnumeratorNodeType.Aggregate, rowTypeBind, varArr)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varArr.");
        if (subEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subEnum.");
        if (comparer == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparer.");
        if (setFuncList == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect setFuncList.");
        if (condition == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect condition.");
        if (query == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect query.");

        extentNumber = extNum;

        subEnumerator = subEnum;
        setFunctionList = setFuncList;
        this.condition = condition;
        if (comparer is MultiComparer)
        {
            this.comparer = (comparer as MultiComparer);
        }
        else if (comparer is ISingleComparer)
        {
            List<ISingleComparer> comparerList = new List<ISingleComparer>();
            comparerList.Add(comparer as ISingleComparer);
            this.comparer = new MultiComparer(comparerList);
        }
        else
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comparer.");
        }
        if (rowTypeBinding.GetTypeBinding(extentNumber) is TemporaryTypeBinding)
        {
            tempTypeBinding = rowTypeBinding.GetTypeBinding(extentNumber) as TemporaryTypeBinding;
        }
        else
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        }

        if (this.comparer.ComparerCount == 0) {
            this.enumerator = this.subEnumerator;
        } else {
            this.enumerator = new Sort(nodeId, subEnumerator.RowTypeBinding, subEnumerator, comparer, variableArray, query, null, null, null);
        }
        cursorObject = null;
        currentObject = null;
        contextObject = null;
        firstCallOfMoveNext = true;

        this.fetchNumberExpr = fetchNumExpr;
        this.fetchOffsetExpr = fetchOffsetExpr;
        this.fetchOffsetKeyExpr = fetchOffsetKeyExpr;

        this.query = query;
    }

    /// <summary>
    /// The type binding of the resulting objects of the query.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            if (projectionTypeCode == null)
                return rowTypeBinding;

            // Singleton object.
            if (projectionTypeCode == DbTypeCode.Object)
                return rowTypeBinding.GetPropertyBinding(0).TypeBinding;

            // Singleton non-object.
            return null;
        }
    }

    public Int32 Depth
    {
        get
        {
            return subEnumerator.Depth;
        }
    }

    Object IEnumerator.Current
    {
        get
        {
            return Current;
        }
    }

    public dynamic Current
    {
        get
        {
            if (currentObject != null)
            {
                switch (projectionTypeCode)
                {
                    case null:
                        return currentObject;

                    case DbTypeCode.Binary:
                        return currentObject.GetBinary(0);

                    case DbTypeCode.Boolean:
                        return currentObject.GetBoolean(0);

                    case DbTypeCode.Byte:
                        return currentObject.GetByte(0);

                    case DbTypeCode.DateTime:
                        return currentObject.GetDateTime(0);

                    case DbTypeCode.Decimal:
                        return currentObject.GetDecimal(0);

                    case DbTypeCode.Double:
                        return currentObject.GetDouble(0);

                    case DbTypeCode.Int16:
                        return currentObject.GetInt16(0);

                    case DbTypeCode.Int32:
                        return currentObject.GetInt32(0);

                    case DbTypeCode.Int64:
                        return currentObject.GetInt64(0);

                    case DbTypeCode.Object:
                        return currentObject.GetObject(0);

                    case DbTypeCode.SByte:
                        return currentObject.GetSByte(0);

                    case DbTypeCode.Single:
                        return currentObject.GetSingle(0);

                    case DbTypeCode.String:
                        return currentObject.GetString(0);

                    case DbTypeCode.UInt16:
                        return currentObject.GetUInt16(0);

                    case DbTypeCode.UInt32:
                        return currentObject.GetUInt32(0);

                    case DbTypeCode.UInt64:
                        return currentObject.GetUInt64(0);

                    default:
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect projectionTypeCode.");
                }
            }

            throw new InvalidOperationException("Enumerator has not started or has already finished.");
        }
    }

    public Row CurrentRow
    {
        get
        {
            if (currentObject != null)
            {
                return currentObject;
            }
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect currentObject.");
        }
    }

    private void CreateEnumerator() {
        enumerator.Reset();
    }

    /// <summary>
    /// Resets the enumerator with a context object.
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(Row obj)
    {
        //if (enumerator != null)
        //{
        //    enumerator.Reset();
        //    enumerator = null;
        //}

        currentObject = null;
        cursorObject = null;
        counter = 0;

        contextObject = obj;
        enumerator.Reset(contextObject);
        firstCallOfMoveNext = true;
    }

    public Boolean MoveNext()
    {
        // We should produce one result object for queries without "GROUP BY" clause and with empty underlying enumerator, 
        // for example "SELECT COUNT(*) FROM PERSON" should give one result even when there are no objects of type PERSON.
        Boolean produceOneResult = firstCallOfMoveNext && comparer.ComparerCount == 0;

        // If first call to MoveNext then initiate cursorObj to the first element in enumerator.
        if (currentObject == null)
        {
            enumerator.Reset();
            if (enumerator.MoveNext())
            {
                cursorObject = enumerator.CurrentRow;
            }
            else
            {
                cursorObject = null;
            }
        }
        // If cursorObj = null then there are no more elements.
        //if (cursorObject == null)
        if (cursorObject == null && !produceOneResult)
        {
            currentObject = null;
            return false;
        }

        if (currentObject == null && fetchNumberExpr != null) {
            if (fetchNumberExpr.EvaluateToInteger(null) != null)
                fetchNumber = fetchNumberExpr.EvaluateToInteger(null).Value;
            else
                fetchNumber = 0;
        }
        // Create new object
        Row compObject = new Row(rowTypeBinding);
        // Do Offset
        if (currentObject == null && fetchOffsetExpr != null)
            if (fetchOffsetExpr.EvaluateToInteger(null) != null) {
                for (int i = 0; i < fetchOffsetExpr.EvaluateToInteger(null).Value; i++)
                    if (!CreateNewObject(compObject, produceOneResult)) {
                        currentObject = null;
                        firstCallOfMoveNext = false;
                        return false;
                    } else {
                        //currentObject = compObject;
                        //counter++;
                        firstCallOfMoveNext = false;
                        produceOneResult = firstCallOfMoveNext && comparer.ComparerCount == 0;
                        compObject = new Row(rowTypeBinding);
                    }
                counter = 0;
            }
        // Check fetch
        if (counter >= fetchNumber) {
            currentObject = null;
            firstCallOfMoveNext = false;
            return false;
        }
        if (CreateNewObject(compObject, produceOneResult))
        {
            currentObject = compObject;
            counter++;
            firstCallOfMoveNext = false;
            return true;
        }
        else
        {
            currentObject = null;
            firstCallOfMoveNext = false;
            return false;
        }
    }

    internal Boolean CreateNewObject(Row compObject, Boolean produceOneResult) {
        TemporaryObject tempObject = new TemporaryObject(tempTypeBinding);
        compObject.AttachObject(extentNumber, tempObject);
        Boolean conditionValue = false;
        //while (cursorObject != null && !conditionValue)
        while ((cursorObject != null || produceOneResult) && !conditionValue) {
            // Set comparer values to new object.
            for (Int32 i = 0; i < comparer.ComparerCount; i++) {
                tempObject.SetValue(i, comparer.GetValue(cursorObject, i));
            }
            // Reset results of set-functions.
            for (Int32 i = 0; i < setFunctionList.Count; i++) {
                setFunctionList[i].ResetResult();
            }
            // Loop cursorObj until it refers the first element in the next group,
            // or if no such element exists then set cursorObj to null.
            while (cursorObject != null && comparer.Compare(tempObject, cursorObject) == 0) {
                for (Int32 i = 0; i < setFunctionList.Count; i++) {
                    setFunctionList[i].UpdateResult(cursorObject);
                }
                if (enumerator.MoveNext()) {
                    cursorObject = enumerator.CurrentRow;
                } else {
                    cursorObject = null;
                }
            }
            // Set set-function result values to new object.
            for (Int32 i = 0; i < setFunctionList.Count; i++) {
                tempObject.SetValue(comparer.ComparerCount + i, setFunctionList[i].GetResult());
            }
            conditionValue = condition.Instantiate(contextObject).Filtrate(compObject);
        }
        return conditionValue;
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        if (!force && MoveNext())
        {
            return true;
        }
        else if (counter == 0 || force)
        {
            // Create a NullObject.
            NullObject nullObj = new NullObject(rowTypeBinding.GetTypeBinding(extentNumber));
            currentObject = new Row(rowTypeBinding);
            currentObject.AttachObject(extentNumber, nullObj);
            counter++;
            return true;
        }
        else
        {
            currentObject = null;
            return false;
        }
    }

    /// <summary>
    /// Saves the underlying enumerator state.
    /// </summary>
    public unsafe Int32 SaveEnumerator(Byte* keysData, Int32 globalOffset, Boolean saveDynamicDataOnly)
    {
        return enumerator.SaveEnumerator(keysData, globalOffset, saveDynamicDataOnly);
    }

    /// <summary>
    /// Depending on query flags, populates the flags value.
    /// </summary>
    public unsafe override void PopulateQueryFlags(UInt32* flags)
    {
        enumerator.PopulateQueryFlags(flags);
    }

    /// <summary>
    /// Creating the clone of enumerator.
    /// </summary>
    /// <returns>Returns clone of the enumerator.</returns>
    public override IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArrClone)
    {
        List<ISetFunction> setFuncListClone = new List<ISetFunction>();
        for (Int32 i = 0; i < setFunctionList.Count; i++)
        {
            setFuncListClone.Add(setFunctionList[i].Clone(varArrClone));
        }

        // Clone fetch data and update varArrClone
        INumericalExpression fetchNumberExprClone = null;
        if (fetchNumberExpr != null)
            fetchNumberExprClone = fetchNumberExpr.CloneToNumerical(varArrClone);

        IBinaryExpression fetchOffsetKeyExprClone = null;
        if (fetchOffsetKeyExpr != null)
            fetchOffsetKeyExprClone = fetchOffsetKeyExpr.CloneToBinary(varArrClone);

        INumericalExpression fetchOffsetExprClone = null;
        if (fetchOffsetExpr != null)
            fetchOffsetExprClone = fetchOffsetExpr.CloneToNumerical(varArrClone);
        
        return new Aggregation(nodeId, rowTypeBindClone, extentNumber,
                               subEnumerator.Clone(rowTypeBindClone, varArrClone),
                               comparer.Clone(varArrClone), setFuncListClone,
                               condition.Clone(varArrClone), varArrClone, query,
                               fetchNumberExprClone, fetchOffsetExprClone, fetchOffsetKeyExprClone);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "Aggregation(");
        stringBuilder.AppendLine(tabs + 1, extentNumber.ToString());
        enumerator.BuildString(stringBuilder, tabs + 1);
        comparer.BuildString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs + 1, "SetFunctions(");
        for (Int32 i = 0; i < setFunctionList.Count; i++)
        {
            setFunctionList[i].BuildString(stringBuilder, tabs + 2);
        }
        stringBuilder.AppendLine(tabs + 1, ")");
        condition.BuildString(stringBuilder, tabs + 1);
        base.BuildFetchString(stringBuilder, tabs + 1);
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        subEnumerator.GenerateCompilableCode(stringGen);
    }

    /// <summary>
    /// Gets the unique name for this enumerator.
    /// </summary>
    public String GetUniqueName(UInt64 seqNumber)
    {
        if (uniqueGenName == null)
            uniqueGenName = "Aggregation" + extentNumber;

        return uniqueGenName;
    }

    public Boolean IsAtRecreatedKey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
    public Boolean StayAtOffsetkey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } set { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
    public Boolean UseOffsetkey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } set { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
}
}
