// ***********************************************************************
// <copyright file="DbState.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
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
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Boolean ReadBoolean(ulong oid, ulong address, Int32 index) {
            Byte value;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadBool2(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                return (value == 1);
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Boolean> ReadNullableBoolean(ulong oid, ulong address, Int32 index) {
            Byte value;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadBool2(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
                    return (value == 1);
                }
                return null;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
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
            UInt64 ticks;
            UInt16 flags;
            UInt32 ec;
            unsafe {
                flags = sccoredb.Mdb_ObjectReadUInt64(oid, address, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                    return DateTime.MinValue;
                }
                return new DateTime((Int64)ticks);
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<DateTime> ReadNullableDateTime(ulong oid, ulong address, Int32 index) {
            UInt64 ticks;
            UInt16 flags;
            UInt32 ec;
            
            unsafe {
                flags = sccoredb.Mdb_ObjectReadUInt64(oid, address, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                    return null;
                }
                return new DateTime((Int64)ticks);
            }
            
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

		/// <summary>
		/// Function used by ReadDecimal(). TODO: Place this somewhere else.
		/// </summary>
 		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt16 convert_x6_decimal_to_clr_decimal
		(UInt64 record_id, UInt64 record_addr, Int32 column_index, Int32* decimal_part_ptr);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public static Decimal ReadDecimal(ulong recordID, ulong recordAddr, Int32 columnIndex) {
            Int64 encValue;
            UInt16 flags;

            unsafe {
                flags = sccoredb.sccoredb_get_encdec(recordID, recordAddr, columnIndex, &encValue);
            }

            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                return X6Decimal.ToDecimal(encValue);
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

		/// <summary>
        /// 
        /// </summary>
		/// <param name="recordID"></param>
		/// <param name="recordAddr"></param>
		/// <param name="columnIndex"></param>
        /// <returns></returns>
		public static Decimal ReadDecimal2(ulong recordID, ulong recordAddr, Int32 columnIndex) {
            UInt16 flags;

            unsafe {
				Int32[] decimalPart = new Int32[4];

				fixed (Int32* decimalPartPtr = decimalPart) {
					flags = convert_x6_decimal_to_clr_decimal(recordID, recordAddr, columnIndex,
					decimalPartPtr);

					if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
						return new Decimal(decimalPart[0], decimalPart[1], decimalPart[2],
						(decimalPart[3] & 0x80000000) != 0, (Byte)(decimalPart[3] >> 16));
					}
				}
            }

            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public static Nullable<Decimal> ReadNullableDecimal(ulong recordID, ulong recordAddr, Int32 columnIndex) {
            Int64 encValue;
            UInt16 flags;
            
            unsafe {
                flags = sccoredb.sccoredb_get_encdec(recordID, recordAddr, columnIndex, &encValue);
            }

            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
                    return X6Decimal.ToDecimal(encValue);
                } else {
                    return null;
                }
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="recordID"></param>
		/// <param name="recordAddr"></param>
		/// <param name="columnIndex"></param>
		/// <returns></returns>
		public static Nullable<Decimal> ReadNullableDecimal2(ulong recordID, ulong recordAddr, Int32 columnIndex) {
			UInt16 flags;
			UInt32 ec;

			unsafe {
				Int32[] decimalPart = new Int32[4];

				fixed (Int32* decimalPartPtr = decimalPart) {
					flags = convert_x6_decimal_to_clr_decimal(recordID, recordAddr, columnIndex,
					decimalPartPtr);

					if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
						if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
							return new Decimal(decimalPart[0], decimalPart[1], decimalPart[2],
							(decimalPart[3] & 0x80000000) != 0, (Byte)(decimalPart[3] >> 16));
						} else {
							return null;
						}
					}
				}
			}

			ec = sccoredb.Mdb_GetLastError();
			throw ErrorCode.ToException(ec);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Double ReadDouble(ulong oid, ulong address, Int32 index) {
            Double value;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadDouble(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                return value;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Double> ReadNullableDouble(ulong oid, ulong address, Int32 index) {
            Double value;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadDouble(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                    return null;
                }
                return value;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
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
            Int64 value;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadInt64(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                return value;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Int64> ReadNullableInt64(ulong oid, ulong address, Int32 index) {
            Int64 value;
            UInt16 flags;

            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadInt64(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                    return null;
                }
                return value;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IObjectView ReadObject(ulong oid, ulong address, Int32 index) {
            UInt16 flags;
            ObjectRef value;
            UInt16 cci;
            UInt32 ec;

            flags = 0;
            unsafe {
                sccoredb.Mdb_ObjectReadObjRef(
                    oid,
                    address,
                    index,
                    &value.ObjectID,
                    &value.ETI,
                    &cci,
                    &flags
                );
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
                    return Bindings.GetTypeBinding(cci).NewInstance(value.ETI, value.ObjectID);
                }
                return null;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
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
            Single value;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadSingle(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                return value;
            }
            
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<Single> ReadNullableSingle(ulong oid, ulong address, Int32 index) {
            Single value;
            UInt16 flags;

            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadSingle(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                    return null;
                }
                return value;
            }

            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
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
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Binary ReadBinary(ulong oid, ulong address, Int32 index) {
            UInt16 flags;
            UInt32 ec;

            unsafe {
                Byte* pValue;
                flags = sccoredb.Mdb_ObjectReadBinary(oid, address, index, &pValue);

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
                        return Binary.FromNative(pValue);
                    }
                    return Binary.Null;
                }
            }
            
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static LargeBinary ReadLargeBinary(ulong oid, ulong address, Int32 index) {
            UInt16 flags;

            unsafe {
                Byte* pValue;
                flags = sccoredb.SCObjectReadLargeBinary(oid, address, index, &pValue);

                if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                    if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0) {
                        return LargeBinary.FromNative(pValue);
                    }
                    return LargeBinary.Null;
                }
            }
            
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            UInt64 ticks;
            UInt16 flags;
            UInt32 ec;
            
            unsafe {
                flags = sccoredb.Mdb_ObjectReadUInt64(oid, address, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) != 0) {
                ec = sccoredb.Mdb_GetLastError();
                throw ErrorCode.ToException(ec);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                return null;
            }
            return new Nullable<TimeSpan>(new TimeSpan((Int64)ticks));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static Int64 ReadTimeSpanEx(ulong oid, ulong address, Int32 index) {
            UInt64 ticks;
            UInt16 flags;
            UInt32 ec;

            unsafe {
                flags = sccoredb.Mdb_ObjectReadUInt64(oid, address, index, &ticks);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) != 0) {
                ec = sccoredb.Mdb_GetLastError();
                throw ErrorCode.ToException(ec);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                return TimeSpan.MinValue.Ticks;
            }
            
            return (Int64)ticks;
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
            UInt16 flags;
            UInt64 value;
            UInt32 ec;
            
            unsafe {
                flags = sccoredb.Mdb_ObjectReadUInt64(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                return value;
            }
            
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Nullable<UInt64> ReadNullableUInt64(ulong oid, ulong address, Int32 index) {
            UInt16 flags;
            UInt64 value;
            UInt32 ec;
            
            unsafe {
                flags = sccoredb.Mdb_ObjectReadUInt64(oid, address, index, &value);
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) != 0) {
                    return null;
                }
                return value;
            }
            
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteBoolean(ulong oid, ulong address, Int32 index, Boolean value) {
            Boolean br;
            
            br = sccoredb.Mdb_ObjectWriteBool2(oid, address, index, value ? (Byte)1 : (Byte)0);
            if (br) {
                return;
            }
            
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            Boolean br;
            if (value < 0) {
                throw new NotImplementedException("Negative timestamps are currently not supported.");
            }
            
            br = sccoredb.Mdb_ObjectWriteUInt64(oid, address, index, (UInt64)value);
            if (br) {
                return;
            }
            
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
        }

		/// <summary>
		/// Function used by WriteDecimal(). TODO: Place this somewhere else.
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt32 convert_clr_decimal_to_x6_decimal
		(UInt64 record_id, UInt64 record_addr, Int32 column_index,
		Int32 low, Int32 middle, Int32 high, Int32 scale_sign);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <param name="value"></param>
        public static void WriteDecimal(ulong recordID, ulong recordAddr, Int32 columnIndex, Decimal value) {
            UInt32 ec;
            long encodedValue = X6Decimal.FromDecimal(value);
            
            ec = sccoredb.sccoredb_put_encdec(recordID, recordAddr, (UInt32)columnIndex, encodedValue);
            if (ec != 0)
                throw ErrorCode.ToException(ec);
        }

		/// <summary>
        /// 
        /// </summary>
        /// <param name="recordID"></param>
        /// <param name="recordAddr"></param>
        /// <param name="columnIndex"></param>
        /// <param name="value"></param>
        public static void WriteDecimal2(ulong recordID, ulong recordAddr, Int32 columnIndex, Decimal value) {
			Int32[] decimalPart = Decimal.GetBits(value);
            UInt32 errorCode;

			// convert_clr_decimal_to_x6_decimal() will do the conversion, and if the value fits,
			// it will be written. If it doesn't fit it will not be written and an error code is
			// returned.
			if ((errorCode = convert_clr_decimal_to_x6_decimal(recordID, recordAddr, columnIndex,
			decimalPart[0], decimalPart[1], decimalPart[2], decimalPart[3])) == 0) {
				// The value was written.
                return;
            }
            
			// An exception is thrown because the value did not fit (and was not written.)
            throw ErrorCode.ToException(errorCode);
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
            Boolean br;
            br = sccoredb.Mdb_ObjectWriteDouble(oid, address, index, value);
            if (br) {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            Boolean br;
            br = sccoredb.Mdb_ObjectWriteInt64(oid, address, index, value);
            if (br) {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            Boolean br;
            
            if (value != null) {
                valueRef.ObjectID = value.Identity;
                valueRef.ETI = value.ThisHandle;
            } else {
                valueRef.ObjectID = sccoredb.MDBIT_OBJECTID;
                valueRef.ETI = sccoredb.INVALID_RECORD_ADDR;
            }
            
            br = sccoredb.Mdb_ObjectWriteObjRef(
                     oid,
                     address,
                     index,
                     valueRef.ObjectID,
                     valueRef.ETI
                 );
            if (br) {
                return;
            }
            
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            Boolean br;
            br = sccoredb.Mdb_ObjectWriteSingle(oid, address, index, value);
            if (br) {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            Boolean br;
            // TODO:
            // DateTime and Timestamp values should be represented as a signed integer
            // in the storage to match use of signed integer in the CLR. Currently they
            // are represented as unsigned integers for reasons unknown. Changing this
            // however will affect alot of code in the kernel aswell as the query
            // language so for now we have to make due with positiv timespans only.
            if (value < 0) {
                throw new NotImplementedException("Negative timestamps are currently not supported.");
            }
            br = sccoredb.Mdb_ObjectWriteUInt64(oid, address, index, (UInt64)value);
            if (br) {
                return;
            }
            throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
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
            var br = sccoredb.Mdb_ObjectWriteUInt64(oid, address, index, value);
            if (!br) {
                throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
            }
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
            UInt32 ret;
            ret = sccoredb.Mdb_ObjectWriteBinary(
                      oid,
                      address,
                      index,
                      value.GetInternalBuffer()
                  );
            if (ret == 0) {
                return;
            }
            throw ErrorCode.ToException(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public static void WriteLargeBinary(ulong oid, ulong address, Int32 index, LargeBinary value) {
            UInt32 ret;
            ret = sccoredb.SCObjectWriteLargeBinary(
                      oid,
                      address,
                      index,
                      value.GetBuffer()
                  );
            if (ret == 0) {
                return;
            }
            throw ErrorCode.ToException(ret);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="address"></param>
        /// <param name="index"></param>
        internal static void WriteNull(ulong oid, ulong address, Int32 index) {
            var r = sccoredb.sccoredb_put_default(oid, address, index);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// Function used by CLRDecimalToEncodedX6Decimal(). TODO: Place this somewhere else.
        /// </summary>
        [DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
        public unsafe extern static UInt32 clr_decimal_to_encoded_x6_decimal
        (Int32* decimal_part_ptr, ref Int64 encoded_x6_decimal_ptr);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clrDecimal"></param>
        /// <returns></returns>
        public static Int64 ClrDecimalToEncodedX6Decimal(Decimal clrDecimal) {
            unsafe {
                Int32[] decimalPart = Decimal.GetBits(clrDecimal);
                Int64 encodedX6Decimal = 0;

                fixed (Int32* decimalPartPtr = decimalPart) {
                    // clr_decimal_to_encoded_x6_decimal() will do the conversion, and if the value fits
                    // without data loss, the value will be written to encodedX6Decimal.

                    UInt32 error_code = clr_decimal_to_encoded_x6_decimal(decimalPartPtr, ref encodedX6Decimal);

                    if (error_code == 0) {
                        return encodedX6Decimal;
                    }

                    throw ErrorCode.ToException(error_code);
                }
            }
        }
		
#if false
		/// <summary>
		/// 
		/// </summary>
		/// <param name="clrDecimal"></param>
		/// <param name="encodedX6Decimal"></param>
		/// <returns></returns>
		public static UInt32 ClrDecimalToEncodedX6Decimal(Decimal clrDecimal, out Int64 encodedX6Decimal) {
            unsafe {
				Int32[] decimalPart = Decimal.GetBits(clrDecimal);
				encodedX6Decimal = 0;
				
				fixed (Int32* decimalPartPtr = decimalPart) {
					// clr_decimal_to_encoded_x6_decimal() will do the conversion, and if the value fits
					// without data loss, the value will be written to encodedX6Decimal.
					return clr_decimal_to_encoded_x6_decimal(decimalPartPtr, ref encodedX6Decimal);
				}
			}
		}
#endif
		
        //public const Int64 X6DECIMALMAX = +4398046511103999999;
        //public const Int64 X6DECIMALMIN = -4398046511103999999;
	}
}
