
using System;

namespace Starcounter.Hosting {

    public static class DbStateRedirect {

        /// <summary>
        /// The target we redirect every call to.
        /// </summary>
        public static IDbState Target { get; set; }

        public static void Insert(ushort tableId, ref ulong oid, ref ulong address) {
            DbStateRedirect.Target.Insert(tableId, ref oid, ref address);
        }

        public static bool ReadBoolean(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadBoolean(oid, address, index);
        }

        public static bool? ReadNullableBoolean(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableBoolean(oid, address, index);
        }

        public static byte ReadByte(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadByte(oid, address, index);
        }

        public static byte? ReadNullableByte(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableByte(oid, address, index);
        }

        public static DateTime ReadDateTime(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadDateTime(oid, address, index);
        }

        public static DateTime? ReadNullableDateTime(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableDateTime(oid, address, index);
        }

        public static decimal ReadDecimal(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadDecimal(oid, address, index);
        }

        public static decimal? ReadNullableDecimal(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableDecimal(oid, address, index);
        }

        public static double ReadDouble(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadDouble(oid, address, index);
        }

        public static double? ReadNullableDouble(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableDouble(oid, address, index);
        }

        public static short ReadInt16(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadInt16(oid, address, index);
        }

        public static short? ReadNullableInt16(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableInt16(oid, address, index);
        }

        public static int ReadInt32(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadInt32(oid, address, index);
        }

        public static int? ReadNullableInt32(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableInt32(oid, address, index);
        }

        public static long ReadInt64(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadInt64(oid, address, index);
        }

        public static long? ReadNullableInt64(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableInt64(oid, address, index);
        }

        public static IObjectView ReadObject(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadObject(oid, address, index);
        }

        public static sbyte ReadSByte(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadSByte(oid, address, index);
        }

        public static sbyte? ReadNullableSByte(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableSByte(oid, address, index);
        }

        public static float ReadSingle(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadSingle(oid, address, index);
        }

        public static float? ReadNullableSingle(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableSingle(oid, address, index);
        }

        public static string ReadString(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadString(oid, address, index);
        }

        public static Binary ReadBinary(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadBinary(oid, address, index);
        }

        public static LargeBinary ReadLargeBinary(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadLargeBinary(oid, address, index);
        }

        public static TimeSpan ReadTimeSpan(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadTimeSpan(oid, address, index);
        }

        public static TimeSpan? ReadNullableTimeSpan(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableTimeSpan(oid, address, index);
        }

        public static long ReadTimeSpanEx(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadTimeSpanEx(oid, address, index);
        }

        public static ushort ReadUInt16(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadUInt16(oid, address, index);
        }

        public static ushort? ReadNullableUInt16(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableUInt16(oid, address, index);
        }

        public static uint ReadUInt32(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadUInt32(oid, address, index);
        }

        public static uint? ReadNullableUInt32(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableUInt32(oid, address, index);
        }

        public static ulong ReadUInt64(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadUInt64(oid, address, index);
        }

        public static ulong? ReadNullableUInt64(ulong oid, ulong address, int index) {
            return DbStateRedirect.Target.ReadNullableUInt64(oid, address, index);
        }

        public static void WriteBoolean(ulong oid, ulong address, int index, bool value) {
            DbStateRedirect.Target.WriteBoolean(oid, address, index, value);
        }

        public static void WriteNullableBoolean(ulong oid, ulong address, int index, bool? value) {
            DbStateRedirect.Target.WriteNullableBoolean(oid, address, index, value);
        }

        public static void WriteByte(ulong oid, ulong address, int index, byte value) {
            DbStateRedirect.Target.WriteByte(oid, address, index, value);
        }

        public static void WriteNullableByte(ulong oid, ulong address, int index, byte? value) {
            DbStateRedirect.Target.WriteNullableByte(oid, address, index, value);
        }

        public static void WriteDateTime(ulong oid, ulong address, int index, DateTime value) {
            DbStateRedirect.Target.WriteDateTime(oid, address, index, value);
        }

        public static void WriteNullableDateTime(ulong oid, ulong address, int index, DateTime? value) {
            DbStateRedirect.Target.WriteNullableDateTime(oid, address, index, value);
        }

        public static void WriteDateTimeEx(ulong oid, ulong address, int index, long value) {
            DbStateRedirect.Target.WriteDateTimeEx(oid, address, index, value);
        }

        public static void WriteDecimal(ulong oid, ulong address, int index, decimal value) {
            DbStateRedirect.Target.WriteDecimal(oid, address, index, value);
        }

