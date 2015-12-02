
using Starcounter.Binding;

namespace Starcounter.ObjectView {

    internal class DefaultPropertyReader {
        ValueFormatter formatter;

        public DefaultPropertyReader(ValueFormatter vf) {
            formatter = vf;
        }

        public string GetPropertyStringValue(
            IObjectView view, 
            ITypeBinding binding, 
            IPropertyBinding property) {
            string value = string.Empty;
            int i = property.Index;

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
    }
}
