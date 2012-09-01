
using Starcounter;

namespace Sc.Server.Binding
{

    public class TypeBinding : TypeOrExtensionBinding
    {

        public TypeBinding Base;

        public TypeBinding(TableDef tableDef) : base(tableDef) { }

        public ExtensionBinding GetExtensionBinding(string name)
        {
            throw new System.NotImplementedException();
        }

        public System.Collections.Generic.IEnumerator<ExtensionBinding> GetAllExtensionBindings()
        {
            throw new System.NotImplementedException();
        }
    }

    public class ExtensionBinding : TypeOrExtensionBinding
    {

        public ExtensionBinding(TableDef tableDef) : base(tableDef) { }
    }
    
    public class TypeOrExtensionBinding : ITypeBinding
    {

        public readonly TableDef TableDef;

        public int Index;

        public ulong DefHandle;

        public TypeOrExtensionBinding(TableDef tableDef)
        {
            TableDef = tableDef;
        }

        public IndexInfo[] GetAllIndexInfos()
        {
            throw new System.NotImplementedException();
        }

        public DbObject NewInstanceUninit()
        {
            throw new System.NotImplementedException();
        }

        public bool SubTypeOf(TypeBinding tb)
        {
            throw new System.NotImplementedException();
        }

        public DbObject NewInstance(ulong addr, ulong oid)
        {
            throw new System.NotImplementedException();
        }

        public string Name
        {
            get { throw new System.NotImplementedException(); }
        }

        public int PropertyCount
        {
            get { throw new System.NotImplementedException(); }
        }

        public int GetPropertyIndex(string name)
        {
            throw new System.NotImplementedException();
        }

        public IPropertyBinding GetPropertyBinding(int index)
        {
            throw new System.NotImplementedException();
        }

        public IPropertyBinding GetPropertyBinding(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}
