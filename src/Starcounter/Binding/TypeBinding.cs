﻿
using Starcounter;
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.Reflection;

// TODO:
// We must keep TypeBinding in namespace Sc.Server.Binding for now because
// generated code links to this code. To be moved to namespace
// Starcounter.Binding as soon as code generation has been updated.

namespace Sc.Server.Binding //namespace Starcounter.Binding
{

    public abstract class TypeBinding : ITypeBinding
    {

        public TypeBinding Base; // TODO:

        internal TypeDef TypeDef;

        private PropertyBinding[] propertyBindings_;
        private Dictionary<string, PropertyBinding> propertyBindingsByName_;

        private string name_;

        public string Name { get { return name_; } internal set { name_ = value; } }

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

        internal Sc.Server.Binding.IndexInfo[] GetAllIndexInfos()
        {
            return TypeDef.TableDef.GetAllIndexInfos();
        }

        internal void SetPropertyBindings(PropertyBinding[] propertyBindings)
        {
            propertyBindings_ = propertyBindings;

            propertyBindingsByName_ = new Dictionary<string, PropertyBinding>(propertyBindings.Length);
            for (int i = 0; i < propertyBindings.Length; i++)
            {
                PropertyBinding pb = propertyBindings[i];
                propertyBindingsByName_.Add(pb.Name, pb);
            }
        }

        IPropertyBinding ITypeBinding.GetPropertyBinding(string name)
        {
            return GetPropertyBinding(name);
        }
    }
}
