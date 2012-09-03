
using Starcounter;
using Starcounter.Binding;
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
/// <summary>
/// A MaxValueObject is used to represent the max value of a reference type in an range.
/// </summary>
internal sealed class MaxValueObject : IObjectView
{
    internal MaxValueObject()
    { }

    public ITypeBinding TypeBinding
    {
        get
        {
            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
        }
    }

    public Nullable<Binary> GetBinary(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Boolean> GetBoolean(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Byte> GetByte(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<DateTime> GetDateTime(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Decimal> GetDecimal(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Double> GetDouble(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Int16> GetInt16(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Int32> GetInt32(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Int64> GetInt64(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public IObjectView GetObject(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<SByte> GetSByte(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<Single> GetSingle(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public String GetString(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<UInt16> GetUInt16(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<UInt32> GetUInt32(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public Nullable<UInt64> GetUInt64(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    public IObjectView GetExtension(Int32 index)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }

    Boolean IObjectView.EqualsOrIsDerivedFrom(IObjectView obj)
    {
        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Not supported.");
    }
}
}
