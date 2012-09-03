
using Starcounter;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using Starcounter.Binding;

namespace Starcounter.Query.Execution
{
internal sealed class TemporaryTypeBinding : ITypeBinding
{
    List<TemporaryProperty> propertyList;
    Dictionary<String, Int32> indexByName;

    public TemporaryTypeBinding()
    {
        propertyList = new List<TemporaryProperty>();
        indexByName = new Dictionary<String, Int32>();
    }

    public String Name
    {
        get
        {
            return "Starcounter.TemporaryObject";
        }
    }

    public Int32 PropertyCount
    {
        get
        {
            return propertyList.Count;
        }
    }

    public Int32 GetPropertyIndex(String name)
    {
        Int32 index = -1;
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
        if (indexByName.TryGetValue(name, out index))
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
        if (indexByName.TryGetValue(name, out index))
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
    //    return GetPropertyBinding(name);
    //}

    // When property-type != Object.
    internal void AddTemporaryProperty(String name, DbTypeCode typeCode)
    {
        Int32 index = propertyList.Count;
        TemporaryProperty tmpProp = new TemporaryProperty(name, index, typeCode);
        indexByName.Add(name, index);
        propertyList.Add(tmpProp);
    }

    // When property-type = Object.
    internal void AddTemporaryProperty(String name, TypeBinding typeBind)
    {
        Int32 index = propertyList.Count;
        TemporaryProperty tmpProp = new TemporaryProperty(name, index, typeBind);
        indexByName.Add(name, index);
        propertyList.Add(tmpProp);
    }

    // When property-type = Object.
    internal void AddTemporaryProperty(String name, String typeName)
    {
        //PI110503 TypeBinding typeBind = TypeRepository.GetTypeBindingByUpperCaseName(typeName);
        TypeBinding typeBind = TypeRepository.GetTypeBinding(typeName);
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Unknown extent name: " + typeName);
        }
        Int32 index = propertyList.Count;
        TemporaryProperty tmpProp = new TemporaryProperty(name, index, typeBind);
        indexByName.Add(name, index);
        propertyList.Add(tmpProp);
    }

    internal void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "TemporaryType(");
        for (Int32 i = 0; i < propertyList.Count; i++)
        {
            propertyList[i].BuildString(stringBuilder, tabs + 1);
        }
        stringBuilder.AppendLine(tabs, ")");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "TemporaryProperty");
    }
}
}
