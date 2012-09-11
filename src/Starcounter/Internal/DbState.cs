
using Sc.Server.Binding;
using Starcounter;
using Starcounter.Internal;
using System;


// TODO:
// We must keep DbState in namespace Sc.Server.Internal for now because
// generated code links to this code. To be moved to namespace
// Starcounter.Internal as soon as code generation has been updated.

namespace Sc.Server.Internal //namespace Starcounter.Internal
{

    public static class DbState
    {

        /// <summary>
        /// Inserts a new object/record of the specified type. The given proxy is
        /// assumed to match the type; if it's not, the behaviour is undefined.
        /// </summary>
        /// <remarks>This method is used by the Starcounter database engine and is
        /// not intended for developers.</remarks>
        /// <param name="proxy">
        /// Managed instance of the type we are about to instantiate.</param>
        /// <param name="typeAddr">The address of the type.</param>
        /// <param name="typeBinding">The <see cref="TypeBinding"/> representing the
        /// type to the engine.</param>
        public static void Insert(Entity proxy, ulong typeAddr, TypeBinding typeBinding)
        {
            uint dr;
            ulong oid;
            ulong addr;

            unsafe
            {
                dr = sccoredb.sc_insert(typeAddr, &oid, &addr);
            }
            if (dr != 0) throw ErrorCode.ToException(dr);

            proxy.Attach(addr, oid, typeBinding);
#if false
            try
            {
                proxy.InvokeOnNew();
            }
            catch (Exception exception)
            {
                if (exception is ThreadAbortException) throw;
                throw ErrorCode.ToException(Error.SCERRERRORINHOOKCALLBACK, exception);
            }
#endif
        }

        public static Boolean ReadBoolean(DbObject obj, Int32 index)
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

        public static Nullable<Boolean> ReadNullableBoolean(DbObject obj, Int32 index)
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

        public static Byte ReadByte(DbObject obj, Int32 index)
        {
            return (Byte)ReadUInt64(obj, index);
        }

        public static Nullable<Byte> ReadNullableByte(DbObject obj, Int32 index)
        {
            return (Byte?)ReadNullableUInt64(obj, index);
        }

        public static DateTime ReadDateTime(DbObject obj, Int32 index)
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

        public static Nullable<DateTime> ReadNullableDateTime(DbObject obj, Int32 index)
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

        public static Decimal ReadDecimal(DbObject obj, Int32 index)
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

        public static Nullable<Decimal> ReadNullableDecimal(DbObject obj, Int32 index)
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

        public static Double ReadDouble(DbObject obj, Int32 index)
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

        public static Nullable<Double> ReadNullableDouble(DbObject obj, Int32 index)
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

        public static Int16 ReadInt16(DbObject obj, Int32 index)
        {
            return (Int16)ReadInt64(obj, index);
        }

        public static Nullable<Int16> ReadNullableInt16(DbObject obj, Int32 index)
        {
            return (Int16?)ReadNullableInt64(obj, index);
        }

        public static Int32 ReadInt32(DbObject obj, Int32 index)
        {
            return (Int32)ReadInt64(obj, index);
        }

        public static Nullable<Int32> ReadNullableInt32(DbObject obj, Int32 index)
        {
            return (Int32?)ReadNullableInt64(obj, index);
        }

        public static Int64 ReadInt64(DbObject obj, Int32 index)
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

        public static Nullable<Int64> ReadNullableInt64(DbObject obj, Int32 index)
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

        public static Entity ReadObject(DbObject obj, Int32 index)
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
                    return TypeRepository.GetTypeBinding(cci).NewInstance(value.ETI, value.ObjectID);
                }
                return null;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        public static SByte ReadSByte(DbObject obj, Int32 index)
        {
            return (SByte)ReadInt64(obj, index);
        }

        public static Nullable<SByte> ReadNullableSByte(DbObject obj, Int32 index)
        {
            return (SByte?)ReadNullableInt64(obj, index);
        }

        public static Single ReadSingle(DbObject obj, Int32 index)
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

        public static Nullable<Single> ReadNullableSingle(DbObject obj, Int32 index)
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

        public static String ReadString(DbObject obj, Int32 index)
        {
            unsafe
            {
                ObjectRef thisRef;
                UInt16 flags;
                Byte* value;
                Int32 sl;
                UInt32 ec;

                thisRef = obj.ThisRef;

                flags = sccoredb.SCObjectReadStringW2(
                    thisRef.ObjectID,
                    thisRef.ETI,
                    index,
                    &value
                );

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
                {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                    {
                        sl = *((Int32*)value);
                        return new String((Char*)(value + 4), 0, sl);
                    }
                    return null;
                }

                ec = sccoredb.Mdb_GetLastError();
                throw ErrorCode.ToException(ec);
            }
        }

        public static Binary ReadBinary(DbObject obj, Int32 index)
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

        public static LargeBinary ReadLargeBinary(DbObject obj, Int32 index)
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

        public static TimeSpan ReadTimeSpan(DbObject obj, Int32 index)
        {
            return new TimeSpan(ReadTimeSpanEx(obj, index));
        }

        public static Nullable<TimeSpan> ReadNullableTimeSpan(DbObject obj, Int32 index)
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

        private static Int64 ReadTimeSpanEx(DbObject obj, Int32 index)
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

