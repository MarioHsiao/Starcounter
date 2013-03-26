using System;
using System.Globalization;
using Starcounter;
using Starcounter.Binding;

namespace SQLTest
{
    internal static class Utilities
    {
        // From class Starcounter.db.
        internal const String FieldSeparator = " | ";

        // From class Starcounter.db.
        internal static String BinaryToHex(Binary value)
        {
            String hexString = BitConverter.ToString(value.ToArray());
            return hexString.Replace("-", "");
        }

        // From class Starcounter.db.
        internal static Binary HexToBinary(String hexString)
        {
            Byte[] byteArr = new Byte[hexString.Length / 2];
            for (Int32 i = 0; i < byteArr.Length; i++)
            {
                byteArr[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return new Binary(byteArr);
        }

        internal static String GetSingletonResult(DbTypeCode projectionTypeCode, dynamic singleton) {
            switch (projectionTypeCode) {
                case DbTypeCode.Binary:
                    Nullable<Binary> binValue = (Nullable<Binary>)singleton;
                    if (binValue == null)
                        return Db.NullString;
                    else
                        return BinaryToHex(binValue.Value);
                case DbTypeCode.Boolean:
                    Nullable<Boolean> blnValue = (Nullable<Boolean>) singleton;
                    if (blnValue == null)
                        return Db.NullString;
                    else
                        return blnValue.Value.ToString();
                case DbTypeCode.Byte:
                    Nullable<Byte> byteValue = (Nullable<Byte>) singleton;
                    if (byteValue == null)
                        return Db.NullString;
                    else
                        return byteValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.DateTime:
                    Nullable<DateTime> dtmValue = (Nullable<DateTime>)singleton;
                    if (dtmValue == null)
                        return Db.NullString;
                    else
                        return dtmValue.Value.ToString("u", DateTimeFormatInfo.InvariantInfo);
                case DbTypeCode.Decimal:
                    Nullable<Decimal> decValue = (Nullable<Decimal>)singleton;
                    if (decValue == null)
                        return Db.NullString;
                    else
                        return decValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.Double:
                    Nullable<Double> dblValue = (Nullable<Double>)singleton;
                    if (dblValue == null)
                        return Db.NullString;
                    else
                        return dblValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.Int16:
                    Nullable<Int16> int16Value = (Nullable<Int16>)singleton;
                    if (int16Value == null)
                        return Db.NullString;
                    else
                        return int16Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.Int32:
                    Nullable<Int32> int32Value = (Nullable<Int32>)singleton;
                    if (int32Value == null)
                        return Db.NullString;
                    else
                        return int32Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.Int64:
                    Nullable<Int64> int64Value = (Nullable<Int64>)singleton;
                    if (int64Value == null)
                        return Db.NullString;
                    else
                        return int64Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.Object:
                    IObjectView objValue = (IObjectView)singleton;
                    if (objValue == null)
                        return Db.NullString;
                    else {
                        //return Utilities.GetObjectIdString(objValue);
                        //// TODO:Ruslan
                        if (objValue is IObjectProxy)
                            //result += DbHelper.GetObjectID(objValue as Entity).ToString();
                            return Utilities.GetObjectIdString(objValue);
                        else
                            return objValue.ToString();
                    }
                case DbTypeCode.SByte:
                    Nullable<SByte> sbyteValue = (Nullable<SByte>)singleton;
                    if (sbyteValue == null)
                        return Db.NullString;
                    else
                        return sbyteValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.Single:
                    Nullable<Single> sngValue = (Nullable<Single>)singleton;
                    if (sngValue == null)
                        return Db.NullString;
                    else
                        return sngValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.String:
                    String strValue = (String)singleton;
                    if (strValue == null)
                        return Db.NullString;
                    else
                        return strValue;
                case DbTypeCode.UInt16:
                    Nullable<UInt16> uint16Value = (Nullable<UInt16>)singleton;
                    if (uint16Value == null)
                        return Db.NullString;
                    else
                        return uint16Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.UInt32:
                    Nullable<UInt32> uint32Value = (Nullable<UInt32>)singleton;
                    if (uint32Value == null)
                        return Db.NullString;
                    else
                        return uint32Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                case DbTypeCode.UInt64:
                    Nullable<UInt64> uint64Value = (Nullable<UInt64>)singleton;
                    if (uint64Value == null)
                        return Db.NullString.ToString(NumberFormatInfo.InvariantInfo);
                    else
                        return uint64Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                default:
                    throw new Exception("Incorrect TypeCode: " + projectionTypeCode);
            }
        }

        // From class Starcounter.Db. Added CultureInfo.InvariantCulture and NumberFormatInfo.InvariantInfo.
        internal static String CreateObjectString(ITypeBinding typeBind, IObjectView currentObj)
        {
            IPropertyBinding propBind = null;
            String result = FieldSeparator;
            for (Int32 i = 0; i < typeBind.PropertyCount; i++)
            {
                propBind = typeBind.GetPropertyBinding(i);
                switch (propBind.TypeCode)
                {
                    case DbTypeCode.Binary:
                        Nullable<Binary> binValue = currentObj.GetBinary(i);
                        if (binValue == null)
                            result += Db.NullString;
                        else
                            result += BinaryToHex(binValue.Value);
                        break;

                    case DbTypeCode.Boolean:
                        Nullable<Boolean> blnValue = currentObj.GetBoolean(i);
                        if (blnValue == null)
                            result += Db.NullString;
                        else
                            result += blnValue.Value.ToString();
                        break;

                    case DbTypeCode.Byte:
                        Nullable<Byte> byteValue = currentObj.GetByte(i);
                        if (byteValue == null)
                            result += Db.NullString;
                        else
                            result += byteValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.DateTime:
                        Nullable<DateTime> dtmValue = currentObj.GetDateTime(i);
                        if (dtmValue == null)
                            result += Db.NullString;
                        else
                            result += dtmValue.Value.ToString("u", DateTimeFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Decimal:
                        Nullable<Decimal> decValue = currentObj.GetDecimal(i);
                        if (decValue == null)
                            result += Db.NullString;
                        else
                            result += decValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Double:
                        Nullable<Double> dblValue = currentObj.GetDouble(i);
                        if (dblValue == null)
                            result += Db.NullString;
                        else
                            result += dblValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Int16:
                        Nullable<Int16> int16Value = currentObj.GetInt16(i);
                        if (int16Value == null)
                            result += Db.NullString;
                        else
                            result += int16Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Int32:
                        Nullable<Int32> int32Value = currentObj.GetInt32(i);
                        if (int32Value == null)
                            result += Db.NullString;
                        else
                            result += int32Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Int64:
                        Nullable<Int64> int64Value = currentObj.GetInt64(i);
                        if (int64Value == null)
                            result += Db.NullString;
                        else
                            result += int64Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Object:
                        IObjectView objValue = currentObj.GetObject(i);
                        if (objValue == null)
                            result += Db.NullString;
                        else
                        {
                            if (objValue is IObjectProxy)
                                //result += DbHelper.GetObjectID(objValue as Entity).ToString();
                                result += Utilities.GetObjectIdString(objValue);
                            else
                                result += objValue.ToString();
                        }
                        break;

                    case DbTypeCode.SByte:
                        Nullable<SByte> sbyteValue = currentObj.GetSByte(i);
                        if (sbyteValue == null)
                            result += Db.NullString;
                        else
                            result += sbyteValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.Single:
                        Nullable<Single> sngValue = currentObj.GetSingle(i);
                        if (sngValue == null)
                            result += Db.NullString;
                        else
                            result += sngValue.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.String:
                        String strValue = currentObj.GetString(i);
                        if (strValue == null)
                            result += Db.NullString;
                        else
                            result += strValue;
                        break;

                    case DbTypeCode.UInt16:
                        Nullable<UInt16> uint16Value = currentObj.GetUInt16(i);
                        if (uint16Value == null)
                            result += Db.NullString;
                        else
                            result += uint16Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.UInt32:
                        Nullable<UInt32> uint32Value = currentObj.GetUInt32(i);
                        if (uint32Value == null)
                            result += Db.NullString;
                        else
                            result += uint32Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    case DbTypeCode.UInt64:
                        Nullable<UInt64> uint64Value = currentObj.GetUInt64(i);
                        if (uint64Value == null)
                            result += Db.NullString.ToString(NumberFormatInfo.InvariantInfo);
                        else
                            result += uint64Value.Value.ToString(NumberFormatInfo.InvariantInfo);
                        break;

                    default:
                        throw new Exception("Incorrect TypeCode: " + propBind.TypeCode);
                }
                result += FieldSeparator;
            }
            return result;
        }

        internal static String CreateVariableValueString(Object obj)
        {
            String type = null;
            String value = null;

            if (obj is Binary)
            {
                type = "Binary";
                value = Utilities.BinaryToHex((Binary)obj);
            }
            else if (obj is Boolean)
            {
                type = "Boolean";
                value = ((Boolean)obj).ToString();
            }
            else if (obj is Byte)
            {
                type = "Byte";
                value = ((Byte)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is DateTime)
            {
                type = "DateTime";
                value = ((DateTime)obj).ToString(DateTimeFormatInfo.InvariantInfo);
            }
            else if (obj is Decimal)
            {
                type = "Decimal";
                value = ((Decimal)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is Double)
            {
                type = "Double";
                value = ((Boolean)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is Int16)
            {
                type = "Int16";
                value = ((Boolean)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is Int32)
            {
                type = "Int32";
                value = ((Boolean)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is Int64)
            {
                type = "Int64";
                value = ((Boolean)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is IObjectProxy)
            {
                type = "Object";
                //value = DbHelper.GetObjectIDString(obj as Entity);
                value = Utilities.GetObjectIdString(obj as IObjectView);
            }
            else if (obj is String)
            {
                type = "String";
                value = (String)obj;
            }
            else if (obj is SByte)
            {
                type = "SByte";
                value = ((SByte)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is UInt16)
            {
                type = "UInt16";
                value = ((UInt16)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is UInt32)
            {
                type = "UInt32";
                value = ((UInt32)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (obj is UInt64)
            {
                type = "UInt64";
                value = ((UInt64)obj).ToString(NumberFormatInfo.InvariantInfo);
            }
            else
                throw new Exception("Incorrect type of object: " + obj.GetType().Name);

            return type + ":" + value + "; ";
        }

        internal static IObjectView GetObject(String strObjectId)
        {
            UInt64 uintObjectId = UInt64.Parse(strObjectId);
            return DbHelper.FromID(uintObjectId);
        }

        internal static String GetObjectIdString(IObjectView obj)
        {
            UInt64 uintObjectId = DbHelper.GetObjectID(obj);
            return uintObjectId.ToString();
        }
    }
}
