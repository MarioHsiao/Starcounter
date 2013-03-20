// ***********************************************************************
// <copyright file="DbState.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Internal;
using System;


namespace Starcounter.Internal
{

    /// <summary>
    /// Class DbState
    /// </summary>
    public static class DbState
    {
        /// <summary>
        /// Defines a set of read-only methods used by the weaver to emit
        /// calls from weave-time implementations of <see cref="IObjectView"/>
        /// that must call into Starcounter.
        /// </summary>
        /// <remarks>
        /// This alternative allows us to keep the visibility of TypeBinding
        /// and PropertyBinding intact. If we find it makes for slower calls
        /// we have to adapt the call layer using IObjectView.
        /// </remarks>
        public static class View {

            public static bool? GetBoolean(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetBoolean(proxy);
            }

            public static Binary? GetBinary(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetBinary(proxy);
            }

            public static byte? GetByte(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetByte(proxy);
            }

            public static DateTime? GetDateTime(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetDateTime(proxy);
            }

            public static decimal? GetDecimal(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetDecimal(proxy);
            }

            public static double? GetDouble(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetDouble(proxy);
            }

            public static short? GetInt16(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetInt16(proxy);
            }

            public static int? GetInt32(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetInt32(proxy);
            }

            public static long? GetInt64(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetInt64(proxy);
            }

            public static IObjectView GetObject(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetObject(proxy);
            }

            public static sbyte? GetSByte(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetSByte(proxy);
            }

            public static float? GetSingle(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetSingle(proxy);
            }

            public static string GetString(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetString(proxy);
            }

            public static ushort? GetUInt16(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetUInt16(proxy);
            }

            public static uint? GetUInt32(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetUInt32(proxy);
            }

            public static ulong? GetUInt64(TypeBinding binding, int index, object proxy) {
                return binding.GetPropertyBinding(index).GetUInt64(proxy);
            }
        }

        /// <summary>
        /// Inserts a new object/record of the specified type.
        /// </summary>
        /// <param name="tableId">Identity of the table to insert into.</param>
        /// <param name="oid">A new unique identity, assigned before this method
        /// returns.</param>
        /// <param name="address">The current (opaque) address of the new object
        /// in the database, assigned before this method returns.</param>
        public static void Insert(ushort tableId, ref ulong oid, ref ulong address) {
            uint dr;
            ulong oid_local;
            ulong addr_local;

            unsafe {
                dr = sccoredb.sccoredb_insert(tableId, &oid_local, &addr_local);
            }
            if (dr == 0) {
                oid = oid_local;
                address = addr_local;
                return;
            }

            throw ErrorCode.ToException(dr);
        }

