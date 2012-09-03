
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

    public class TypeBinding : ITypeBinding
    {

        public readonly TypeDef TypeDef;

        public TypeBinding Base;

        private readonly Type type_;

        public TypeBinding(TypeDef typeDef, Type type)
        {
            TypeDef = typeDef;
            type_ = type;
        }

        public DbObject NewInstanceUninit()
        {
            ConstructorInfo ctor = type_.GetConstructor(new Type[] { typeof(Sc.Server.Internal.Uninitialized) });
            DbObject obj = (DbObject)ctor.Invoke(new object[] { null });
            return obj;
        }

        public DbObject NewInstance(ulong addr, ulong oid)
        {
            DbObject obj = NewInstanceUninit();
            obj.Attach(addr, oid, this);
            return obj;
        }

        public bool SubTypeOf(TypeBinding tb)
        {
            throw new System.NotImplementedException();
        }

        public ulong DefHandle { get { return TypeDef.TableDef.DefinitionAddr; } }

        public string Name
        {
            get { return TypeDef.Name; }
        }

        public PropertyBinding GetPropertyBinding(string name)
        {
            for (int i = 0; i < TypeDef.PropertyDefs.Length; i++)
            {
                if (TypeDef.PropertyDefs[i].Name == name)
                {
                    return new PropertyBinding(TypeDef.PropertyDefs[i], i);
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
