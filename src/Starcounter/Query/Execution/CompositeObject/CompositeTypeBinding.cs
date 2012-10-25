// ***********************************************************************
// <copyright file="CompositeTypeBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal class CompositeTypeBinding : ITypeBinding
{
    List<ITypeBinding> typeBindingList;

    List<PropertyMapping> propertyList;
    Dictionary<String, Int32> propertyIndexDictByName;
    //PI110503 Dictionary<String, Int32> propertyIndexDictByUpperCaseName;

    // Used to create hierarchical result structure.
    List<Int32> extentOrder;
    List<IPropertyBinding>[] propertyListArr;

    public CompositeTypeBinding()
    {
        typeBindingList = new List<ITypeBinding>();
        propertyList = new List<PropertyMapping>();
        propertyIndexDictByName = new Dictionary<String, Int32>();
        //PI110503 propertyIndexDictByUpperCaseName = new Dictionary<String, Int32>();
        extentOrder = null;
        propertyListArr = null;
    }

    internal CompositeTypeBinding Clone(VariableArray varArray)
    {
        CompositeTypeBinding resultTypeBind = new CompositeTypeBinding();
        resultTypeBind.typeBindingList = this.typeBindingList;
        resultTypeBind.extentOrder = this.extentOrder;
        resultTypeBind.propertyListArr = this.propertyListArr;

        ITypeExpression expressionClone = null;
        for (Int32 i = 0; i < this.propertyList.Count; i++)
        {
            expressionClone = this.propertyList[i].Expression.Clone(varArray);
            resultTypeBind.AddPropertyMapping(this.propertyList[i].Name, expressionClone);
        }

        return resultTypeBind;
    }

	internal List<PropertyMapping> GetAllProperties()
	{
		return propertyList;
	}

    public void ResetCached()
    {
        // Result type binding can be simply shared.
    }

    public String Name
    {
        get
        {
            return "Starcounter.CompositeObject";
        }
    }

    public Int32 TypeBindingCount
    {
        get
        {
            return typeBindingList.Count;
        }
    }

    public Int32 PropertyCount
    {
        get
        {
            return propertyList.Count;
        }
    }

    internal List<Int32> ExtentOrder
    {
        get
        {
            return extentOrder;
        }
        set
        {
            extentOrder = value;
        }
    }

    internal ITypeBinding GetTypeBinding(Int32 index)
    {
        if (index < 0 || index >= typeBindingList.Count)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index: " + index);
        }
        return typeBindingList[index];
    }

    internal void AddTypeBinding(ITypeBinding typeBind)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        typeBindingList.Add(typeBind);
    }

    internal void AddTypeBinding(String typeName)
    {
        //PI110503 TypeBinding typeBind = TypeRepository.GetTypeBindingByUpperCaseName(typeName.ToUpper());
        TypeBinding typeBind = TypeRepository.GetTypeBinding(typeName);
        if (typeBind != null)
            typeBindingList.Add(typeBind);
        else
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeName: " + typeName);
    }

    public Int32 GetPropertyIndex(String name)
    {
        Int32 index = -1;
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
        if (propertyIndexDictByName.TryGetValue(name, out index))
        {
            return index;
        }
        else
        {
            throw new KeyNotFoundException("There is no property with name: " + name);
        }
    }

    public String GetPropertyName(Int32 index)
    {
        if (index < 0 || index >= propertyList.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (propertyList.Count - 1) + "): " + index);
        }
        return propertyList[index].Name;
    }

    public IPropertyBinding GetPropertyBinding(Int32 index)
    {
        if (index < 0 || index >= propertyList.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (propertyList.Count - 1) + "): " + index);
        }
        return propertyList[index];
    }

    public IPropertyBinding GetPropertyBinding(String name)
    {
        Int32 index = -1;
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
        if (propertyIndexDictByName.TryGetValue(name, out index))
        {
            return propertyList[index];
        }
        else
        {
            throw new KeyNotFoundException("There is no property with name: " + name);
        }
    }

    //PI110503
    //public IPropertyBinding GetPropertyBindingByUpperCaseName(String name)
    //{
    //    Int32 index = -1;
    //    if (name == null)
    //    {
    //        throw new ArgumentNullException("name");
    //    }
    //    if (propertyIndexDictByUpperCaseName.TryGetValue(name, out index))
    //    {
    //        return propertyList[index];
    //    }
    //    else
    //    {
    //        throw new KeyNotFoundException("There is no property with name: " + name);
    //    }
    //}

    internal void AddPropertyMapping(String name, ITypeExpression expr)
    {
        Int32 index = propertyList.Count;
        PropertyMapping propMap = new PropertyMapping(name, index, expr);
        propertyIndexDictByName.Add(name, index);
        //PI110503 propertyIndexDictByUpperCaseName.Add(name.ToUpper(), index);
        propertyList.Add(propMap);
    }

    /// <summary>
    /// Creates an array indexed by extent number, where each item contains a list of
    /// properties that should be evaluated at that extent.
    /// </summary>
    private void CreatePropertyListArray()
    {
        if (extentOrder == null || extentOrder.Count == 0)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentOrder.");
        }
        propertyListArr = new List<IPropertyBinding>[extentOrder.Count];
        for (Int32 i = 0; i < propertyListArr.Length; i++)
        {
            propertyListArr[i] = new List<IPropertyBinding>();
        }
        ExtentSet extentSet = new ExtentSet();
        Int32 extentNum = -1;
        for (Int32 i = 0; i < propertyList.Count; i++)
        {
            extentSet.Empty();
            propertyList[i].Expression.InstantiateExtentSet(extentSet);
            extentNum = extentSet.GetLastIncluded(extentOrder);
            propertyListArr[extentNum].Add(propertyList[i]);
        }
    }

    /// <summary>
    /// Returns a list of properties that should be evaluated at the input extent.
    /// </summary>
    /// <param name="extentNum">An extent number.</param>
    /// <returns>A list of properties.</returns>
    internal List<IPropertyBinding> GetPropertyList(Int32 extentNum)
    {
        if (propertyListArr == null)
        {
            CreatePropertyListArray();
        }
        if (extentNum < 0 || extentNum >= propertyListArr.Length)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect extentNum.");
        }
        return propertyListArr[extentNum];
    }

    internal void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        // Present the tables.
        stringBuilder.AppendLine(tabs, "Tables(");
        for (Int32 j = 0; j < typeBindingList.Count; j++)
        {
            stringBuilder.Append(tabs + 1, j.ToString());
            stringBuilder.Append(" = ");
            if (typeBindingList[j] is TemporaryTypeBinding)
            {
                stringBuilder.AppendLine("");
                (typeBindingList[j] as TemporaryTypeBinding).BuildString(stringBuilder, tabs + 2);
            }
            else
            {
                stringBuilder.AppendLine(typeBindingList[j].Name);
            }
        }
        stringBuilder.AppendLine(tabs, ")");
        // Present the projection (CompositeObject).
        stringBuilder.AppendLine(tabs, "Projection(");
        for (Int32 i = 0; i < propertyList.Count; i++)
        {
            propertyList[i].BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }
}
}
