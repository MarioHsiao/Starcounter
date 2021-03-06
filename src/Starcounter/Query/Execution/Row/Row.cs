﻿// ***********************************************************************
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
using Starcounter.Advanced;
using Starcounter.ObjectView;

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

    public ulong Identity { get { throw new NotImplementedException(); } }
    public IBindableRetriever Retriever { get { throw new NotImplementedException(); } }

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
        /// Get the value of the property with <paramref name="index"/> as a
        /// string.
        /// </summary>
        /// <param name="index">Index of the property.</param>
        /// <param name="displayName">The display name of the property.</param>
        /// <returns>The property value, as a string, formatted by the supplied
        /// formatter.</returns>
        internal string GetPropertyStringValue(int index, ValueFormatter formatter, out string displayName) {
            var property = typeBinding.GetPropertyBinding(index) as PropertyMapping;
            displayName = property.DisplayName;
            string value = string.Empty;
            var view = this;
            var i = property.Index;

            switch (property.TypeCode) {
                case DbTypeCode.Binary:
                    var binValue = view.GetBinary(i);
                    value = formatter.GetBinary(binValue);
                    break;
                case DbTypeCode.Boolean:
                    var boolValue = view.GetBoolean(i);
                    value = formatter.GetBoolean(boolValue);
                    break;
                case DbTypeCode.Byte:
                    var byteValue = view.GetByte(i);
                    value = formatter.GetByte(byteValue);
                    break;
                case DbTypeCode.DateTime:
                    var dtmValue = view.GetDateTime(i);
                    value = formatter.GetDateTime(dtmValue);
                    break;
                case DbTypeCode.Decimal:
                    var decValue = view.GetDecimal(i);
                    value = formatter.GetDecimal(decValue);
                    break;
                case DbTypeCode.Double:
                    var dblValue = view.GetDouble(i);
                    value = formatter.GetDouble(dblValue);
                    break;
                case DbTypeCode.Int16:
                    var i16 = view.GetInt16(i);
                    value = formatter.GetInt16(i16);
                    break;
                case DbTypeCode.Int32:
                    var i32 = view.GetInt32(i);
                    value = formatter.GetInt32(i32);
                    break;
                case DbTypeCode.Int64:
                    var i64 = view.GetInt64(i);
                    value = formatter.GetInt64(i64);
                    break;
                case DbTypeCode.SByte:
                    var sb = view.GetSByte(i);
                    value = formatter.GetSByte(sb);
                    break;
                case DbTypeCode.Single:
                    var single = view.GetSingle(i);
                    value = formatter.GetSingle(single);
                    break;
                case DbTypeCode.String:
                    var strValue = view.GetString(i);
                    value = formatter.GetString(strValue);
                    break;
                case DbTypeCode.Object:
                    var objValue = view.GetObject(i);
                    value = formatter.GetObject(objValue);
                    break;
                case DbTypeCode.UInt16:
                    var ui16 = view.GetUInt16(i);
                    value = formatter.GetUInt16(ui16);
                    break;
                case DbTypeCode.UInt32:
                    var ui32 = view.GetUInt32(i);
                    value = formatter.GetUInt32(ui32);
                    break;
                case DbTypeCode.UInt64:
                    var ui64 = view.GetUInt64(i);
                    value = formatter.GetUInt64(ui64);
                    break;
            }

            return value;
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
                    return (Nullable<UInt64>)(propMap.Expression as INumericalExpression).EvaluateToUInteger(this);

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

    public dynamic GetValue(String propertyName) {
        int propIndex = typeBinding.GetPropertyIndexCaseInsensitive(propertyName);
        PropertyMapping prop = (PropertyMapping)typeBinding.GetPropertyBinding(propIndex);
        switch (prop.TypeCode) {
            case DbTypeCode.Binary: return GetBinary(propIndex);
            case DbTypeCode.Boolean: return GetBoolean(propIndex);
            case DbTypeCode.Byte: return GetByte(propIndex);
            case DbTypeCode.DateTime: return GetDateTime(propIndex);
            case DbTypeCode.Decimal: return GetDecimal(propIndex);
            case DbTypeCode.Double: return GetDouble(propIndex);
            case DbTypeCode.Int16: return GetInt16(propIndex);
            case DbTypeCode.Int32: return GetInt32(propIndex);
            case DbTypeCode.Int64: return GetInt64(propIndex);
            case DbTypeCode.SByte: return GetSByte(propIndex);
            case DbTypeCode.Single: return GetSingle(propIndex);
            case DbTypeCode.String: return GetString(propIndex);
            case DbTypeCode.Object: return GetObject(propIndex);
            case DbTypeCode.UInt16: return GetUInt16(propIndex);
            case DbTypeCode.UInt32: return GetUInt32(propIndex);
            case DbTypeCode.UInt64: return GetUInt64(propIndex);
            default:
                throw new ArgumentException("Incorrect type " + prop.TypeCode + " of property with name: " + propertyName);
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
