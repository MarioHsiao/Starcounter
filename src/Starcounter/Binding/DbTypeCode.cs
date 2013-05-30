// ***********************************************************************
// <copyright file="DbTypeCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Binding
{
    //
    // Since the internal type codes are not very jump table friendly we
    // convert between the binding type code and internal type code rather then
    // mapping them directly.
    //

    /// <summary>
    /// Enum DbTypeCode
    /// </summary>
    public enum DbTypeCode
    {
        /// <summary>
        /// The boolean
        /// </summary>
        Boolean,
        /// <summary>
        /// The byte
        /// </summary>
        Byte,
        /// <summary>
        /// The date time
        /// </summary>
        DateTime,
        /// <summary>
        /// The decimal
        /// </summary>
        Decimal,
        /// <summary>
        /// The single
        /// </summary>
        Single,
        /// <summary>
        /// The double
        /// </summary>
        Double,
        /// <summary>
        /// The int64
        /// </summary>
        Int64,
        /// <summary>
        /// The int32
        /// </summary>
        Int32,
        /// <summary>
        /// The int16
        /// </summary>
        Int16,
        /// <summary>
        /// The object
        /// </summary>
        Object,
        //Objects,
        /// <summary>
        /// The S byte
        /// </summary>
        SByte,
        /// <summary>
        /// The string
        /// </summary>
        String,
        /// <summary>
        /// The U int64
        /// </summary>
        UInt64,
        /// <summary>
        /// The U int32
        /// </summary>
        UInt32,
        /// <summary>
        /// The U int16
        /// </summary>
        UInt16,
        /// <summary>
        /// The binary
        /// </summary>
        Binary,
        /// <summary>
        /// The large binary
        /// </summary>
        LargeBinary,
        /// <summary>
        /// </summary>
        Key
    }
}
