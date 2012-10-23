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
    IEnumerator<CompositeObject> enumerator;

    internal Sort(IExecutionEnumerator subEnum,
        IQueryComparer comp,
        VariableArray varArr,
        String query)
        : base(varArr)
    {
        if (subEnum == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect subEnum.");
        if (comp == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect comp.");
        if (varArr == null)
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect variables clone.");

        subEnumerator = subEnum;
        comparer = comp;
        compTypeBinding = subEnumerator.CompositeTypeBinding;
        enumerator = null;

        this.query = query;
        variableArray = varArr;
    }

    /// <summary>
    /// The type binding of the resulting objects of the query.
    /// </summary>
    public ITypeBinding TypeBinding
    {
        get
        {
            if (singleObject)
                return compTypeBinding.GetPropertyBinding(0).TypeBinding;

            return compTypeBinding;
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
                if (singleObject)
                    return enumerator.Current.GetObject(0);

                return enumerator.Current;
            }

            throw new InvalidOperationException("Enumerator has not started or has already finished.");
        }
    }

    public CompositeObject CurrentCompositeObject
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

        List<CompositeObject> list = new List<CompositeObject>();
        while (subEnumerator.MoveNext())
        {
            list.Add(subEnumerator.CurrentCompositeObject);
        }
        list.Sort(comparer);
        enumerator = list.GetEnumerator();
    }

    /// <summary>
    /// Resets the enumerator with a context object.
    /// </summary>
    /// <param name="obj">Context object from another enumerator.</param>
    public override void Reset(CompositeObject obj)
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

    public override IExecutionEnumerator Clone(CompositeTypeBinding resultTypeBindClone, VariableArray varArrClone)
    {
        return new Sort(subEnumerator.Clone(resultTypeBindClone, varArrClone), comparer.Clone(varArrClone), varArrClone, query);
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