        public static void WriteNullableDecimal(ulong oid, ulong address, int index, decimal? value) {
            DbStateRedirect.Target.WriteNullableDecimal(oid, address, index, value);
        }

        public static void WriteDouble(ulong oid, ulong address, int index, double value) {
            DbStateRedirect.Target.WriteDouble(oid, address, index, value);
        }

        public static void WriteNullableDouble(ulong oid, ulong address, int index, double? value) {
            DbStateRedirect.Target.WriteNullableDouble(oid, address, index, value);
        }

        public static void WriteInt16(ulong oid, ulong address, int index, short value) {
            DbStateRedirect.Target.WriteInt16(oid, address, index, value);
        }

        public static void WriteNullableInt16(ulong oid, ulong address, int index, short? value) {
            DbStateRedirect.Target.WriteNullableInt16(oid, address, index, value);
        }

        public static void WriteInt32(ulong oid, ulong address, int index, int value) {
            DbStateRedirect.Target.WriteInt32(oid, address, index, value);
        }

        public static void WriteNullableInt32(ulong oid, ulong address, int index, int? value) {
            DbStateRedirect.Target.WriteNullableInt32(oid, address, index, value);
        }

        public static void WriteInt64(ulong oid, ulong address, int index, long value) {
            DbStateRedirect.Target.WriteInt64(oid, address, index, value);
        }

        public static void WriteNullableInt64(ulong oid, ulong address, int index, long? value) {
            DbStateRedirect.Target.WriteNullableInt64(oid, address, index, value);
        }

        public static void WriteObject(ulong oid, ulong address, int index, Binding.IObjectProxy value) {
            DbStateRedirect.Target.WriteObject(oid, address, index, value);
        }

        public static void WriteSByte(ulong oid, ulong address, int index, sbyte value) {
            DbStateRedirect.Target.WriteSByte(oid, address, index, value);
        }

        public static void WriteNullableSByte(ulong oid, ulong address, int index, sbyte? value) {
            DbStateRedirect.Target.WriteNullableSByte(oid, address, index, value);
        }

        public static void WriteSingle(ulong oid, ulong address, int index, float value) {
            DbStateRedirect.Target.WriteSingle(oid, address, index, value);
        }

        public static void WriteNullableSingle(ulong oid, ulong address, int index, float? value) {
            DbStateRedirect.Target.WriteNullableSingle(oid, address, index, value);
        }

        public static void WriteString(ulong oid, ulong address, int index, string value) {
            DbStateRedirect.Target.WriteString(oid, address, index, value);
        }

        public static void WriteTimeSpan(ulong oid, ulong address, int index, TimeSpan value) {
            DbStateRedirect.Target.WriteTimeSpan(oid, address, index, value);
        }

        public static void WriteNullableTimeSpan(ulong oid, ulong address, int index, TimeSpan? value) {
            DbStateRedirect.Target.WriteNullableTimeSpan(oid, address, index, value);
        }

        public static void WriteTimeSpanEx(ulong oid, ulong address, int index, long value) {
            DbStateRedirect.Target.WriteTimeSpanEx(oid, address, index, value);
        }

        public static void WriteUInt16(ulong oid, ulong address, int index, ushort value) {
            DbStateRedirect.Target.WriteUInt16(oid, address, index, value);
        }

        public static void WriteNullableUInt16(ulong oid, ulong address, int index, ushort? value) {
            DbStateRedirect.Target.WriteNullableUInt16(oid, address, index, value);
        }

        public static void WriteUInt32(ulong oid, ulong address, int index, uint value) {
            DbStateRedirect.Target.WriteUInt32(oid, address, index, value);
        }

        public static void WriteNullableUInt32(ulong oid, ulong address, int index, uint? value) {
            DbStateRedirect.Target.WriteNullableUInt32(oid, address, index, value);
        }

        public static void WriteUInt64(ulong oid, ulong address, int index, ulong value) {
            DbStateRedirect.Target.WriteUInt64(oid, address, index, value);
        }

        public static void WriteNullableUInt64(ulong oid, ulong address, int index, ulong? value) {
            DbStateRedirect.Target.WriteNullableUInt64(oid, address, index, value);
        }

        public static void WriteBinary(ulong oid, ulong address, int index, Binary value) {
            DbStateRedirect.Target.WriteBinary(oid, address, index, value);
        }

        public static void WriteLargeBinary(ulong oid, ulong address, int index, LargeBinary value) {
            DbStateRedirect.Target.WriteLargeBinary(oid, address, index, value);
        }
    }
}
