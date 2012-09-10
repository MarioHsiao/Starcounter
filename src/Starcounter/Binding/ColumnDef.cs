
namespace Starcounter.Binding
{
    
    public sealed class ColumnDef
    {

        public string Name;
        public DbTypeCode Type;
        public bool IsNullable;
        public bool IsInherited;

        public ColumnDef(string name, DbTypeCode type, bool isNullable, bool isInherited)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
            IsInherited = isInherited;
        }
    }
}
