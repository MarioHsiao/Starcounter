
using Starcounter.Binding;
using System;

namespace Starcounter.Hosting {

    public interface IDbState {

        void Insert(ushort tableId, ref ulong oid, ref ulong address);
        bool ReadBoolean(ulong oid, ulong address, int index);
        Nullable<bool> ReadNullableBoolean(ulong oid, ulong address, int index);
        byte ReadByte(ulong oid, ulong address, int index);
        Nullable<byte> ReadNullableByte(ulong oid, ulong address, int index);
        DateTime ReadDateTime(ulong oid, ulong address, int index);
        Nullable<DateTime> ReadNullableDateTime(ulong oid, ulong address, int index);
        decimal ReadDecimal(ulong oid, ulong address, int index);
        Nullable<decimal> ReadNullableDecimal(ulong oid, ulong address, int index);
        double ReadDouble(ulong oid, ulong address, int index);
        Nullable<double> ReadNullableDouble(ulong oid, ulong address, int index);
        short ReadInt16(ulong oid, ulong address, int index);
        Nullable<short> ReadNullableInt16(ulong oid, ulong address, int index);
        int ReadInt32(ulong oid, ulong address, int index);
        Nullable<int> ReadNullableInt32(ulong oid, ulong address, int index);
        long ReadInt64(ulong oid, ulong address, int index);
        Nullable<long> ReadNullableInt64(ulong oid, ulong address, int index);
        IObjectView ReadObject(ulong oid, ulong address, int index);
        sbyte ReadSByte(ulong oid, ulong address, int index);
        Nullable<sbyte> ReadNullableSByte(ulong oid, ulong address, int index);
        float ReadSingle(ulong oid, ulong address, int index);
        Nullable<float> ReadNullableSingle(ulong oid, ulong address, int index);
        string ReadString(ulong oid, ulong address, int index);
        Binary ReadBinary(ulong oid, ulong address, int index);
        LargeBinary ReadLargeBinary(ulong oid, ulong address, int index);
        TimeSpan ReadTimeSpan(ulong oid, ulong address, int index);
        Nullable<TimeSpan> ReadNullableTimeSpan(ulong oid, ulong address, int index);
        long ReadTimeSpanEx(ulong oid, ulong address, int index);
        ushort ReadUInt16(ulong oid, ulong address, int index);
        Nullable<ushort> ReadNullableUInt16(ulong oid, ulong address, int index);
        uint ReadUInt32(ulong oid, ulong address, int index);
        Nullable<uint> ReadNullableUInt32(ulong oid, ulong address, int index);
        ulong ReadUInt64(ulong oid, ulong address, int index);
        Nullable<ulong> ReadNullableUInt64(ulong oid, ulong address, int index);
        void WriteBoolean(ulong oid, ulong address, int index, bool value);
        void WriteNullableBoolean(ulong oid, ulong address, int index, Nullable<bool> value);
        void WriteByte(ulong oid, ulong address, int index, byte value);
        void WriteNullableByte(ulong oid, ulong address, int index, Nullable<byte> value);
        void WriteDateTime(ulong oid, ulong address, int index, DateTime value);
        void WriteNullableDateTime(ulong oid, ulong address, int index, Nullable<DateTime> value);
        void WriteDateTimeEx(ulong oid, ulong address, int index, long value);
        void WriteDecimal(ulong oid, ulong address, int index, decimal value);
        void WriteNullableDecimal(ulong oid, ulong address, int index, Nullable<decimal> value);
        void WriteDouble(ulong oid, ulong address, int index, double value);
        void WriteNullableDouble(ulong oid, ulong address, int index, Nullable<double> value);
        void WriteInt16(ulong oid, ulong address, int index, short value);
        void WriteNullableInt16(ulong oid, ulong address, int index, Nullable<short> value);
        void WriteInt32(ulong oid, ulong address, int index, int value);
        void WriteNullableInt32(ulong oid, ulong address, int index, Nullable<int> value);
        void WriteInt64(ulong oid, ulong address, int index, long value);
        void WriteNullableInt64(ulong oid, ulong address, int index, Nullable<long> value);
        void WriteObject(ulong oid, ulong address, int index, IObjectProxy value);
        void WriteSByte(ulong oid, ulong address, int index, sbyte value);
        void WriteNullableSByte(ulong oid, ulong address, int index, Nullable<sbyte> value);
        void WriteSingle(ulong oid, ulong address, int index, float value);
        void WriteNullableSingle(ulong oid, ulong address, int index, Nullable<float> value);
        void WriteString(ulong oid, ulong address, int index, string value);
        void WriteTimeSpan(ulong oid, ulong address, int index, TimeSpan value);
        void WriteNullableTimeSpan(ulong oid, ulong address, int index, Nullable<TimeSpan> value);
        void WriteTimeSpanEx(ulong oid, ulong address, int index, long value);
        void WriteUInt16(ulong oid, ulong address, int index, ushort value);
        void WriteNullableUInt16(ulong oid, ulong address, int index, Nullable<ushort> value);
        void WriteUInt32(ulong oid, ulong address, int index, uint value);
        void WriteNullableUInt32(ulong oid, ulong address, int index, Nullable<uint> value);
        void WriteUInt64(ulong oid, ulong address, int index, ulong value);
        void WriteNullableUInt64(ulong oid, ulong address, int index, Nullable<ulong> value);
        void WriteBinary(ulong oid, ulong address, int index, Binary value);
        void WriteLargeBinary(ulong oid, ulong address, int index, LargeBinary value);
    }
}
