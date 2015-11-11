using GenerateMetadataClasses.Model;
using System;

namespace GenerateMetadataClasses.CodeGenerator {

    public class DataTypeMap {
        public string ReferenceTypeName;
        public string ReadMethod;
        public string WriteMethod;
        public string CastExpression;

        public static DataTypeMap Map(Column column) {
            var map = new DataTypeMap();
            map.CastExpression = "";

            if (!string.IsNullOrEmpty(column.CastType)) {
                return MapCastType(map, column);
            }

            switch (column.TypeCode.ToUpperInvariant()) {
                case "STAR_TYPE_ULONG":
                    map.ReferenceTypeName = typeof(ulong).Name;
                    map.ReadMethod = "ReadUInt64";
                    map.WriteMethod = "WriteUInt64";
                    break;
                case "STAR_TYPE_REFERENCE":
                    var type = string.IsNullOrEmpty(column.TypeName) ? "MetadataEntity" : column.TypeName;
                    map.ReferenceTypeName = type;
                    map.ReadMethod = "ReadObject";
                    map.WriteMethod = "WriteObject";
                    map.CastExpression = "(" + type + ")";
                    break;
                case "STAR_TYPE_STRING":
                    map.ReferenceTypeName = typeof(string).Name;
                    map.ReadMethod = "ReadString";
                    map.WriteMethod = "WriteString";
                    break;
                default:
                    throw ExceptionCantMap(column);
            }
            return map;
        }

        static DataTypeMap MapCastType(DataTypeMap map, Column column) {
            switch (column.CastType.ToLowerInvariant()) {
                case "uint8_t":
                    map.ReferenceTypeName = typeof(byte).Name;
                    map.ReadMethod = "ReadByte";
                    map.WriteMethod = "WriteByte";
                    break;
                case "uint16_t":
                    map.ReferenceTypeName = typeof(ushort).Name;
                    map.ReadMethod = "ReadUInt16";
                    map.WriteMethod = "WriteUInt16";
                    break;
                case "int16_t":
                    map.ReferenceTypeName = typeof(short).Name;
                    map.ReadMethod = "ReadInt16";
                    map.WriteMethod = "WriteInt16";
                    break;
                case "bool":
                    map.ReferenceTypeName = typeof(bool).Name;
                    map.ReadMethod = "ReadBoolean";
                    map.WriteMethod = "WriteBoolean";
                    break;
                default:
                    throw ExceptionCantMap(column);
            }
            return map;
        }

        static Exception ExceptionCantMap(Column column) {
            var m = string.Format(
                "Unknown data type, cant map column {0}: type {1}, cast type {2}", 
                column.ColumnName,
                column.TypeCode,
                column.CastType
            );
            return new NotSupportedException(m);
        }
    }
}
