// ***********************************************************************
// <copyright file="DbState.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Advanced;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;


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
            ulong contextHandle;
            uint r;
            ulong oid_local;
            ulong ref_local;

            unsafe {
                string setpec = Starcounter.SqlProcessor.SqlProcessor.GetSetSpecifier(tableId);
                contextHandle = ThreadData.ContextHandle;
                r = sccoredb.star_context_insert(contextHandle, tableId, &oid_local);
                if (r == 0) {
                    ref_local = DbHelper.EncodeObjectRef(0, tableId);
                    fixed (char* p = setpec) {
                        r = sccoredb.star_context_put_setspec(
                            contextHandle, oid_local, ref_local, p
                            );
                    }
                    if (r == 0) {
                        oid = oid_local;
                        address = ref_local;
                        return;
                    }

                    // If insert succeeds then setting set specifier is very unlikely to fail. But
                    // in case it does the transaction will always be aborted. So there is no risk
                    // that there will be records with no set specifier because of this.
                }
                throw ErrorCode.ToException(r);
            }

        }

        internal static void SystemInsert(ushort tableId, ref ulong oid, ref ulong address) {
            ulong contextHandle;
            uint dr;
            ulong oid_local;
            ulong addr_local;

            unsafe {
                string setpec = Starcounter.SqlProcessor.SqlProcessor.GetSetSpecifier(tableId);
                contextHandle = ThreadData.ContextHandle;
                dr = sccoredb.star_context_insert_system(contextHandle, tableId, &oid_local);
                if (dr == 0)
                {
                    addr_local = DbHelper.EncodeObjectRef(0, tableId);
                    fixed (char* p = setpec)
                    {
                        dr = sccoredb.star_context_put_setspec(
                            contextHandle, oid_local, addr_local, p
                            );
                    }
                    if (dr == 0)
                    {
                        oid = oid_local;
                        address = addr_local;
                        return;
                    }
                }
            }
            throw ErrorCode.ToException(dr);
        }

        public static ObjectRef? Lookup(ulong oid)
        {
            ulong ref_local;
            uint r;

            unsafe
            {
                r = sccoredb.star_context_lookup(ThreadData.ContextHandle, oid, &ref_local);
            }

            if (r == 0)
                return new ObjectRef { ObjectID = oid, ETI = ref_local };
            else if (r == Error.SCERRRECORDNOTFOUND)
                return null;
            else
                throw ErrorCode.ToException(r);
        }

        public static void InsertWithId(ushort tableId, ulong oid, out ulong address)
        {
            ulong contextHandle;
            uint r;
            ulong ref_local;

            unsafe
            {
                //look for existing object
                if (Lookup(oid) != null)
                    throw ErrorCode.ToException(Error.SCERRCONSTRAINTVIOLATIONABORT);

                string setpec = Starcounter.SqlProcessor.SqlProcessor.GetSetSpecifier(tableId);
                contextHandle = ThreadData.ContextHandle;
                r = sccoredb.stari_context_insert_with_id(contextHandle, tableId, oid);
                if (r == 0)
                {
                    ref_local = DbHelper.EncodeObjectRef(0, tableId);
                    fixed (char* p = setpec)
                    {
                        r = sccoredb.star_context_put_setspec(
                            contextHandle, oid, ref_local, p
                            );
                    }
                    if (r == 0)
                    {
                        address = ref_local;
                        return;
                    }

                    // If insert succeeds then setting set specifier is very unlikely to fail. But
                    // in case it does the transaction will always be aborted. So there is no risk
                    // that there will be records with no set specifier because of this.
                }
                throw ErrorCode.ToException(r);
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Boolean ReadBoolean(ulong oid, ulong address, Int32 index) {
            ulong value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return (value != 0);
            }
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Boolean> ReadNullableBoolean(ulong oid, ulong address, Int32 index) {
            ulong value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return (value != 0);
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Byte ReadByte(ulong oid, ulong address, Int32 index) {
            return (Byte)ReadUInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Byte> ReadNullableByte(ulong oid, ulong address, Int32 index) {
            return (Byte?)ReadNullableUInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static DateTime ReadDateTime(ulong oid, ulong address, Int32 index) {
            ulong ticks;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &ticks);
            }
            if (r == 0) {
                return new DateTime((long)ticks);
            }
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<DateTime> ReadNullableDateTime(ulong oid, ulong address, Int32 index) {
            ulong ticks;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &ticks);
            }
            if (r == 0) {
                return new DateTime((long)ticks);
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public static Decimal ReadDecimal(ulong recordID, ulong recordAddr, Int32 columnIndex) {
            uint r;
			long value;

            unsafe {
                r = sccoredb.star_context_get_decimal(ThreadData.ContextHandle, recordID, recordAddr, columnIndex, &value);
            }
            if (r == 0) {
                return X6Decimal.FromRaw(value);
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public static Nullable<Decimal> ReadNullableDecimal(ulong recordID, ulong recordAddr, Int32 columnIndex) {
            long value;
            uint r;
            
            unsafe {
                r = sccoredb.star_context_get_decimal(ThreadData.ContextHandle, recordID, recordAddr, columnIndex, &value);
            }
            if (r == 0) {
                return X6Decimal.FromRaw(value);
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Double ReadDouble(ulong oid, ulong address, Int32 index) {
            double value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_double(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Double> ReadNullableDouble(ulong oid, ulong address, Int32 index) {
            double value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_double(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Int16 ReadInt16(ulong oid, ulong address, Int32 index) {
            return (Int16)ReadInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Int16> ReadNullableInt16(ulong oid, ulong address, Int32 index) {
            return (Int16?)ReadNullableInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Int32 ReadInt32(ulong oid, ulong address, Int32 index) {
            return (Int32)ReadInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Int32> ReadNullableInt32(ulong oid, ulong address, Int32 index) {
            return (Int32?)ReadNullableInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Int64 ReadInt64(ulong oid, ulong address, Int32 index) {
            long value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_long(
                    ThreadData.ContextHandle, oid, address, index, &value
                    );
            }
            if (r == 0) {
                return value;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Int64> ReadNullableInt64(ulong oid, ulong address, Int32 index) {
            long value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_long(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IObjectView ReadObject(ulong oid, ulong address, Int32 index) {
            unsafe {
                sccoredb.STAR_REFERENCE_VALUE value;
                uint r = sccoredb.star_context_get_reference(
                    ThreadData.ContextHandle,
                    oid,
                    address,
                    index,
                    &value
                    );
                if (r == 0) {
                    TypeBinding tb = Bindings.GetTypeBinding(value.layout_handle);
                    ulong record_ref = DbHelper.EncodeObjectRef(value.handle.opt, tb.TableId);
                    return tb.NewInstance(record_ref, value.handle.id);
                }
                else if (r == Error.SCERRVALUEUNDEFINED) {
                    return null;
                }
                else {
                    throw ErrorCode.ToException(r);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IObjectView ReadTypeReference(ulong oid, ulong address, Int32 index) {
            return ReadObject(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IObjectView ReadInherits(ulong oid, ulong address, Int32 index) {
            return ReadObject(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string ReadTypeName(ulong oid, ulong address, Int32 index) {
            return ReadString(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static SByte ReadSByte(ulong oid, ulong address, Int32 index) {
            return (SByte)ReadInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<SByte> ReadNullableSByte(ulong oid, ulong address, Int32 index) {
            return (SByte?)ReadNullableInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Single ReadSingle(ulong oid, ulong address, Int32 index) {
            float value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_float(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Single> ReadNullableSingle(ulong oid, ulong address, Int32 index) {
            float value;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_float(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
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
                uint r;
                byte* value;
                int sl;

                r = sccoredb.star_context_get_string(
                    ThreadData.ContextHandle, oid, address, index, &value
                    );
                if (r == 0) {
                    sl = *((Int32*)value);
                    return new String((Char*)(value + 4), 0, sl);
                }
                else if (r == Error.SCERRVALUEUNDEFINED) {
                    return null;
                }
                else {
                    throw ErrorCode.ToException(r);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Binary ReadBinary(ulong oid, ulong address, Int32 index) {
            unsafe {
                byte* pValue;
                uint r;

                r = sccoredb.star_context_get_binary_OLD(ThreadData.ContextHandle, oid, address, index, &pValue);
                if (r == 0) {
                    return Binary.FromNative(pValue);
                }
                else if (r == Error.SCERRVALUEUNDEFINED) {
                    return Binary.Null;
                }
                else {
                    throw ErrorCode.ToException(r);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TimeSpan ReadTimeSpan(ulong oid, ulong address, Int32 index) {
            return new TimeSpan(ReadTimeSpanEx(oid, address, index));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<TimeSpan> ReadNullableTimeSpan(ulong oid, ulong address, Int32 index) {
            ulong ticks;
            uint r;
            
            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &ticks);
            }
            if (r == 0) {
                return new TimeSpan((long)ticks);
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static Int64 ReadTimeSpanEx(ulong oid, ulong address, Int32 index) {
            ulong ticks;
            uint r;

            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &ticks);
            }
            if (r == 0) {
                return (long)ticks;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static UInt16 ReadUInt16(ulong oid, ulong address, Int32 index) {
            return (UInt16)ReadUInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<UInt16> ReadNullableUInt16(ulong oid, ulong address, Int32 index) {
            return (UInt16?)ReadNullableUInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static UInt32 ReadUInt32(ulong oid, ulong address, Int32 index) {
            return (UInt32)ReadUInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<UInt32> ReadNullableUInt32(ulong oid, ulong address, Int32 index) {
            return (UInt32?)ReadNullableUInt64(oid, address, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static UInt64 ReadUInt64(ulong oid, ulong address, Int32 index) {
            uint r;
            ulong value;
            
            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<UInt64> ReadNullableUInt64(ulong oid, ulong address, Int32 index) {
            uint r;
            ulong value;
            
            unsafe {
                r = sccoredb.star_context_get_ulong(ThreadData.ContextHandle, oid, address, index, &value);
            }
            if (r == 0) {
                return value;
            }
            else if (r == Error.SCERRVALUEUNDEFINED) {
                return null;
            }
            else {
                throw ErrorCode.ToException(r);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteBoolean(ulong oid, ulong address, Int32 index, Boolean value) {
            uint r;
            r = sccoredb.star_context_put_ulong(ThreadData.ContextHandle, oid, address, index, value ? 1UL : 0UL);
            if (r == 0) return;

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableBoolean(ulong oid, ulong address, Int32 index, Nullable<Boolean> value) {
            if (value.HasValue) {
                WriteBoolean(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteByte(ulong oid, ulong address, Int32 index, Byte value) {
            WriteUInt64(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableByte(ulong oid, ulong address, Int32 index, Nullable<Byte> value) {
            if (value.HasValue) {
                WriteByte(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteDateTime(ulong oid, ulong address, Int32 index, DateTime value) {
            WriteDateTimeEx(oid, address, index, value.Ticks);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableDateTime(ulong oid, ulong address, Int32 index, Nullable<DateTime> value) {
            if (value.HasValue) {
                WriteDateTimeEx(oid, address, index, value.Value.Ticks);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <exception cref="System.NotImplementedException">Negative timestamps are currently not supported.</exception>
        private static void WriteDateTimeEx(ulong oid, ulong address, Int32 index, Int64 value) {
            uint r;
            if (value < 0) {
                throw new NotImplementedException("Negative timestamps are currently not supported.");
            }
            
            r = sccoredb.star_context_put_ulong(ThreadData.ContextHandle, oid, address, index, (UInt64)value);
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <param name="value"></param>
        public static void WriteDecimal(ulong recordID, ulong recordAddr, Int32 columnIndex, Decimal value) {
            UInt32 ec;
            long value2 = X6Decimal.ToRaw(value);

            ec = sccoredb.star_context_put_decimal(ThreadData.ContextHandle, recordID, recordAddr, columnIndex, value2);
            if (ec == 0)
                return;

            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableDecimal(ulong oid, ulong address, Int32 index, Nullable<Decimal> value) {
            if (value.HasValue) {
                WriteDecimal(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteDouble(ulong oid, ulong address, Int32 index, Double value) {
            uint r;
            r = sccoredb.star_context_put_double(ThreadData.ContextHandle, oid, address, index, value);
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableDouble(ulong oid, ulong address, Int32 index, Nullable<Double> value) {
            if (value.HasValue) {
                WriteDouble(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteInt16(ulong oid, ulong address, Int32 index, Int16 value) {
            WriteInt64(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableInt16(ulong oid, ulong address, Int32 index, Nullable<Int16> value) {
            if (value.HasValue) {
                WriteInt16(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteInt32(ulong oid, ulong address, Int32 index, Int32 value) {
            WriteInt64(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableInt32(ulong oid, ulong address, Int32 index, Nullable<Int32> value) {
            if (value.HasValue) {
                WriteInt32(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteInt64(ulong oid, ulong address, Int32 index, Int64 value) {
            uint r;
            r = sccoredb.star_context_put_long(ThreadData.ContextHandle, oid, address, index, value);
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableInt64(ulong oid, ulong address, Int32 index, Nullable<Int64> value) {
            if (value.HasValue) {
                WriteInt64(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteObject(ulong oid, ulong address, Int32 index, IObjectProxy value) {
            ObjectRef valueRef;
            
            if (value != null) {
                valueRef.ObjectID = value.Identity;
                valueRef.ETI = value.ThisHandle;
            } else {
                valueRef.ObjectID = sccoredb.MDBIT_OBJECTID;
                valueRef.ETI = sccoredb.INVALID_RECORD_REF;
            }

            WriteObjectRaw(oid, address, index, valueRef);
        }

        public static void WriteObjectRaw(ulong oid, ulong address, Int32 index, ObjectRef valueRef)
        {
            uint r;

            r = sccoredb.star_context_put_reference(
                     ThreadData.ContextHandle,
                     oid,
                     address,
                     index,
                     valueRef.ObjectID,
                     valueRef.ETI
                 );
            if (r != 0)
            {
                throw ErrorCode.ToException(r);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteTypeReference(ulong oid, ulong address, Int32 index, IObjectProxy value) {
            WriteObject(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteInherits(ulong oid, ulong address, Int32 index, IObjectProxy value) {
            WriteObject(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteTypeName(ulong oid, ulong address, Int32 index, String value) {
            WriteString(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteSByte(ulong oid, ulong address, Int32 index, SByte value) {
            WriteInt64(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableSByte(ulong oid, ulong address, Int32 index, Nullable<SByte> value) {
            if (value.HasValue) {
                WriteSByte(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteSingle(ulong oid, ulong address, Int32 index, Single value) {
            uint r;
            r = sccoredb.star_context_put_float(ThreadData.ContextHandle, oid, address, index, value);
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableSingle(ulong oid, ulong address, Int32 index, Nullable<Single> value) {
            if (value.HasValue) {
                WriteSingle(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteString(ulong oid, ulong address, int index, string value) {
            uint r;
            unsafe {
                fixed (char* p = value) {
                    r = sccoredb.star_context_put_string(
                        ThreadData.ContextHandle, oid, address, index, p
                        );
                }
            }
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteTimeSpan(ulong oid, ulong address, Int32 index, TimeSpan value) {
            WriteTimeSpanEx(oid, address, index, value.Ticks);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableTimeSpan(ulong oid, ulong address, Int32 index, Nullable<TimeSpan> value) {
            if (value.HasValue) {
                WriteTimeSpan(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <exception cref="System.NotImplementedException">Negative timestamps are currently not supported.</exception>
        private static void WriteTimeSpanEx(ulong oid, ulong address, Int32 index, Int64 value) {
            uint r;
            // TODO:
            // DateTime and Timestamp values should be represented as a signed integer
            // in the storage to match use of signed integer in the CLR. Currently they
            // are represented as unsigned integers for reasons unknown. Changing this
            // however will affect alot of code in the kernel aswell as the query
            // language so for now we have to make due with positiv timespans only.
            if (value < 0) {
                throw new NotImplementedException("Negative timestamps are currently not supported.");
            }
            r = sccoredb.star_context_put_ulong(ThreadData.ContextHandle, oid, address, index, (UInt64)value);
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteUInt16(ulong oid, ulong address, Int32 index, UInt16 value) {
            WriteUInt64(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableUInt16(ulong oid, ulong address, Int32 index, Nullable<UInt16> value) {
            if (value.HasValue) {
                WriteUInt16(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteUInt32(ulong oid, ulong address, Int32 index, UInt32 value) {
            WriteUInt64(oid, address, index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableUInt32(ulong oid, ulong address, Int32 index, Nullable<UInt32> value) {
            if (value.HasValue) {
                WriteUInt32(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteUInt64(ulong oid, ulong address, Int32 index, UInt64 value) {
            var r = sccoredb.star_context_put_ulong(ThreadData.ContextHandle, oid, address, index, value);
            if (r == 0) return;

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteNullableUInt64(ulong oid, ulong address, Int32 index, Nullable<UInt64> value) {
            if (value.HasValue) {
                WriteUInt64(oid, address, index, value.Value);
            } else {
                WriteNull(oid, address, index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteBinary(ulong oid, ulong address, Int32 index, Binary value) {
            uint r;
            r = sccoredb.star_context_put_binary_OLD(
                      ThreadData.ContextHandle,
                      oid,
                      address,
                      index,
                      value.GetInternalBuffer()
                  );
            if (r == 0) {
                return;
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        public static void WriteNull(ulong oid, ulong address, Int32 index) {
            var r = sccoredb.star_context_put_default(ThreadData.ContextHandle, oid, address, index);
            if (r == 0) return;

            throw ErrorCode.ToException(r);
        }
	}
}
