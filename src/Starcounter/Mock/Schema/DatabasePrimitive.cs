namespace Sc.Server.Weaver.Schema
{
/// <summary>
/// Enumeration of primitives supported by the database.
/// </summary>
public enum DatabasePrimitive
{
    None = 0, // Means that the type is NOT a primitive.
    Boolean,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Decimal,
    Single,
    Double,
    String,
    DateTime,
    TimeSpan,
    Binary,
    LargeBinary
}
}