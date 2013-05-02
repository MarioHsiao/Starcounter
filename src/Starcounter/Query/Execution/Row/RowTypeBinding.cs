// ***********************************************************************
// <copyright file="RowTypeBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Optimization;
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class RowTypeBinding : ITypeBinding
{
    List<ITypeBinding> typeBindingList;

    List<PropertyMapping> propertyList;
    Dictionary<String, Int32> propertyIndexDictByName;

    // Used to create hierarchical result structure.
    List<Int32> extentOrder;
    List<IPropertyBinding>[] propertyListArr;

    public RowTypeBinding()
    {
        typeBindingList = new List<ITypeBinding>();
        propertyList = new List<PropertyMapping>();
        propertyIndexDictByName = new Dictionary<String, Int32>();
        extentOrder = null;
        propertyListArr = null;
    }

    internal RowTypeBinding Clone(VariableArray varArray)
    {
        RowTypeBinding rowTypeBind = new RowTypeBinding();
        rowTypeBind.typeBindingList = this.typeBindingList;
        rowTypeBind.extentOrder = this.extentOrder;
        rowTypeBind.propertyListArr = this.propertyListArr;

        IValueExpression expressionClone = null;
        for (Int32 i = 0; i < this.propertyList.Count; i++)
        {
            expressionClone = this.propertyList[i].Expression.Clone(varArray);
            rowTypeBind.AddPropertyMapping(this.propertyList[i].Name, expressionClone);
        }

        return rowTypeBind;
    }

	internal List<PropertyMapping> GetAllProperties()
	{
		return propertyList;
	}

    /// <summary>
    /// Row type binding can be simply shared.
    /// </summary>
    public void ResetCached()
    {
    }

    public String Name
    {
        get
        {
            return "Starcounter.Row";
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

    internal void AddPropertyMapping(String name, IValueExpression expr)
    {
        Int32 index = propertyList.Count;
        PropertyMapping propMap = new PropertyMapping(name, index, expr);
        propertyIndexDictByName.Add(name, index);
        //if (propMap.DisplayName != name)
        //    propertyIndexDictByName.Add(propMap.DisplayName, index);
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
        // Present the projection (Row).
        stringBuilder.AppendLine(tabs, "Projection(");
        for (Int32 i = 0; i < propertyList.Count; i++)
        {
            propertyList[i].BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    internal bool AssertEquals(RowTypeBinding other) {
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
        // Check cardinalities of collections
        Debug.Assert(this.propertyList.Count == other.propertyList.Count);
        if (this.propertyList.Count != other.propertyList.Count)
            return false;
        if (this.propertyListArr == null) {
            Debug.Assert(other.propertyListArr == null);
            if (other.propertyListArr != null)
                return false;
        } else {
            Debug.Assert(this.propertyListArr.Length == other.propertyListArr.Length);
            if (this.propertyListArr.Length != other.propertyListArr.Length)
                return false;
        }
        // Check collections of basic types
        if (this.extentOrder == null) {
            Debug.Assert(other.extentOrder == null);
            if (other.extentOrder != null)
                return false;
        } else {
            Debug.Assert(this.extentOrder.Count == other.extentOrder.Count);
            if (this.extentOrder.Count != other.extentOrder.Count)
                return false;
            for (int i = 0; i < this.extentOrder.Count; i++) {
                Debug.Assert(this.extentOrder[i] == other.extentOrder[i]);
                if (this.extentOrder[i] != other.extentOrder[i])
                    return false;
            }
        }
        if (this.propertyIndexDictByName.Count != other.propertyIndexDictByName.Count)
            return false;
        foreach (KeyValuePair<String, Int32> kvp in this.propertyIndexDictByName) {
            Int32 otherVal;
            Debug.Assert(other.propertyIndexDictByName.TryGetValue(kvp.Key, out otherVal));
            if (!other.propertyIndexDictByName.TryGetValue(kvp.Key, out otherVal))
                return false;
            Debug.Assert(kvp.Value == otherVal);
            if (kvp.Value != otherVal)
                return false;
        }
        Debug.Assert(this.typeBindingList.Count == other.typeBindingList.Count);
        if (this.typeBindingList.Count != other.typeBindingList.Count)
            return false;
        for (int i = 0; i < this.typeBindingList.Count; i++) {
            Debug.Assert(this.typeBindingList[i] == other.typeBindingList[i]);
            if (this.typeBindingList[i] != other.typeBindingList[i])
                return false;
        }
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        // Check collections of objects
        for (int i = 0; i < this.propertyList.Count && areEquals; i++)
            areEquals = this.propertyList[i].AssertEquals(other.propertyList[i]);
        if (this.propertyListArr != null)
            for (int i = 0; i < this.propertyListArr.Length && areEquals; i++) {
                areEquals = this.propertyListArr[i].Count == other.propertyListArr[i].Count;
                for (int j = 0; j < this.propertyListArr[i].Count && areEquals; j++)
                    areEquals = this.propertyListArr[i][j].AssertEquals(other.propertyListArr[i][j]);
            }
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
