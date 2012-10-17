
using Starcounter;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Starcounter.Binding
{

    [Flags]
    internal enum TypeBindingFlags
    {

        Callback_OnDelete
    }
    
    public abstract class TypeBinding : ITypeBinding
    {

        internal TypeBindingFlags Flags;

        internal TypeDef TypeDef;

        private PropertyBinding[] propertyBindings_;
        private Dictionary<string, PropertyBinding> propertyBindingsByName_;

        private string name_;

        public string Name
        {
            get { return name_; }
            internal set
            {
                name_ = value;
                uppername_ = value.ToUpper();
            }
        }

        private ushort tableId_;

        public ushort TableId { get { return tableId_; } internal set { tableId_ = value; } }

        //private string shortname_;
        private string uppername_;
        public string UpperName { get { return uppername_; } internal set { uppername_ = value; } }

        protected abstract Entity NewUninitializedInst();

        internal Entity NewInstanceUninit()
        {
            return NewUninitializedInst();
        }

        internal Entity NewInstance(ulong addr, ulong oid)
        {
            Entity obj = NewUninitializedInst();
            obj.Attach(addr, oid, this);
            return obj;
        }

        internal bool SubTypeOf(TypeBinding tb)
        {
            throw new System.NotImplementedException();
        }

        internal ulong DefHandle { get { return TypeDef.TableDef.DefinitionAddr; } }

        internal PropertyBinding GetPropertyBinding(int index)
        {
            return propertyBindings_[index];
        }

        internal PropertyBinding GetPropertyBinding(string name)
        {
            PropertyBinding pb;
            propertyBindingsByName_.TryGetValue(name, out pb);
            return pb;
        }

        internal IndexInfo[] GetAllIndexInfos()
        {
            return TypeDef.TableDef.GetAllIndexInfos();
        }

        internal IndexInfo GetIndexInfo(string name)
        {
            return TypeDef.TableDef.GetIndexInfo(name);
        }

        internal void SetPropertyBindings(PropertyBinding[] propertyBindings)
        {
            propertyBindings_ = propertyBindings;

            propertyBindingsByName_ = new Dictionary<string, PropertyBinding>(propertyBindings.Length);
            for (int i = 0; i < propertyBindings.Length; i++)
            {
                PropertyBinding pb = propertyBindings[i];
                propertyBindingsByName_.Add(pb.Name, pb);
                propertyBindingsByName_.Add(pb.UpperName, pb);
            }
        }

        IPropertyBinding ITypeBinding.GetPropertyBinding(string name)
        {
            return GetPropertyBinding(name);
        }

        IPropertyBinding ITypeBinding.GetPropertyBinding(int index)
        {
            if (index < propertyBindings_.Length)
                return GetPropertyBinding(index);
            else
                return null;
        }

        int ITypeBinding.PropertyCount { get { return propertyBindings_.Length; } }
    }
}