        public static UInt16 ReadUInt16(DbObject obj, Int32 index)
        {
            return (UInt16)ReadUInt64(obj, index);
        }

        public static Nullable<UInt16> ReadNullableUInt16(DbObject obj, Int32 index)
        {
            return (UInt16?)ReadNullableUInt64(obj, index);
        }

        public static UInt32 ReadUInt32(DbObject obj, Int32 index)
        {
            return (UInt32)ReadUInt64(obj, index);
        }

        public static Nullable<UInt32> ReadNullableUInt32(DbObject obj, Int32 index)
        {
            return (UInt32?)ReadNullableUInt64(obj, index);
        }

        public static UInt64 ReadUInt64(DbObject obj, Int32 index)
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

        public static Nullable<UInt64> ReadNullableUInt64(DbObject obj, Int32 index)
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

        public static void WriteBoolean(DbObject obj, Int32 index, Boolean value)
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

        public static void WriteNullableBoolean(DbObject obj, Int32 index, Nullable<Boolean> value)
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

        public static void WriteByte(DbObject obj, Int32 index, Byte value)
        {
            WriteUInt64(obj, index, value);
        }

        public static void WriteNullableByte(DbObject obj, Int32 index, Nullable<Byte> value)
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

        public static void WriteDateTime(DbObject obj, Int32 index, DateTime value)
        {
            WriteDateTimeEx(obj, index, value.Ticks);
        }

        public static void WriteNullableDateTime(DbObject obj, Int32 index, Nullable<DateTime> value)
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

        private static void WriteDateTimeEx(DbObject obj, Int32 index, Int64 value)
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

        public static void WriteDecimal(DbObject obj, Int32 index, Decimal value)
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

        public static void WriteNullableDecimal(DbObject obj, Int32 index, Nullable<Decimal> value)
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

        public static void WriteDouble(DbObject obj, Int32 index, Double value)
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

        public static void WriteNullableDouble(DbObject obj, Int32 index, Nullable<Double> value)
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

        public static void WriteInt16(DbObject obj, Int32 index, Int16 value)
        {
            WriteInt64(obj, index, value);
        }

        public static void WriteNullableInt16(DbObject obj, Int32 index, Nullable<Int16> value)
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

        public static void WriteInt32(DbObject obj, Int32 index, Int32 value)
        {
            WriteInt64(obj, index, value);
        }

        public static void WriteNullableInt32(DbObject obj, Int32 index, Nullable<Int32> value)
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

        public static void WriteInt64(DbObject obj, Int32 index, Int64 value)
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

        public static void WriteNullableInt64(DbObject obj, Int32 index, Nullable<Int64> value)
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

        public static void WriteObject(DbObject obj, Int32 index, Entity value)
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

        public static void WriteSByte(DbObject obj, Int32 index, SByte value)
        {
            WriteInt64(obj, index, value);
        }

        public static void WriteNullableSByte(DbObject obj, Int32 index, Nullable<SByte> value)
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

        public static void WriteSingle(DbObject obj, Int32 index, Single value)
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

        public static void WriteNullableSingle(DbObject obj, Int32 index, Nullable<Single> value)
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

        public static void WriteString(DbObject obj, Int32 index, String value)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            unsafe
            {
                fixed (Char* p = value)
                {
                    br = sccoredb.Mdb_ObjectWriteString16(
                        thisRef.ObjectID,
                        thisRef.ETI,
                        index,
                        p
                    );
                }
            }
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        public static void WriteTimeSpan(DbObject obj, Int32 index, TimeSpan value)
        {
            WriteTimeSpanEx(obj, index, value.Ticks);
        }

        public static void WriteNullableTimeSpan(DbObject obj, Int32 index, Nullable<TimeSpan> value)
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

        private static void WriteTimeSpanEx(DbObject obj, Int32 index, Int64 value)
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

        public static void WriteUInt16(DbObject obj, Int32 index, UInt16 value)
        {
            WriteUInt64(obj, index, value);
        }

        public static void WriteNullableUInt16(DbObject obj, Int32 index, Nullable<UInt16> value)
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

        public static void WriteUInt32(DbObject obj, Int32 index, UInt32 value)
        {
            WriteUInt64(obj, index, value);
        }

        public static void WriteNullableUInt32(DbObject obj, Int32 index, Nullable<UInt32> value)
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

        public static void WriteUInt64(DbObject obj, Int32 index, UInt64 value)
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

        public static void WriteNullableUInt64(DbObject obj, Int32 index, Nullable<UInt64> value)
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

        public static void WriteBinary(DbObject obj, Int32 index, Binary value)
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

        public static void WriteLargeBinary(DbObject obj, Int32 index, LargeBinary value)
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

        internal static Entity ReadObject(ObjectRef thisRef, Int32 index)
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
                    return TypeRepository.GetTypeBinding(cci).NewInstance(value.ETI, value.ObjectID);
                }
                return null;
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        internal static void WriteNull(DbObject obj, Int32 index)
        {
            ObjectRef thisRef;
            Boolean br;
            thisRef = obj.ThisRef;
            unsafe
            {
                br = sccoredb.Mdb_ObjectWriteAttributeState(thisRef.ObjectID, thisRef.ETI, index, sccoredb.Mdb_DataValueFlag_Null);
            }
            if (br)
            {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }
    }
}
