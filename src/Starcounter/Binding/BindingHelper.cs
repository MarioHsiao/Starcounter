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
        internal static byte ConvertDbTypeCodeToScTypeCode(DbTypeCode t) {
            switch (t) {
            case DbTypeCode.Boolean: return sccoredb.STAR_TYPE_ULONG;
            case DbTypeCode.Byte: return sccoredb.STAR_TYPE_ULONG;
            case DbTypeCode.DateTime: return sccoredb.STAR_TYPE_ULONG;
            case DbTypeCode.Decimal: return sccoredb.STAR_TYPE_DECIMAL;
            case DbTypeCode.Single: return sccoredb.STAR_TYPE_FLOAT;
            case DbTypeCode.Double: return sccoredb.STAR_TYPE_DOUBLE;
            case DbTypeCode.Int64: return sccoredb.STAR_TYPE_LONG;
            case DbTypeCode.Int32: return sccoredb.STAR_TYPE_LONG;
            case DbTypeCode.Int16: return sccoredb.STAR_TYPE_LONG;
            case DbTypeCode.Object: return sccoredb.STAR_TYPE_REFERENCE;
            case DbTypeCode.SByte: return sccoredb.STAR_TYPE_LONG;
            case DbTypeCode.String: return sccoredb.STAR_TYPE_STRING;
            case DbTypeCode.UInt64: return sccoredb.STAR_TYPE_ULONG;
            case DbTypeCode.UInt32: return sccoredb.STAR_TYPE_ULONG;
            case DbTypeCode.UInt16: return sccoredb.STAR_TYPE_ULONG;
            case DbTypeCode.Binary: return sccoredb.STAR_TYPE_BINARY;
            case DbTypeCode.Key: return sccoredb.STAR_TYPE_KEY;
            default: throw ErrorCode.ToException(Error.SCERRUNEXPECTEDINTERNALERROR,
                       "Unknown DbTypeCode is defined in CLR code host: " +
                       Enum.GetName(typeof(DbTypeCode), t));
            };
        }

        /// <summary>
        /// </summary>
        internal static DbTypeCode ConvertScTypeCodeToDbTypeCode(byte t) {
            switch (t) {
            case sccoredb.STAR_TYPE_STRING: return DbTypeCode.String;
            case sccoredb.STAR_TYPE_BINARY: return DbTypeCode.Binary;
            case sccoredb.STAR_TYPE_LONG: return DbTypeCode.Int64;
            case sccoredb.STAR_TYPE_ULONG: return DbTypeCode.UInt64;
            case sccoredb.STAR_TYPE_DECIMAL: return DbTypeCode.Decimal;
            case sccoredb.STAR_TYPE_FLOAT: return DbTypeCode.Single;
            case sccoredb.STAR_TYPE_DOUBLE: return DbTypeCode.Double;
            case sccoredb.STAR_TYPE_REFERENCE: return DbTypeCode.Object;
            case sccoredb.STAR_TYPE_KEY: return DbTypeCode.Key;
            default: throw new ArgumentException();
            };
        }
    }
}
