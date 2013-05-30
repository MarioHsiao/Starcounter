// ***********************************************************************
// <copyright file="BindingHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class BindingHelper
    /// </summary>
    internal static class BindingHelper
    {

        /// <summary>
        /// Converts the db type code to sc type code.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>System.Byte.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        internal static byte ConvertDbTypeCodeToScTypeCode(DbTypeCode t)
        {
            switch (t)
            {
            case DbTypeCode.Boolean: return sccoredb.Mdb_Type_Boolean;
            case DbTypeCode.Byte: return sccoredb.Mdb_Type_Byte;
            case DbTypeCode.DateTime: return sccoredb.Mdb_Type_DateTime;
            case DbTypeCode.Decimal: return sccoredb.Mdb_Type_Decimal;
            case DbTypeCode.Single: return sccoredb.Mdb_Type_Single;
            case DbTypeCode.Double: return sccoredb.Mdb_Type_Double;
            case DbTypeCode.Int64: return sccoredb.Mdb_Type_Int64;
            case DbTypeCode.Int32: return sccoredb.Mdb_Type_Int32;
            case DbTypeCode.Int16: return sccoredb.Mdb_Type_Int16;
            case DbTypeCode.Object: return sccoredb.Mdb_Type_ObjectID;
            case DbTypeCode.SByte: return sccoredb.Mdb_Type_SByte;
            case DbTypeCode.String: return sccoredb.Mdb_Type_String;
            case DbTypeCode.UInt64: return sccoredb.Mdb_Type_UInt64;
            case DbTypeCode.UInt32: return sccoredb.Mdb_Type_UInt32;
            case DbTypeCode.UInt16: return sccoredb.Mdb_Type_UInt16;
            case DbTypeCode.Binary: return sccoredb.Mdb_Type_Binary;
            case DbTypeCode.LargeBinary: return sccoredb.Mdb_Type_LargeBinary;
            case DbTypeCode.Key: return sccoredb.Mdb_Type_ObjectKey;
            default: throw new ArgumentException();
            };
        }

        /// <summary>
        /// Converts the sc type code to db type code.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>DbTypeCode.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        internal static DbTypeCode ConvertScTypeCodeToDbTypeCode(byte t)
        {
            switch (t)
            {
            case sccoredb.Mdb_Type_Boolean: return DbTypeCode.Boolean;
            case sccoredb.Mdb_Type_Byte: return DbTypeCode.Byte;
            case sccoredb.Mdb_Type_DateTime: return DbTypeCode.DateTime;
            case sccoredb.Mdb_Type_Decimal: return DbTypeCode.Decimal;
            case sccoredb.Mdb_Type_Single: return DbTypeCode.Single;
            case sccoredb.Mdb_Type_Double: return DbTypeCode.Double;
            case sccoredb.Mdb_Type_Int64: return DbTypeCode.Int64;
            case sccoredb.Mdb_Type_Int32: return DbTypeCode.Int32;
            case sccoredb.Mdb_Type_Int16: return DbTypeCode.Int16;
            case sccoredb.Mdb_Type_ObjectID: return DbTypeCode.Object;
            case sccoredb.Mdb_Type_SByte: return DbTypeCode.SByte;
            case sccoredb.Mdb_Type_String: return DbTypeCode.String;
            case sccoredb.Mdb_Type_UInt64: return DbTypeCode.UInt64;
            case sccoredb.Mdb_Type_UInt32: return DbTypeCode.UInt32;
            case sccoredb.Mdb_Type_UInt16: return DbTypeCode.UInt16;
            case sccoredb.Mdb_Type_Binary: return DbTypeCode.Binary;
            case sccoredb.Mdb_Type_LargeBinary: return DbTypeCode.LargeBinary;
            case sccoredb.Mdb_Type_ObjectKey: return DbTypeCode.Key;
            default: throw new ArgumentException();
            };
        }
    }
}
