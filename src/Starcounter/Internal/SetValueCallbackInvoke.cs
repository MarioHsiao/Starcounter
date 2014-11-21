
using Starcounter.Advanced;
using Starcounter.Binding;
using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Provide the facade to the weaver to generate calls to implementations
    /// of <see cref="ISetValueCallback"/>.
    /// </summary>
    /// <remarks>
    /// Use caution when refactoring this class since it's methods are soft
    /// referenced by name from the weaver, and need to match the names of
    /// corresponding write methods in <see cref="DbState"/>.
    /// </remarks>
    public static class SetValueCallbackInvoke {

        public static void WriteBinary(ISetValueCallback target, Int32 index, Binary value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteBinary(name, value);
        }

        public static void WriteBoolean(ISetValueCallback target, Int32 index, Boolean value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteBoolean(name, value);
        }

        public static void WriteNullableBoolean(ISetValueCallback target, Int32 index, Nullable<Boolean> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteBoolean(name, value);
        }

        public static void WriteByte(ISetValueCallback target, Int32 index, Byte value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteNullableByte(ISetValueCallback target, Int32 index, Nullable<Byte> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteDateTime(ISetValueCallback target, Int32 index, DateTime value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteDateTime(name, value);
        }

        public static void WriteNullableDateTime(ISetValueCallback target, Int32 index, Nullable<DateTime> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteDateTime(name, value);
        }

        public static void WriteDecimal(ISetValueCallback target, Int32 index, Decimal value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteDecimal(name, value);
        }

        public static void WriteNullableDecimal(ISetValueCallback target, Int32 index, Nullable<Decimal> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteDecimal(name, value);
        }

        public static void WriteDouble(ISetValueCallback target, Int32 index, Double value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteDouble(name, value);
        }

        public static void WriteNullableDouble(ISetValueCallback target, Int32 index, Nullable<Double> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteDouble(name, value);
        }

        public static void WriteInt16(ISetValueCallback target, Int32 index, Int16 value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteNullableInt16(ISetValueCallback target, Int32 index, Nullable<Int16> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteInt32(ISetValueCallback target, Int32 index, Int32 value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteNullableInt32(ISetValueCallback target, Int32 index, Nullable<Int32> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteInt64(ISetValueCallback target, Int32 index, Int64 value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteNullableInt64(ISetValueCallback target, Int32 index, Nullable<Int64> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteObject(ISetValueCallback target, Int32 index, IObjectProxy value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteObject(name, value);
        }

        public static void WriteTypeReference(ISetValueCallback target, Int32 index, IObjectProxy value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteObject(name, value);
        }

        public static void WriteInherits(ISetValueCallback target, Int32 index, IObjectProxy value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteObject(name, value);
        }

        public static void WriteTypeName(ISetValueCallback target, Int32 index, string value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteString(name, value);
        }

        public static void WriteSByte(ISetValueCallback target, Int32 index, SByte value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteNullableSByte(ISetValueCallback target, Int32 index, Nullable<SByte> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteInteger(name, value);
        }

        public static void WriteSingle(ISetValueCallback target, Int32 index, Single value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteSingle(name, value);
        }

        public static void WriteNullableSingle(ISetValueCallback target, Int32 index, Nullable<Single> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteSingle(name, value);
        }

        public static void WriteString(ISetValueCallback target, int index, string value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteString(name, value);
        }

        public static void WriteTimeSpan(ISetValueCallback target, Int32 index, TimeSpan value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteTimeSpan(name, value);
        }

        public static void WriteNullableTimeSpan(ISetValueCallback target, Int32 index, Nullable<TimeSpan> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteTimeSpan(name, value);
        }

        public static void WriteUInt16(ISetValueCallback target, Int32 index, UInt16 value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteNullableUInt16(ISetValueCallback target, Int32 index, Nullable<UInt16> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteUInt32(ISetValueCallback target, Int32 index, UInt32 value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteNullableUInt32(ISetValueCallback target, Int32 index, Nullable<UInt32> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteUInt64(ISetValueCallback target, Int32 index, UInt64 value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        public static void WriteNullableUInt64(ISetValueCallback target, Int32 index, Nullable<UInt64> value) {
            var name = AttributeIndexToColumName(target, index);
            target.OnWriteUnsignedInteger(name, value);
        }

        static string AttributeIndexToColumName(ISetValueCallback target, Int32 index) {
            var proxy = (IObjectProxy)target;
            var tb = (TypeBinding)proxy.TypeBinding;
            return tb.TypeDef.TableDef.ColumnDefs[index].Name;
        }
    }
}
