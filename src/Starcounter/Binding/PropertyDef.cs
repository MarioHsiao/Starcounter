
namespace Starcounter.Binding
{
    
    public sealed class PropertyDef
    {

        public string Name;
        public DbTypeCode Type;
        public bool IsNullable;

        /// <summary>
        /// Index of column if representing a database column. -1 otherwise.
        /// </summary>
        public int ColumnIndex;

        public string TargetTypeName;

        public PropertyDef(string name, DbTypeCode type, bool isNullable, int columnIndex)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
            ColumnIndex = columnIndex;
        }
    }
}
