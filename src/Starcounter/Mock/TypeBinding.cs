
using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Execution;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sc.Server.Binding
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
