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

    internal Sort(RowTypeBinding rowTypeBind, 
        IExecutionEnumerator subEnum,
        IQueryComparer comp,
        VariableArray varArr,
        String query)
        : base(rowTypeBind, varArr)
    {
        if (subEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subEnum.");
        if (comp == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comp.");

        subEnumerator = subEnum;
        comparer = comp;
        //rowTypeBinding = subEnumerator.RowTypeBinding;
        enumerator = null;

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
    public override void Reset(Row obj)
    {
        subEnumerator.Reset(obj);
        counter = 0;

        if (enumerator != null)
        {
            enumerator.Reset();
            enumerator = null;
        }
    }

    public Boolean MoveNext()
    {
        if (enumerator == null)
        {
            CreateEnumerator();
        }
        if (enumerator.MoveNext())
            return true;

        return false;
    }

    public Boolean MoveNextSpecial(Boolean force)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    /// <summary>
    /// Saves the underlying enumerator state.
    /// </summary>
    public unsafe Int32 SaveEnumerator(Byte* keysData, Int32 globalOffset, Boolean saveDynamicDataOnly)
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
        return new Sort(rowTypeBindClone, subEnumerator.Clone(rowTypeBindClone, varArrClone), comparer.Clone(varArrClone), varArrClone, query);
    }

    public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "Sort(");
        subEnumerator.BuildString(stringBuilder, tabs + 1);
        comparer.BuildString(stringBuilder, tabs + 1);
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
}
}
