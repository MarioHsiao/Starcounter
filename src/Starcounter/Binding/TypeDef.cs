
namespace Starcounter.Binding
{
    
    public class TypeDef
    {

        public string Name;

        public string BaseName;

        public PropertyDef[] PropertyDefs;

        public TypeLoader TypeLoader;

        public TableDef TableDef;

        public TypeDef(string name, PropertyDef[] propertyDefs, TypeLoader typeLoader, TableDef tableDef)
        {
            Name = name;
            PropertyDefs = propertyDefs;
            TypeLoader = typeLoader;
            TableDef = tableDef;
        }
    }
}
