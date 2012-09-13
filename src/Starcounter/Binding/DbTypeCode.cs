
namespace Starcounter.Binding
{
    //
    // Since the internal type codes are not very jump table friendly we
    // convert between the binding type code and internal type code rather then
    // mapping them directly.
    //

    public enum DbTypeCode
    {
        Boolean,
        Byte,
        DateTime,
        Decimal,
        Single,
        Double,
        Int64,
        Int32,
        Int16,
        Object,
        //Objects,
        SByte,
        String,
        UInt64,
        UInt32,
        UInt16,
        Binary,
        LargeBinary
    }
}
