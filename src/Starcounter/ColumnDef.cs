
namespace Starcounter
{
    
    public sealed class ColumnDef
    {

#if true
        internal const byte SC_BASETYPE_SINT64 = 0x02;
        internal const byte SC_BASETYPE_STRING = 0x06;
        internal const byte Mdb_Type_Int64 = (0x40 | SC_BASETYPE_SINT64);
        internal const byte Mdb_Type_String = (0x10 | SC_BASETYPE_STRING);
#endif

        public const byte TYPE_INT64 = Mdb_Type_Int64;
        public const byte TYPE_STRING = Mdb_Type_String;

        public string Name;
        public byte Type;
        public bool IsNullable;

        public ColumnDef(string name, byte type, bool isNullable)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
        }
    }
}
