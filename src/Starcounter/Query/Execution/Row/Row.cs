// ***********************************************************************
// <copyright file="Row.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;
using Starcounter.Binding;
using Starcounter.Logging;
using System.Diagnostics;

namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Holds information about one row/tuple/item in the result set of a query.
    /// </summary>
public sealed class Row : IObjectView, IDynamicMetaObjectProvider
{
    RowTypeBinding typeBinding;
    IObjectView[] objectArr;

    Int32 random;
    private static readonly LogSource logSource = LogSources.Sql;

    internal Row(RowTypeBinding typeBind)
    {
        if (typeBind == null)
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect typeBind.");
        }
        typeBinding = typeBind;
        objectArr = new IObjectView[typeBinding.TypeBindingCount];
        random = -1;
    }

    internal void ResetCached()
    {
        random = -1;
    }

    /// <summary>
    /// View type binding.
    /// </summary>
    /// <value>The type binding.</value>
    public ITypeBinding TypeBinding
    {
        get
        {
            return typeBinding;
        }
    }

    /// <summary>
    /// A random number used when ordering a result by random (... ORDER BY RANDOM).
    /// </summary>
    internal Int32 Random
    {
        get
        {
            return random;
        }
        set
        {
            random = value;
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    override public String ToString()
    {
        String presentation = "";
        for (Int32 i = 0; i < typeBinding.PropertyCount; i++)
        {
            PropertyMapping propBind = typeBinding.GetPropertyBinding(i) as PropertyMapping;
            presentation += propBind.DisplayName + " = ";
            switch (propBind.TypeCode)
            {
                case DbTypeCode.Binary:
                    Nullable<Binary> binValue = GetBinary(i);
                    if (binValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += Db.BinaryToHex(binValue.Value) + "\n";
                    }
                    break;
                case DbTypeCode.Boolean:
                    Nullable<Boolean> boolValue = GetBoolean(i);
                    if (boolValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += boolValue.Value + "\n";
                    }
                    break;
                case DbTypeCode.Byte:
                    Nullable<UInt64> uintValue = GetByte(i);
                    if (uintValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += uintValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.DateTime:
                    Nullable<DateTime> dtmValue = GetDateTime(i);
                    if (dtmValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += dtmValue.Value + "\n";
                    }
                    break;
                case DbTypeCode.Decimal:
                    Nullable<Decimal> decValue = GetDecimal(i);
                    if (decValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += decValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.Double:
                    Nullable<Double> dblValue = GetDouble(i);
                    if (dblValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += dblValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.Int16:
                    Nullable<Int64> intValue = GetInt16(i);
                    if (intValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += intValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.Int32:
                    intValue = GetInt32(i);
                    if (intValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += intValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.Int64:
                    intValue = GetInt64(i);
                    if (intValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += intValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.SByte:
                    intValue = GetSByte(i);
                    if (intValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += intValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.Single:
                    dblValue = GetSingle(i);
                    if (dblValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += dblValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.String:
                    String strValue = GetString(i);
                    if (strValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += strValue + "\n";
                    }
                    break;
                case DbTypeCode.Object:
                    IObjectView objValue = GetObject(i);
                    if (objValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                        // presentation += (DbHelper.GetObjectID(value as Entity));
                    {
                        presentation += objValue.ToString() + "\n";
                    }
                    break;
                case DbTypeCode.UInt16:
                    uintValue = GetUInt16(i);
                    if (uintValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += uintValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.UInt32:
                    uintValue = GetUInt32(i);
                    if (uintValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += uintValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
                case DbTypeCode.UInt64:
                    uintValue = GetUInt64(i);
                    if (uintValue == null)
                    {
                        presentation += Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += uintValue.Value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + "\n";
                    }
                    break;
            }
        }
        return presentation;
    }

    /// <summary>
    /// Access the attached object at the specified index.
    /// If no object is attached at the specified index, null is returned.
    /// </summary>
    internal IObjectView AccessObject(Int32 index)
    {
        if (index >= 0 && index < objectArr.Length)
        {
            return objectArr[index];
        }
        else
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index: " + index);
        }
    }

    /// <summary>
    /// Attach an instance of an IObjectView at the specified index.
    /// The type of the instance must be the same as the specified type used when creating the Row.
    /// </summary>
    internal void AttachObject(Int32 index, IObjectView value)
    {
        if (index >= 0 && index < objectArr.Length)
        {
            objectArr[index] = value;
        }
        else
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect index: " + index);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Binary> GetBinary(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Binary:
                    return (propMap.Expression as IBinaryExpression).EvaluateToBinary(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Boolean> GetBoolean(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Boolean:
                    return (propMap.Expression as IBooleanExpression).EvaluateToBoolean(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Byte> GetByte(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Byte:
                    return (Nullable<Byte>)(propMap.Expression as INumericalExpression).EvaluateToUInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<DateTime> GetDateTime(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.DateTime:
                    return (propMap.Expression as IDateTimeExpression).EvaluateToDateTime(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Decimal> GetDecimal(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Decimal:
                case DbTypeCode.Int64:
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                case DbTypeCode.UInt64:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (propMap.Expression as INumericalExpression).EvaluateToDecimal(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Double> GetDouble(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Double:
                case DbTypeCode.Single:
                    return (propMap.Expression as INumericalExpression).EvaluateToDouble(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Int16> GetInt16(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                case DbTypeCode.Byte:
                    return (Nullable<Int16>)(propMap.Expression as INumericalExpression).EvaluateToInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Int32> GetInt32(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (Nullable<Int32>)(propMap.Expression as INumericalExpression).EvaluateToInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Int64> GetInt64(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Int64:
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (propMap.Expression as INumericalExpression).EvaluateToInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public IObjectView GetObject(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = ((PropertyMapping)typeBinding.GetPropertyBinding(index));

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Object:
                    IObjectView obj = (propMap.Expression as IObjectExpression).EvaluateToObject(this);
                    if (obj is NullObject)
                        return null;
                    else
                        return obj;

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<SByte> GetSByte(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.SByte:
                    return (Nullable<SByte>)(propMap.Expression as INumericalExpression).EvaluateToInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<Single> GetSingle(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Single:
                    return (Nullable<Single>)(propMap.Expression as INumericalExpression).EvaluateToDouble(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public String GetString(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.String:
                    return (propMap.Expression as IStringExpression).EvaluateToString(this);

                case DbTypeCode.Boolean:
                    return (propMap.Expression as IBooleanExpression).EvaluateToBoolean(this).ToString();

                case DbTypeCode.DateTime:
                    return (propMap.Expression as IDateTimeExpression).EvaluateToDateTime(this).ToString();

                case DbTypeCode.Decimal:
                    return (propMap.Expression as IDecimalExpression).EvaluateToDecimal(this).ToString();

                case DbTypeCode.Double:
                case DbTypeCode.Single:
                    return (propMap.Expression as IDoubleExpression).EvaluateToDouble(this).ToString();

                case DbTypeCode.Int64:
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                    return (propMap.Expression as INumericalExpression).EvaluateToInteger(this).ToString();

                case DbTypeCode.Object:
                    return (propMap.Expression as IObjectExpression).EvaluateToObject(this).ToString();

                case DbTypeCode.UInt64:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (propMap.Expression as INumericalExpression).EvaluateToUInteger(this).ToString();

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<UInt16> GetUInt16(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.SByte:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (Nullable<UInt16>)(propMap.Expression as INumericalExpression).EvaluateToUInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<UInt32> GetUInt32(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (Nullable<UInt32>)(propMap.Expression as INumericalExpression).EvaluateToUInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Nullable<UInt64> GetUInt64(Int32 index)
    {
        try
        {
            if (index < 0 || index >= typeBinding.PropertyCount)
            {
                throw new ArgumentOutOfRangeException("index", "Index is out of range (0 - " + (typeBinding.PropertyCount - 1) + "): " + index);
            }
            PropertyMapping propMap = (typeBinding.GetPropertyBinding(index) as PropertyMapping);

            switch (propMap.TypeCode)
            {
                case DbTypeCode.Int32:
                case DbTypeCode.Int16:
                case DbTypeCode.SByte:
                case DbTypeCode.UInt64:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt16:
                case DbTypeCode.Byte:
                    return (Nullable<UInt32>)(propMap.Expression as INumericalExpression).EvaluateToUInteger(this);

                default:
                    throw new ArgumentException("Incorrect type " + propMap.TypeCode + " of property with index: " + index);
            }
        }
        catch (DbException exception)
        {
            logSource.LogException(exception, "Internal Error");
            throw exception;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public IObjectView GetExtension(Int32 index)
    {
        throw new NotSupportedException();
    }

    Boolean IObjectView.EqualsOrIsDerivedFrom(IObjectView obj)
    {
        return Equals(obj);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public DynamicMetaObject GetMetaObject(Expression parameter)
    {
        return new RowMetaObject(parameter, this);
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
    /// <summary>
    /// Comparing this and given objects and asserting that they are equal.
    /// </summary>
    /// <param name="other">The given object to compare with this object.</param>
    /// <returns>True if the objects are equals and false otherwise.</returns>
    public bool AssertEquals(IObjectView other) {
        Row otherNode = other as Row;
        Debug.Assert(otherNode != null);
        return this.AssertEquals(otherNode);
    }
    internal bool AssertEquals(Row other) {
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
        Debug.Assert(this.objectArr.Length == other.objectArr.Length);
        if (this.objectArr.Length != other.objectArr.Length)
            return false;
        // Check references. This should be checked if there is cyclic reference.
        AssertEqualsVisited = true;
        bool areEquals = this.typeBinding.AssertEquals(other.typeBinding);
        // Check collections of objects
        for (int i = 0; i < this.objectArr.Length && areEquals; i++)
            if (this.objectArr[i] == null) {
                Debug.Assert(other.objectArr[i] == null);
                areEquals = other.objectArr[i] == null;
            } else
                areEquals = this.objectArr[i].AssertEquals(other.objectArr[i]);
        AssertEqualsVisited = false;
        return areEquals;
    }
#endif
}
}
