
using Starcounter;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;

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

    public Int32 AccessCost
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public Int32 Index
    {
        get
        {
            return propIndex;
        }
    }

    public IPropertyBinding InversePropertyBinding
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public Int32 MutateCost
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public String Name
    {
        get
        {
            return propName;
        }
    }

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
}
}
