
namespace Starcounter.Binding
{
    
    public sealed class ColumnDef
    {

        public string Name;
        public DbTypeCode Type;
        public bool IsNullable;

        public ColumnDef(string name, DbTypeCode type, bool isNullable)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
        }
    }
}
