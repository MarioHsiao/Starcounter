// ***********************************************************************
// <copyright file="Sort.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class Sort : ExecutionEnumerator, IExecutionEnumerator
{
    IExecutionEnumerator subEnumerator;
    IQueryComparer comparer;
    IEnumerator<Row> enumerator;

    internal Sort(byte nodeId, RowTypeBinding rowTypeBind, 
        IExecutionEnumerator subEnum,
        IQueryComparer comp,
        VariableArray varArr,
        String query,
        INumericalExpression fetchNumExpr, INumericalExpression fetchOffsetExpr, IBinaryExpression fetchOffsetKeyExpr,
        Boolean topNode)
        : base(nodeId, EnumeratorNodeType.Sorting, rowTypeBind, varArr, topNode)
    {
        if (rowTypeBind == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect rowTypeBind.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect varArr.");
        if (subEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subEnum.");
        if (comp == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comp.");

        subEnumerator = subEnum;
        comparer = comp;
        //rowTypeBinding = subEnumerator.RowTypeBinding;
        enumerator = null;
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
            if (enumerator != null)
            {
                switch (projectionTypeCode)
                {
                    case null:
                        return enumerator.Current;

                    case DbTypeCode.Binary:
                        return enumerator.Current.GetBinary(0);

                    case DbTypeCode.Boolean:
                        return enumerator.Current.GetBoolean(0);

                    case DbTypeCode.Byte:
                        return enumerator.Current.GetByte(0);

                    case DbTypeCode.DateTime:
                        return enumerator.Current.GetDateTime(0);

                    case DbTypeCode.Decimal:
                        return enumerator.Current.GetDecimal(0);

                    case DbTypeCode.Double:
                        return enumerator.Current.GetDouble(0);

                    case DbTypeCode.Int16:
                        return enumerator.Current.GetInt16(0);

                    case DbTypeCode.Int32:
                        return enumerator.Current.GetInt32(0);

                    case DbTypeCode.Int64:
                        return enumerator.Current.GetInt64(0);

                    case DbTypeCode.Object:
                        return enumerator.Current.GetObject(0);

                    case DbTypeCode.SByte:
                        return enumerator.Current.GetSByte(0);

                    case DbTypeCode.Single:
                        return enumerator.Current.GetSingle(0);

                    case DbTypeCode.String:
                        return enumerator.Current.GetString(0);

                    case DbTypeCode.UInt16:
                        return enumerator.Current.GetUInt16(0);

                    case DbTypeCode.UInt32:
                        return enumerator.Current.GetUInt32(0);

                    case DbTypeCode.UInt64:
                        return enumerator.Current.GetUInt64(0);

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
            if (enumerator != null)
                return enumerator.Current;

            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect currentObject.");
        }
    }

    public Int32 Depth
    {
        get
        {
            return 0;
        }
    }

    private void CreateEnumerator()
    {
        if (enumerator != null)
            enumerator.Reset();

        List<Row> list = new List<Row>();
        while (subEnumerator.MoveNext())
        {
            list.Add(subEnumerator.CurrentRow);
        }
        list.Sort(comparer);
        enumerator = list.GetEnumerator();
    }

    /// <summary>
    /// Resets the enumerator with a context object.
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(Row obj, Boolean fromFinalize)
    {
        subEnumerator.Reset(obj, fromFinalize);
        counter = 0;

        if (enumerator != null)
        {
            enumerator.Dispose();
            enumerator = null;
        }
    }

    public Boolean MoveNext()
    {
        if (enumerator == null)
        {
            CreateEnumerator();
        }
        if (counter == 0 && fetchOffsetExpr != null)
            if (fetchOffsetExpr.EvaluateToInteger(null) != null) {
                for (int i = 0; i < fetchOffsetExpr.EvaluateToInteger(null).Value; i++)
                    if (!enumerator.MoveNext()) {
                        enumerator.Dispose();
                        enumerator = null;
                        return false;
                    }
                counter = 0;
            }
        if (counter == 0 && fetchNumberExpr != null) {
            if (fetchNumberExpr.EvaluateToInteger(null) != null)
                fetchNumber = fetchNumberExpr.EvaluateToInteger(null).Value;
            else
                fetchNumber = 0;
        }

        if (counter >= fetchNumber) {
            //currentObject = null;
            enumerator.Dispose();
            enumerator = null;
            return false;
        }
        if (enumerator.MoveNext()) {
            counter++;
            return true;
        }
        enumerator.Dispose();
        enumerator = null;
        return false;
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    /// <summary>
    /// Saves the underlying enumerator state.
    /// </summary>
    public unsafe UInt16 SaveEnumerator(Byte* keysData, UInt16 globalOffset, Boolean saveDynamicDataOnly)
    {
        return globalOffset;
    }

    /// <summary>
    /// Depending on query flags, populates the flags value.
    /// </summary>
    public unsafe override void PopulateQueryFlags(UInt32* flags)
    {
        subEnumerator.PopulateQueryFlags(flags);
    }

    public override IExecutionEnumerator Clone(RowTypeBinding rowTypeBindClone, VariableArray varArrClone)
    {
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

        return new Sort(nodeId, rowTypeBindClone, subEnumerator.Clone(rowTypeBindClone, varArrClone), comparer.Clone(varArrClone), varArrClone, query, 
            fetchNumberExprClone, fetchOffsetExprClone, fetchOffsetKeyExprClone, 
            TopNode);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "Sort(");
        subEnumerator.BuildString(stringBuilder, tabs + 1);
        comparer.BuildString(stringBuilder, tabs + 1);
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
            uniqueGenName = "Sort" + seqNumber;

        return uniqueGenName;
    }

    public Boolean IsAtRecreatedKey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
    public Boolean StayAtOffsetkey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } set { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
    public Boolean UseOffsetkey { get { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } set { throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED); } }
}
}
