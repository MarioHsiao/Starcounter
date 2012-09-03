
using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sc.Server.Binding
{

    public class TypeBinding : TypeOrExtensionBinding
    {

        public TypeBinding Base;

        private readonly Type type_;

        public TypeBinding(TypeDef typeDef, Type type) : base(typeDef)
        {
            type_ = type;
        }

        public ExtensionBinding GetExtensionBinding(string name)
        {
            throw new System.NotImplementedException();
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
    }

    public class ExtensionBinding : TypeOrExtensionBinding
    {

        public int Index;

        public ExtensionBinding(TypeDef typeDef) : base(typeDef) { }
    }
    
    public class TypeOrExtensionBinding : ITypeBinding
    {

        public readonly TypeDef TypeDef;

        public TypeOrExtensionBinding(TypeDef typeDef)
        {
            TypeDef = typeDef;
        }

        public ulong DefHandle { get { return TypeDef.TableDef.DefinitionAddr; } }

        public string Name
        {
            get { return TypeDef.Name; }
        }

        public int PropertyCount
        {
            get { return TypeDef.PropertyDefs.Length; }
        }

        public int GetPropertyIndex(string name)
        {
            throw new System.NotImplementedException();
        }

        public IPropertyBinding GetPropertyBinding(int index)
        {
            PropertyDef propertyDef = TypeDef.PropertyDefs[index];
            return new PropertyBinding(propertyDef, index);
        }

        public IPropertyBinding GetPropertyBinding(string name)
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

        internal IndexInfo[] GetAllIndexInfos()
        {
            return TypeDef.TableDef.GetAllIndexInfos();
        }
    }
}
