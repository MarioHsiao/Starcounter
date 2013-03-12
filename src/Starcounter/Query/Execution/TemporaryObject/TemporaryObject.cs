// ***********************************************************************
// <copyright file="TemporaryObject.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using Starcounter.Binding;
using Starcounter.Logging;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
internal sealed class TemporaryObject : IObjectView
{
    TemporaryTypeBinding typeBinding;
    ILiteral[] valueArr;
    static readonly LogSource logSource = LogSources.Sql;

    internal TemporaryObject(TemporaryTypeBinding typeBind)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        typeBinding = typeBind;
        valueArr = new ILiteral[typeBinding.PropertyCount];
    }

    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    override public String ToString()
    {
        return Starcounter.Db.NoIdString;
    }

    public Nullable<Binary> GetBinary(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Binary)
            {
                return (valueArr[index] as BinaryLiteral).EvaluateToBinary(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Boolean> GetBoolean(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Boolean)
            {
                return (valueArr[index] as BooleanLiteral).EvaluateToBoolean(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Byte> GetByte(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<Byte>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<DateTime> GetDateTime(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.DateTime)
            {
                return (valueArr[index] as DateTimeLiteral).EvaluateToDateTime(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Decimal> GetDecimal(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Decimal)
            {
                return (valueArr[index] as DecimalLiteral).EvaluateToDecimal(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Int64)
            {
                return (Nullable<Decimal>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Int32)
            {
                return (Nullable<Decimal>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Int16)
            {
                return (Nullable<Decimal>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.SByte)
            {
                return (Nullable<Decimal>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt64)
            {
                return (Nullable<Decimal>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt32)
            {
                return (Nullable<Decimal>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt16)
            {
                return (Nullable<Decimal>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<Decimal>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Double> GetDouble(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Double)
            {
                return (valueArr[index] as DoubleLiteral).EvaluateToDouble(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Single)
            {
                return (valueArr[index] as DoubleLiteral).EvaluateToDouble(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Int16> GetInt16(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Int16)
            {
                return (Nullable<Int16>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.SByte)
            {
                return (Nullable<Int16>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<Int16>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Int32> GetInt32(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Int32)
            {
                return (Nullable<Int32>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Int16)
            {
                return (Nullable<Int32>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.SByte)
            {
                return (Nullable<Int32>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt16)
            {
                return (Nullable<Int32>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<Int32>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Int64> GetInt64(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Int64)
            {
                return (valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Int32)
            {
                return (valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Int16)
            {
                return (valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.SByte)
            {
                return (valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt32)
            {
                return (Nullable<Int64>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt16)
            {
                return (Nullable<Int64>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<Int64>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public IObjectView GetObject(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Object)
            {
                return (valueArr[index] as ObjectLiteral).EvaluateToObject(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<SByte> GetSByte(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.SByte)
            {
                return (Nullable<SByte>)(valueArr[index] as IntegerLiteral).EvaluateToInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<Single> GetSingle(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.Single)
            {
                return (Nullable<Single>)(valueArr[index] as DoubleLiteral).EvaluateToDouble(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public String GetString(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.String)
            {
                return (valueArr[index] as StringLiteral).EvaluateToString(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Boolean)
            {
                return (valueArr[index] as BooleanLiteral).EvaluateToBoolean(null).ToString();
            }
            else if (propBind.TypeCode == DbTypeCode.Byte || propBind.TypeCode == DbTypeCode.UInt16 ||
                     propBind.TypeCode == DbTypeCode.UInt32 || propBind.TypeCode == DbTypeCode.UInt64)
            {
                return (valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null).ToString();
            }
            else if (propBind.TypeCode == DbTypeCode.DateTime)
            {
                return (valueArr[index] as DateTimeLiteral).EvaluateToDateTime(null).ToString();
            }
            else if (propBind.TypeCode == DbTypeCode.Decimal)
            {
                return (valueArr[index] as DecimalLiteral).EvaluateToDecimal(null).ToString();
            }
            else if (propBind.TypeCode == DbTypeCode.Double || propBind.TypeCode == DbTypeCode.Single)
            {
                return (valueArr[index] as DoubleLiteral).EvaluateToDouble(null).ToString();
            }
            else if (propBind.TypeCode == DbTypeCode.SByte || propBind.TypeCode == DbTypeCode.Int16 ||
                     propBind.TypeCode == DbTypeCode.Int32 || propBind.TypeCode == DbTypeCode.Int64)
            {
                return (valueArr[index] as IntegerLiteral).EvaluateToInteger(null).ToString();
            }
            else if (propBind.TypeCode == DbTypeCode.Object)
            {
                return (valueArr[index] as ObjectLiteral).EvaluateToObject(null).ToString();
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<UInt16> GetUInt16(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.UInt16)
            {
                return (Nullable<UInt16>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<UInt16>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<UInt32> GetUInt32(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.UInt32)
            {
                return (Nullable<UInt32>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt16)
            {
                return (Nullable<UInt32>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (Nullable<UInt32>)(valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    public Nullable<UInt64> GetUInt64(Int32 index)
    {
        try
        {
            if (index < 0 || index >= valueArr.Length)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (valueArr.Length - 1) + "): " + index);
            }
            IPropertyBinding propBind = typeBinding.GetPropertyBinding(index);
            if (propBind.TypeCode == DbTypeCode.UInt64)
            {
                return (valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt32)
            {
                return (valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.UInt16)
            {
                return (valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else if (propBind.TypeCode == DbTypeCode.Byte)
            {
                return (valueArr[index] as UIntegerLiteral).EvaluateToUInteger(null);
            }
            else
            {
                throw new ArgumentException("Incorrect type " + propBind.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    internal ILiteral GetValue(Int32 index)
    {
        if (index < 0 || index >= valueArr.Length)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index: " + index);
        }
        return valueArr[index];
    }

    internal void SetValue(Int32 index, ILiteral value)
    {
        if (index < 0 || index >= valueArr.Length)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index: " + index);
        }
        if (typeBinding.GetPropertyBinding(index).TypeCode == value.DbTypeCode)
        {
            valueArr[index] = value;
        }
        else
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect type code.");
        }
    }

    public IObjectView GetExtension(Int32 index)
    {
        throw new NotSupportedException();
    }

    Boolean IObjectView.EqualsOrIsDerivedFrom(IObjectView obj)
    {
        return Equals(obj);
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "TemporaryObject");
    }

    #region Temporary extension methods from Entity
    public void Attach(ObjectRef objectRef, TypeBinding typeBinding) {
        throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "This interface is temporary");
    }
    public void Attach(ulong addr, ulong oid, TypeBinding typeBinding) {
        throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "This interface is temporary");
    }
    public ObjectRef ThisRef {
        get {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "This interface is temporary");
        }
        set {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "This interface is temporary");
        }
    }
    #endregion

#if DEBUG
    private bool AssertEqualsVisited = false;
    public bool AssertEquals(IObjectView other) {
        TemporaryObject otherNode = other as TemporaryObject;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(TemporaryObject other) {
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
        Debug.Assert(this.valueArr.Length == other.valueArr.Length);
        if (this.valueArr.Length != other.valueArr.Length)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = true;
        if (this.typeBinding == null) {
            Debug.Assert(other.typeBinding == null);
            areEquals = other.typeBinding == null;
        } else
            areEquals = this.typeBinding.AssertEquals(other.typeBinding);
        // Check collections of objects
        for (int i = 0; i < this.valueArr.Length && areEquals; i++)
            if (this.valueArr[i] == null) {
                Debug.Assert(other.valueArr[i] == null);
                areEquals = other.valueArr[i] == null;
            } else
                areEquals = this.valueArr[i].AssertEquals(other.valueArr[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
