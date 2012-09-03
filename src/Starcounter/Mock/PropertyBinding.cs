
using Starcounter;
using Starcounter.Binding;

namespace Sc.Server.Binding
{
    public class PropertyBinding : IPropertyBinding
    {

        public readonly PropertyDef PropertyDef;

        private readonly int index_;

        public PropertyBinding(PropertyDef propertyDef, int index)
        {
            PropertyDef = propertyDef;
            index_ = index;
        }

        public int GetDataIndex()
        {
            throw new System.NotImplementedException();
        }

        public int Index
        {
            get { return index_; }
        }

        public string Name
        {
            get { return PropertyDef.Name; }
        }

        public ITypeBinding TypeBinding
        {
            get { throw new System.NotImplementedException(); }
        }

        public DbTypeCode TypeCode
        {
            get { return PropertyDef.Type; }
        }
    }
}
