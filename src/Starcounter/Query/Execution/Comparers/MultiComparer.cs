
using Starcounter;
using Starcounter.Query.Sql;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Collections;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class MultiComparer : IQueryComparer
{
    List<ISingleComparer> comparerList;

    internal MultiComparer()
    {
        comparerList = new List<ISingleComparer>();
    }

    internal MultiComparer(List<ISingleComparer> comparerList)
    {
        this.comparerList = comparerList;
    }

    internal Int32 ComparerCount
    {
        get
        {
            return comparerList.Count;
        }
    }

    internal void AddComparer(ISingleComparer comp)
    {
        comparerList.Add(comp);
    }

    internal DbTypeCode GetComparerTypeCode(Int32 index)
    {
        if (index < 0 || index >= comparerList.Count)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index.");
        }
        return comparerList[index].ComparerTypeCode;
    }

    internal ISingleComparer GetComparer(Int32 index)
    {
        if (index < 0 || index >= comparerList.Count)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index.");
        }
        return comparerList[index];
    }

    public Int32 Compare(CompositeObject obj1, CompositeObject obj2)
    {
        if (obj1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj1.");
        }
        if (obj2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj2.");
        }
        Int32 result = 0;
        Int32 i = 0;
        while (result == 0 && i < comparerList.Count)
        {
            result = comparerList[i].Compare(obj1, obj2);
            i++;
        }
        return result;
    }

    internal Int32 Compare(TemporaryObject obj1, CompositeObject obj2)
    {
        if (obj1 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj1.");
        }
        if (obj2 == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj2.");
        }
        Int32 result = 0;
        Int32 i = 0;
        while (result == 0 && i < comparerList.Count)
        {
            result = comparerList[i].Compare(obj1.GetValue(i), obj2);
            i++;
        }
        return result;
    }

    internal ILiteral GetValue(CompositeObject obj, Int32 index)
    {
        if (obj == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect obj.");
        }
        if (index < 0 || index >= comparerList.Count)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index.");
        }
        return comparerList[index].Evaluate(obj);
    }

    public IQueryComparer Clone(VariableArray varArray)
    {
        List<ISingleComparer> comparerListClone = new List<ISingleComparer>();
        for (Int32 i = 0; i < comparerList.Count; i++)
        {
            comparerListClone.Add(comparerList[i].CloneToSingleComparer(varArray));
        }
        return new MultiComparer(comparerListClone);
    }

    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "MultiComparer(");
        for (Int32 i = 0; i < comparerList.Count; i++)
        {
            comparerList[i].BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "MultiComparer");
    }
}
}
