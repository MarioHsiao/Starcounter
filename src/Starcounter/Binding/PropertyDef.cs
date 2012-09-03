
namespace Starcounter.Binding
{
    
    public sealed class PropertyDef
    {

        public string Name;
        public DbTypeCode Type;
        public bool IsNullable;

        public TypeDef TypeDef;

        public PropertyDef(string name, DbTypeCode type, bool isNullable)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
        }
    }
}
