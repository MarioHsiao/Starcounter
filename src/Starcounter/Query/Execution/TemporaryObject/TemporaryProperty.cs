// ***********************************************************************
// <copyright file="TemporaryProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using Starcounter.Binding;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal class TemporaryProperty : IPropertyBinding
{
    String propName;
    Int32 propIndex;
    DbTypeCode typeCode;
    TypeBinding typeBinding;

    // Constructor when type != Object.
    internal TemporaryProperty(String name, Int32 index, DbTypeCode type)
    : base()
    {
        if (name == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect name.");
        }
        if (type == DbTypeCode.Object)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type: " + type);
        }
        propName = name;
        propIndex = index;
        typeCode = type;
        typeBinding = null;
    }

    // Constructor when type = Object.
    internal TemporaryProperty(String name, Int32 index, TypeBinding typeBind)
    : base()
    {
        if (name == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect name.");
        }
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        propName = name;
        propIndex = index;
        typeCode = DbTypeCode.Object;
        typeBinding = typeBind;
    }

    public Int32 Index
    {
        get
        {
            return propIndex;
        }
    }

    public String Name
    {
        get
        {
            return propName;
        }
    }

    public String DisplayName { get { return propName; } }

    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    public DbTypeCode TypeCode
    {
        get
        {
            return typeCode;
        }
    }

    internal void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.Append(tabs, propName);
        stringBuilder.Append(" : ");
        stringBuilder.AppendLine(typeCode.ToString());
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "TemporaryProperty");
    }

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IPropertyBinding other) {
        TemporaryProperty otherNode = other as TemporaryProperty;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(TemporaryProperty other) {
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
        Debug.Assert(this.propName == other.propName);
        if (this.propName != other.propName)
            return false;
        Debug.Assert(this.propIndex == other.propIndex);
        if (this.propIndex != other.propIndex)
            return false;
        Debug.Assert(this.typeCode == other.typeCode);
        if (this.typeCode != other.typeCode)
            return false;
        Debug.Assert(this.typeBinding == other.typeBinding);
        if (this.typeBinding != other.typeBinding)
            return false;
        return true;
    }
#endif
}
}
