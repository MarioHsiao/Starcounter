
namespace Starcounter.Binding
{
    
    public sealed class PropertyDef
    {

        public string Name;
        public DbTypeCode Type;
        public bool IsNullable;

        public string TargetTypeName;

        /// <summary>
        /// Name of column if representing a database column. NULL otherwise.
        /// </summary>
        public string ColumnName;

        /// <summary>
        /// Index of column if representing a database column. -1 otherwise.
        /// </summary>
        public int ColumnIndex;

        public PropertyDef(string name, DbTypeCode type, bool isNullable) : this(name, type, isNullable, null) { }
        
        public PropertyDef(string name, DbTypeCode type, bool isNullable, string targetTypeName)
        {
            Name = name;
            Type = type;
            IsNullable = isNullable;
            TargetTypeName = targetTypeName;
            //ColumnName = null;
            ColumnIndex = -1;
        }
    }
}