        /// <summary>
        /// Reads the boolean.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Boolean.</returns>
        public static Boolean ReadBoolean(Entity obj, Int32 index)
        {
            Byte value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadBool2(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                return (value == 1);
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable boolean.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Boolean}.</returns>
        public static Nullable<Boolean> ReadNullableBoolean(Entity obj, Int32 index)
        {
            Byte value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadBool2(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                {
                    return (value == 1);
                }
                return null;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Byte.</returns>
        public static Byte ReadByte(Entity obj, Int32 index)
        {
            return (Byte)ReadUInt64(obj, index);
        }

        /// <summary>
        /// Reads the nullable byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Byte}.</returns>
        public static Nullable<Byte> ReadNullableByte(Entity obj, Int32 index)
        {
            return (Byte?)ReadNullableUInt64(obj, index);
        }

        /// <summary>
        /// Reads the date time.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>DateTime.</returns>
        public static DateTime ReadDateTime(Entity obj, Int32 index)
        {
            UInt64 ticks;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadUInt64(thisRef.ObjectID, thisRef.ETI, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
                {
                    return DateTime.MinValue;
                }
                return new DateTime((Int64)ticks);
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable date time.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{DateTime}.</returns>
        public static Nullable<DateTime> ReadNullableDateTime(Entity obj, Int32 index)
        {
            UInt64 ticks;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadUInt64(thisRef.ObjectID, thisRef.ETI, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
                {
                    return null;
                }
                return new DateTime((Int64)ticks);
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the decimal.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Decimal.</returns>
        public static Decimal ReadDecimal(Entity obj, Int32 index)
        {
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                Int32* pArray4;
                flags = sccoredb.SCObjectReadDecimal2(thisRef.ObjectID, thisRef.ETI, index, &pArray4);

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
                {
                    return new Decimal(
                        pArray4[0],
                        pArray4[1],
                        pArray4[2],
                        (pArray4[3] & 0x80000000) != 0,
                        (Byte)(pArray4[3] >> 16)
                    );
                }
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable decimal.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Decimal}.</returns>
        public static Nullable<Decimal> ReadNullableDecimal(Entity obj, Int32 index)
        {
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                Int32* pArray4;
                flags = sccoredb.SCObjectReadDecimal2(thisRef.ObjectID, thisRef.ETI, index, &pArray4);

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
                {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                    {
                        return new Decimal(
                            pArray4[0],
                            pArray4[1],
                            pArray4[2],
                            (pArray4[3] & 0x80000000) != 0,
                            (Byte)(pArray4[3] >> 16)
                            );
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the double.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Double.</returns>
        public static Double ReadDouble(Entity obj, Int32 index)
        {
            Double value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadDouble(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable double.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Double}.</returns>
        public static Nullable<Double> ReadNullableDouble(Entity obj, Int32 index)
        {
            Double value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadDouble(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
                {
                    return null;
                }
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Int16.</returns>
        public static Int16 ReadInt16(Entity obj, Int32 index)
        {
            return (Int16)ReadInt64(obj, index);
        }

        /// <summary>
        /// Reads the nullable int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Int16}.</returns>
        public static Nullable<Int16> ReadNullableInt16(Entity obj, Int32 index)
        {
            return (Int16?)ReadNullableInt64(obj, index);
        }

        /// <summary>
        /// Reads the int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Int32.</returns>
        public static Int32 ReadInt32(Entity obj, Int32 index)
        {
            return (Int32)ReadInt64(obj, index);
        }

        /// <summary>
        /// Reads the nullable int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Int32}.</returns>
        public static Nullable<Int32> ReadNullableInt32(Entity obj, Int32 index)
        {
            return (Int32?)ReadNullableInt64(obj, index);
        }

        /// <summary>
        /// Reads the int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Int64.</returns>
        public static Int64 ReadInt64(Entity obj, Int32 index)
        {
            Int64 value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadInt64(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Int64}.</returns>
        public static Nullable<Int64> ReadNullableInt64(Entity obj, Int32 index)
        {
            Int64 value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadInt64(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
                {
                    return null;
                }
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Entity.</returns>
        public static IObjectView ReadObject(Entity obj, Int32 index)
        {
            ObjectRef thisRef;
            UInt16 flags;
            ObjectRef value;
            UInt16 cci;
            UInt32 ec;
            thisRef = obj.ThisRef;
            flags = 0;
            unsafe
            {
                sccoredb.Mdb_ObjectReadObjRef(
                    thisRef.ObjectID,
                    thisRef.ETI,
                    index,
                    &value.ObjectID,
                    &value.ETI,
                    &cci,
                    &flags
                );
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                {
                    return Bindings.GetTypeBinding(cci).NewInstance(value.ETI, value.ObjectID);
                }
                return null;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the S byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>SByte.</returns>
        public static SByte ReadSByte(Entity obj, Int32 index)
        {
            return (SByte)ReadInt64(obj, index);
        }

        /// <summary>
        /// Reads the nullable S byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{SByte}.</returns>
        public static Nullable<SByte> ReadNullableSByte(Entity obj, Int32 index)
        {
            return (SByte?)ReadNullableInt64(obj, index);
        }

        /// <summary>
        /// Reads the single.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Single.</returns>
        public static Single ReadSingle(Entity obj, Int32 index)
        {
            Single value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadSingle(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable single.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{Single}.</returns>
        public static Nullable<Single> ReadNullableSingle(Entity obj, Int32 index)
        {
            Single value;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadSingle(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
                {
                    return null;
                }
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the string.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>String.</returns>
        public static String ReadStringFromEntity(Entity obj, Int32 index) {
            return ReadString(obj.ThisRef.ObjectID, obj.ThisRef.ETI, index);
        }

        /// <summary>
        /// Reads a database string.
        /// </summary>
        /// <param name="oid">The identity of the object to read the value
        /// from.</param>
        /// <param name="address">The last-known address of the object to
        /// read the value from.</param>
        /// <param name="index">The index of the string inside the object.
        /// </param>
        /// <returns>The value of the string at the given index.</returns>
        public static string ReadString(ulong oid, ulong address, int index) {
            unsafe {
                UInt16 flags;
                Byte* value;
                Int32 sl;
                UInt32 ec;

                flags = sccoredb.SCObjectReadStringW2(
                    oid,
                    address,
                    index,
                    &value
                );

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
                        sl = *((Int32*)value);
                        return new String((Char*)(value + 4), 0, sl);
                    }
                    return null;
                }

                ec = sccoredb.Mdb_GetLastError();
                throw ErrorCode.ToException(ec);
            }
        }


        /// <summary>
        /// Reads the binary.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Binary.</returns>
        public static Binary ReadBinary(Entity obj, Int32 index)
        {
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                Byte* pValue;
                flags = sccoredb.Mdb_ObjectReadBinary(thisRef.ObjectID, thisRef.ETI, index, &pValue);

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
                {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                    {
                        return Binary.FromNative(pValue);
                    }
                    return Binary.Null;
                }
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the large binary.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>LargeBinary.</returns>
        public static LargeBinary ReadLargeBinary(Entity obj, Int32 index)
        {
            UInt16 flags;
            ObjectRef thisRef;
            thisRef = obj.ThisRef;
            unsafe
            {
                Byte* pValue;
                flags = sccoredb.SCObjectReadLargeBinary(thisRef.ObjectID, thisRef.ETI, index, &pValue);

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
                {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                    {
                        return LargeBinary.FromNative(pValue);
                    }
                    return LargeBinary.Null;
                }
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Reads the time span.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>TimeSpan.</returns>
        public static TimeSpan ReadTimeSpan(Entity obj, Int32 index)
        {
            return new TimeSpan(ReadTimeSpanEx(obj, index));
        }

        /// <summary>
        /// Reads the nullable time span.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{TimeSpan}.</returns>
        public static Nullable<TimeSpan> ReadNullableTimeSpan(Entity obj, Int32 index)
        {
            UInt64 ticks;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadUInt64(thisRef.ObjectID, thisRef.ETI, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) != 0)
            {
                ec = sccoredb.Mdb_GetLastError();
                throw ErrorCode.ToException(ec);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
            {
                return null;
            }
            return new Nullable<TimeSpan>(new TimeSpan((Int64)ticks));
        }

        /// <summary>
        /// Reads the time span ex.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Int64.</returns>
        private static Int64 ReadTimeSpanEx(Entity obj, Int32 index)
        {
            UInt64 ticks;
            UInt16 flags;
            ObjectRef thisRef;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadUInt64(thisRef.ObjectID, thisRef.ETI, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) != 0)
            {
                ec = sccoredb.Mdb_GetLastError();
                throw ErrorCode.ToException(ec);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
            {
                return TimeSpan.MinValue.Ticks;
            }
            return (Int64)ticks;
        }

        /// <summary>
        /// Reads the U int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>UInt16.</returns>
        public static UInt16 ReadUInt16(Entity obj, Int32 index)
        {
            return (UInt16)ReadUInt64(obj, index);
        }

        /// <summary>
        /// Reads the nullable U int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{UInt16}.</returns>
        public static Nullable<UInt16> ReadNullableUInt16(Entity obj, Int32 index)
        {
            return (UInt16?)ReadNullableUInt64(obj, index);
        }

        /// <summary>
        /// Reads the U int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>UInt32.</returns>
        public static UInt32 ReadUInt32(Entity obj, Int32 index)
        {
            return (UInt32)ReadUInt64(obj, index);
        }

        /// <summary>
        /// Reads the nullable U int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{UInt32}.</returns>
        public static Nullable<UInt32> ReadNullableUInt32(Entity obj, Int32 index)
        {
            return (UInt32?)ReadNullableUInt64(obj, index);
        }

        /// <summary>
        /// Reads the U int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>UInt64.</returns>
        public static UInt64 ReadUInt64(Entity obj, Int32 index)
        {
            ObjectRef thisRef;
            UInt16 flags;
            UInt64 value;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadUInt64(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Reads the nullable U int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <returns>Nullable{UInt64}.</returns>
        public static Nullable<UInt64> ReadNullableUInt64(Entity obj, Int32 index)
        {
            ObjectRef thisRef;
            UInt16 flags;
            UInt64 value;
            UInt32 ec;
            thisRef = obj.ThisRef;
            unsafe
            {
                flags = sccoredb.Mdb_ObjectReadUInt64(thisRef.ObjectID, thisRef.ETI, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0)
                {
                    return null;
                }
                return value;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Writes the boolean.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteBoolean(Entity obj, Int32 index, Boolean value)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteBool2(thisRef.ObjectID, thisRef.ETI, index, value ? (Byte)1 : (Byte)0);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the nullable boolean.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableBoolean(Entity obj, Int32 index, Nullable<Boolean> value)
        {
            if (value.HasValue)
            {
                WriteBoolean(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteByte(Entity obj, Int32 index, Byte value)
        {
            WriteUInt64(obj, index, value);
        }

        /// <summary>
        /// Writes the nullable byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableByte(Entity obj, Int32 index, Nullable<Byte> value)
        {
            if (value.HasValue)
            {
                WriteByte(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the date time.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteDateTime(Entity obj, Int32 index, DateTime value)
        {
            WriteDateTimeEx(obj, index, value.Ticks);
        }

        /// <summary>
        /// Writes the nullable date time.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableDateTime(Entity obj, Int32 index, Nullable<DateTime> value)
        {
            if (value.HasValue)
            {
                WriteDateTimeEx(obj, index, value.Value.Ticks);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the date time ex.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException">Negative timestamps are currently not supported.</exception>
        private static void WriteDateTimeEx(Entity obj, Int32 index, Int64 value)
        {
            ObjectRef thisRef;
            Boolean br;
            if (value < 0)
            {
                throw new NotImplementedException("Negative timestamps are currently not supported.");
            }
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteUInt64(thisRef.ObjectID, thisRef.ETI, index, (UInt64)value);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the decimal.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteDecimal(Entity obj, Int32 index, Decimal value)
        {
            ObjectRef thisRef;
            Int32[] bits;
            Boolean br;
            bits = Decimal.GetBits(value);
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteDecimal(thisRef.ObjectID, thisRef.ETI, index, bits[0], bits[1], bits[2], bits[3]);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the nullable decimal.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableDecimal(Entity obj, Int32 index, Nullable<Decimal> value)
        {
            if (value.HasValue)
            {
                WriteDecimal(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the double.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteDouble(Entity obj, Int32 index, Double value)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteDouble(thisRef.ObjectID, thisRef.ETI, index, value);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the nullable double.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableDouble(Entity obj, Int32 index, Nullable<Double> value)
        {
            if (value.HasValue)
            {
                WriteDouble(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteInt16(Entity obj, Int32 index, Int16 value)
        {
            WriteInt64(obj, index, value);
        }

        /// <summary>
        /// Writes the nullable int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableInt16(Entity obj, Int32 index, Nullable<Int16> value)
        {
            if (value.HasValue)
            {
                WriteInt16(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteInt32(Entity obj, Int32 index, Int32 value)
        {
            WriteInt64(obj, index, value);
        }

        /// <summary>
        /// Writes the nullable int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableInt32(Entity obj, Int32 index, Nullable<Int32> value)
        {
            if (value.HasValue)
            {
                WriteInt32(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteInt64(Entity obj, Int32 index, Int64 value)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteInt64(thisRef.ObjectID, thisRef.ETI, index, value);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the nullable int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableInt64(Entity obj, Int32 index, Nullable<Int64> value)
        {
            if (value.HasValue)
            {
                WriteInt64(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteObject(Entity obj, Int32 index, Entity value)
        {
            ObjectRef thisRef;
            ObjectRef valueRef;
            Boolean br;
            thisRef = obj.ThisRef;
            if (value != null)
            {
                valueRef = value.ThisRef;
            }
            else
            {
                valueRef.ObjectID = sccoredb.MDBIT_OBJECTID;
                valueRef.ETI = sccoredb.INVALID_RECORD_ADDR;
            }
            br = sccoredb.Mdb_ObjectWriteObjRef(
                     thisRef.ObjectID,
                     thisRef.ETI,
                     index,
                     valueRef.ObjectID,
                     valueRef.ETI
                 );
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the S byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteSByte(Entity obj, Int32 index, SByte value)
        {
            WriteInt64(obj, index, value);
        }

        /// <summary>
        /// Writes the nullable S byte.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableSByte(Entity obj, Int32 index, Nullable<SByte> value)
        {
            if (value.HasValue)
            {
                WriteSByte(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the single.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteSingle(Entity obj, Int32 index, Single value)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteSingle(thisRef.ObjectID, thisRef.ETI, index, value);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the nullable single.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableSingle(Entity obj, Int32 index, Nullable<Single> value)
        {
            if (value.HasValue)
            {
                WriteSingle(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the string.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteStringFromEntity(Entity obj, Int32 index, String value) {
            WriteString(obj.ThisRef.ObjectID, obj.ThisRef.ETI, index, value);
        }

        public static void WriteString(ulong oid, ulong address, int index, string value) {
            Boolean br;
            unsafe {
                fixed (Char* p = value) {
                    br = sccoredb.Mdb_ObjectWriteString16(
                        oid,
                        address,
                        index,
                        p
                    );
                }
            }
            if (br) {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the time span.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteTimeSpan(Entity obj, Int32 index, TimeSpan value)
        {
            WriteTimeSpanEx(obj, index, value.Ticks);
        }

        /// <summary>
        /// Writes the nullable time span.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableTimeSpan(Entity obj, Int32 index, Nullable<TimeSpan> value)
        {
            if (value.HasValue)
            {
                WriteTimeSpan(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the time span ex.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException">Negative timestamps are currently not supported.</exception>
        private static void WriteTimeSpanEx(Entity obj, Int32 index, Int64 value)
        {
            ObjectRef thisRef;
            Boolean br;
            // TODO:
            // DateTime and Timestamp values should be represented as a signed integer
            // in the storage to match use of signed integer in the CLR. Currently they
            // are represented as unsigned integers for reasons unknown. Changing this
            // however will affect alot of code in the kernel aswell as the query
            // language so for now we have to make due with positiv timespans only.
            if (value < 0)
            {
                throw new NotImplementedException("Negative timestamps are currently not supported.");
            }
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteUInt64(thisRef.ObjectID, thisRef.ETI, index, (UInt64)value);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the U int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteUInt16(Entity obj, Int32 index, UInt16 value)
        {
            WriteUInt64(obj, index, value);
        }

        /// <summary>
        /// Writes the nullable U int16.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableUInt16(Entity obj, Int32 index, Nullable<UInt16> value)
        {
            if (value.HasValue)
            {
                WriteUInt16(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the U int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteUInt32(Entity obj, Int32 index, UInt32 value)
        {
            WriteUInt64(obj, index, value);
        }

        /// <summary>
        /// Writes the nullable U int32.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableUInt32(Entity obj, Int32 index, Nullable<UInt32> value)
        {
            if (value.HasValue)
            {
                WriteUInt32(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the U int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteUInt64(Entity obj, Int32 index, UInt64 value)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            br = sccoredb.Mdb_ObjectWriteUInt64(thisRef.ObjectID, thisRef.ETI, index, value);
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Writes the nullable U int64.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteNullableUInt64(Entity obj, Int32 index, Nullable<UInt64> value)
        {
            if (value.HasValue)
            {
                WriteUInt64(obj, index, value.Value);
            }
            else
            {
                WriteNull(obj, index);
            }
        }

        /// <summary>
        /// Writes the binary.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteBinary(Entity obj, Int32 index, Binary value)
        {
            ObjectRef thisRef;
            UInt32 ret;
            thisRef = obj.ThisRef;
            ret = sccoredb.Mdb_ObjectWriteBinary(
                      thisRef.ObjectID,
                      thisRef.ETI,
                      index,
                      value.GetInternalBuffer()
                  );
            if (ret == 0)
            {
                return;
            }
            throw ErrorCode.ToException(ret);
        }

        /// <summary>
        /// Writes the large binary.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void WriteLargeBinary(Entity obj, Int32 index, LargeBinary value)
        {
            ObjectRef thisRef;
            UInt32 ret;
            thisRef = obj.ThisRef;
            ret = sccoredb.SCObjectWriteLargeBinary(
                      thisRef.ObjectID,
                      thisRef.ETI,
                      index,
                      value.GetBuffer()
                  );
            if (ret == 0)
            {
                return;
            }
            throw ErrorCode.ToException(ret);
        }

        /// <summary>
        /// Writes the object.
        /// </summary>
        /// <param name="thisRef">The this ref.</param>
        /// <param name="index">The index.</param>
        /// <param name="valueRef">The value ref.</param>
        internal static void WriteObject(ObjectRef thisRef, Int32 index, ObjectRef valueRef)
        {
            Boolean br;
            br = sccoredb.Mdb_ObjectWriteObjRef(
                     thisRef.ObjectID,
                     thisRef.ETI,
                     index,
                     valueRef.ObjectID,
                     valueRef.ETI
                 );
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// Reads the object.
        /// </summary>
        /// <param name="thisRef">The this ref.</param>
        /// <param name="index">The index.</param>
        /// <returns>Entity.</returns>
        internal static IObjectView ReadObject(ObjectRef thisRef, Int32 index)
        {
            UInt16 flags;
            ObjectRef value;
            UInt16 cci;
            UInt32 ec;
            flags = 0;
            unsafe
            {
                sccoredb.Mdb_ObjectReadObjRef(
                    thisRef.ObjectID,
                    thisRef.ETI,
                    index,
                    &value.ObjectID,
                    &value.ETI,
                    &cci,
                    &flags
                );
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                {
                    return Bindings.GetTypeBinding(cci).NewInstance(value.ETI, value.ObjectID);
                }
                return null;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Writes the null.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="index">The index.</param>
        internal static void WriteNull(Entity obj, int index)
        {
            var thisRef = obj.ThisRef;
            var r = sccoredb.sccoredb_set_null(thisRef.ObjectID, thisRef.ETI, index);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }
    }
}
