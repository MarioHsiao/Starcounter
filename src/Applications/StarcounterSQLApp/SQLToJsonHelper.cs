using System;
using Starcounter;
using Starcounter.Binding;

namespace StarcounterSQLApp {
    internal class SingleProjectionBinding : IPropertyBinding {
        private DbTypeCode typeCode;

        public bool AssertEquals(IPropertyBinding other) {
            throw new NotImplementedException();
        }

        public int Index {
            get { return 0; }
        }

        public string Name {
            get { return "0"; }
        }

        public ITypeBinding TypeBinding {
            get { return null; }
        }

        public DbTypeCode TypeCode {
            get { return typeCode; }
            set { typeCode = value; }
        }
    }

    internal static class SQLToJsonHelper {
        private delegate string PropertyToJsonValueDelegate(IPropertyBinding property, IObjectView obj);

        private static PropertyToJsonValueDelegate[] _getJsonValue;

        static SQLToJsonHelper() {
            _getJsonValue = new PropertyToJsonValueDelegate[17];
            _getJsonValue[(int)DbTypeCode.Boolean] = new PropertyToJsonValueDelegate(GetBoolean);
            _getJsonValue[(int)DbTypeCode.Byte] = new PropertyToJsonValueDelegate(GetByte);
            _getJsonValue[(int)DbTypeCode.DateTime] = new PropertyToJsonValueDelegate(GetDateTime);
            _getJsonValue[(int)DbTypeCode.Decimal] = new PropertyToJsonValueDelegate(GetDecimal);
            _getJsonValue[(int)DbTypeCode.Single] = new PropertyToJsonValueDelegate(GetSingle);
            _getJsonValue[(int)DbTypeCode.Double] = new PropertyToJsonValueDelegate(GetDouble);
            _getJsonValue[(int)DbTypeCode.Int64] = new PropertyToJsonValueDelegate(GetInt64);
            _getJsonValue[(int)DbTypeCode.Int32] = new PropertyToJsonValueDelegate(GetInt32);
            _getJsonValue[(int)DbTypeCode.Int16] = new PropertyToJsonValueDelegate(GetInt16);
            _getJsonValue[(int)DbTypeCode.Object] = new PropertyToJsonValueDelegate(GetObject);
            _getJsonValue[(int)DbTypeCode.SByte] = new PropertyToJsonValueDelegate(GetSByte);
            _getJsonValue[(int)DbTypeCode.String] = new PropertyToJsonValueDelegate(GetString);
            _getJsonValue[(int)DbTypeCode.UInt64] = new PropertyToJsonValueDelegate(GetUInt64);
            _getJsonValue[(int)DbTypeCode.UInt32] = new PropertyToJsonValueDelegate(GetUInt32);
            _getJsonValue[(int)DbTypeCode.UInt16] = new PropertyToJsonValueDelegate(GetUInt16);
            _getJsonValue[(int)DbTypeCode.Binary] = new PropertyToJsonValueDelegate(GetBinary);
            _getJsonValue[(int)DbTypeCode.LargeBinary] = new PropertyToJsonValueDelegate(GetLargeBinary);
        }

        internal static string PropertyToJsonString(IPropertyBinding property, IObjectView obj) {
            return _getJsonValue[(int)property.TypeCode](property, obj);
        }

        internal static string DbTypeCodeToString(DbTypeCode typeCode) {
            string ret;

            switch (typeCode){
                case DbTypeCode.Byte:
                case DbTypeCode.Decimal:
                case DbTypeCode.Double:
                case DbTypeCode.Int16:
                case DbTypeCode.Int32:
                case DbTypeCode.Int64:
                case DbTypeCode.SByte:
                case DbTypeCode.Single:
                case DbTypeCode.UInt16:
                case DbTypeCode.UInt32:
                case DbTypeCode.UInt64:
                    ret = "number";
                    break;
                case DbTypeCode.Boolean:
                    ret = "boolean";
                    break;
                default:
                    ret = "string";
                    break;
            }
            return ret;
        }

        private static string GetBoolean(IPropertyBinding property, IObjectView obj) {
            bool? value = obj.GetBoolean(property.Index);
            return value.ToString().ToLower();
        }

        private static string GetByte(IPropertyBinding property, IObjectView obj) {
            byte? value = obj.GetByte(property.Index);
            return value.ToString();
        }

        private static string GetDateTime(IPropertyBinding property, IObjectView obj) {
            DateTime? value = obj.GetDateTime(property.Index);
            return value.ToString();
        }

        private static string GetDecimal(IPropertyBinding property, IObjectView obj) {
            decimal? value = obj.GetDecimal(property.Index);
            return value.ToString();
        }

        private static string GetSingle(IPropertyBinding property, IObjectView obj) {
            float? value = obj.GetSingle(property.Index);
            return value.ToString();
        }

        private static string GetDouble(IPropertyBinding property, IObjectView obj) {
            double? value = obj.GetDouble(property.Index);
            return value.ToString();
        }

        private static string GetInt64(IPropertyBinding property, IObjectView obj) {
            long? value = obj.GetInt64(property.Index);
            return value.ToString();
        }

        private static string GetInt32(IPropertyBinding property, IObjectView obj) {
            int? value = obj.GetInt32(property.Index);
            return value.ToString();
        }

        private static string GetInt16(IPropertyBinding property, IObjectView obj) {
            short? value = obj.GetInt16(property.Index);
            return value.ToString();
        }

        private static string GetObject(IPropertyBinding property, IObjectView obj) {
            var value = obj.GetObject(property.Index) as Entity;
            if (value != null)
                return DbHelper.GetObjectID(value).ToString();
            else
                return null;
        }

        private static string GetSByte(IPropertyBinding property, IObjectView obj) {
            sbyte? value = obj.GetSByte(property.Index);
            return value.ToString();
        }

        private static string GetString(IPropertyBinding property, IObjectView obj) {
            string value = obj.GetString(property.Index);

            if (value != null)
                return '\"' + value + '\"';
            return "\"\"";
        }

        private static string GetUInt64(IPropertyBinding property, IObjectView obj) {
            ulong? value = obj.GetUInt64(property.Index);
            return value.ToString();
        }

        private static string GetUInt32(IPropertyBinding property, IObjectView obj) {
            uint? value = obj.GetUInt32(property.Index);
            return value.ToString();
        }

        private static string GetUInt16(IPropertyBinding property, IObjectView obj) {
            ushort? value = obj.GetUInt16(property.Index);
            return value.ToString();
        }

        private static string GetBinary(IPropertyBinding property, IObjectView obj) {
            Binary? value = obj.GetBinary(property.Index);
            // TODO:
            return "\"TODO:\"";
            //            return value.ToString();
        }

        private static string GetLargeBinary(IPropertyBinding property, IObjectView obj) {
            Binary? value = obj.GetBinary(property.Index);
            // TODO:
            return "\"TODO:\"";
            //            return value.ToString();
        }
    }
}
