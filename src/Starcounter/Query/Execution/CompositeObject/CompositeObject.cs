
using Starcounter;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;

namespace Starcounter.Query.Execution
{
public sealed class CompositeObject : IObjectView, IDynamicMetaObjectProvider
{
    CompositeTypeBinding typeBinding;
    IObjectView[] objectArr;

    Int32 random;
    private static readonly LogSource logSource = LogSources.Sql;

    internal CompositeObject(CompositeTypeBinding typeBind)
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

    // TODO: Remove.
    //public Object Clone()
    //{
    //    return new CompositeObject(typeBinding);
    //}

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
                        presentation += Starcounter.Db.NullString + "\n";
                    }
                    else
                    {
                        presentation += BinaryToHex(binValue.Value) + "\n";
                    }
                    break;
                case DbTypeCode.Boolean:
                    Nullable<Boolean> boolValue = GetBoolean(i);
                    if (boolValue == null)
                    {
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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
                        presentation += Starcounter.Db.NullString + "\n";
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

    private String BinaryToHex(Binary value)
    {
        // TODO: Is there an easier way to convert a Binary into a hexadecimal string?
        StringBuilder stringBuilder = new StringBuilder(value.Length * 2);
        for (Int32 i = 0; i < value.Length; i++)
        {
            stringBuilder.AppendFormat("{0:x2}", value.GetByte(i));
        }
        return stringBuilder.ToString();
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
    /// The type of the instance must be the same as the specified type used when creating the CompositeObject.
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

    public IObjectView GetExtension(Int32 index)
    {
        throw new NotSupportedException();
    }

    Boolean IObjectView.EqualsOrIsDerivedFrom(IObjectView obj)
    {
        return Equals(obj);
    }

    public DynamicMetaObject GetMetaObject(Expression parameter)
    {
        return new CompositeMetaObject(parameter, this);
    }
}
}
