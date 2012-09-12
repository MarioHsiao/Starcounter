
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

        public ColumnDef Clone()
        {
            return new ColumnDef(Name, Type, IsNullable, IsInherited);
        }

        public bool Equals(ColumnDef columnDef)
        {
            return
                Name.Equals(columnDef.Name) &&
                Type == columnDef.Type &&
                IsNullable == columnDef.IsNullable &&
                IsInherited == columnDef.IsInherited
                ;
        }
    }
}
