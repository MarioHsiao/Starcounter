
using Starcounter;
using Starcounter.Binding;
using System;
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

        public string Name { get { return TypeDef.Name; } }

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

        internal PropertyBinding GetPropertyBinding(string name)
        {
            for (int i = 0; i < TypeDef.PropertyDefs.Length; i++)
            {
                if (TypeDef.PropertyDefs[i].Name == name)
                {
                    // TODO:
                    PropertyBinding pb = new TestPropertyBinding();
                    pb.SetName(TypeDef.PropertyDefs[i].Name);
                    pb.SetIndex(i);
                    pb.SetDataIndex(i);
                    return pb;
                }
            }
            return null;
        }

        internal Sc.Server.Binding.IndexInfo[] GetAllIndexInfos()
        {
            return TypeDef.TableDef.GetAllIndexInfos();
        }

        IPropertyBinding ITypeBinding.GetPropertyBinding(string name)
        {
            return GetPropertyBinding(name);
        }
    }
}
