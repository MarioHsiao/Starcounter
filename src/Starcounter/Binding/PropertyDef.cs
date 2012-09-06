
namespace Starcounter.Binding
{
    
    public sealed class PropertyDef
    {

        public string Name;
        public DbTypeCode Type;
        public bool IsNullable;

        public string TargetTypeName;

        /// <summary>
        /// Index of column if representing a database column. -1 otherwise.
        /// </summary>
        public int ColumnIndex;

        public PropertyDef(string name, DbTypeCode type, bool isNullable, int columnIndex) : this(name, type, isNullable, null, columnIndex) { }
        
        public PropertyDef(string name, DbTypeCode type, bool isNullable, string targetTypeName, int columnIndex)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
            TargetTypeName = targetTypeName;
            ColumnIndex = columnIndex;
        }
    }
}
