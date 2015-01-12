// ***********************************************************************
// <copyright file="DatabasePrimitive.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Sc.Server.Weaver.Schema
{
    /// <summary>
    /// Enumeration of primitives supported by the database.
    /// </summary>
public enum DatabasePrimitive
{
    /// <summary>
    /// The none
    /// </summary>
    None = 0, // Means that the type is NOT a primitive.
    /// <summary>
    /// The boolean
    /// </summary>
    Boolean,
    /// <summary>
    /// The byte
    /// </summary>
    Byte,
    /// <summary>
    /// The S byte
    /// </summary>
    SByte,
    /// <summary>
    /// The int16
    /// </summary>
    Int16,
    /// <summary>
    /// The U int16
    /// </summary>
    UInt16,
    /// <summary>
    /// The int32
    /// </summary>
    Int32,
    /// <summary>
    /// The U int32
    /// </summary>
    UInt32,
    /// <summary>
    /// The int64
    /// </summary>
    Int64,
    /// <summary>
    /// The U int64
    /// </summary>
    UInt64,
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
    /// The string
    /// </summary>
    String,
    /// <summary>
    /// The date time
    /// </summary>
    DateTime,
    /// <summary>
    /// The time span
    /// </summary>
    TimeSpan,
    /// <summary>
    /// The binary
    /// </summary>
    Binary,
}
}