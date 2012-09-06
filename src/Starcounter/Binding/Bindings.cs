
using Sc.Server.Binding;
using System;
using System.Collections.Generic;

namespace Starcounter.Binding
{
    
    public static class Bindings
    {

        // TODO: Access to collections needs to be thread-safe.

        private static List<TypeBinding> typeBindingsById_ = new List<TypeBinding>();
        private static Dictionary<string, TypeBinding> typeBindingsByName_ = new Dictionary<string, TypeBinding>();

        private static List<TypeDef> typeDefsById_ = new List<TypeDef>();
        private static Dictionary<string, TypeDef> typeDefsByName_ = new Dictionary<string, TypeDef>();

        //
        // Note that a type definition must not be registered until the table
        // definition has been synchronized with the database.
        //
        public static void RegisterTypeDef(TypeDef typeDef)
        {
            typeDefsByName_.Add(typeDef.Name, typeDef);

            TableDef tableDef = typeDef.TableDef;
            while (typeDefsById_.Count <= tableDef.TableId) typeDefsById_.Add(null); // TODO:
            typeDefsById_.Insert(tableDef.TableId, typeDef);
        }

        internal static TypeDef GetTypeDef(string name)
        {
            TypeDef typeDef;
            typeDefsByName_.TryGetValue(name, out typeDef);
            return typeDef;
        }

        internal static IEnumerable<TypeDef> GetAllTypeDefs()
        {
            return typeDefsByName_.Values;
        }

        internal static TypeBinding GetTypeBinding(ushort tableId)
        {
            // TODO:
            // Can we make this so that not found always raised exception?
            TypeBinding tb;
            try
            {
                tb = typeBindingsById_[tableId];
            }
            catch (IndexOutOfRangeException)
            {
                tb = null;
            }

            if (tb != null) return tb;

            return BuildTypeBindingFromTypeDef(tableId);
        }

        internal static TypeBinding GetTypeBinding(string name)
        {
            try
            {
                return typeBindingsByName_[name];
            }
            catch (KeyNotFoundException)
            {
                return BuildTypeBindingFromTypeDef(name);
            }
        }

        private static TypeBinding BuildTypeBindingFromTypeDef(ushort tableId)
        {
            try
            {
                return BuildTypeBindingFromTypeDef(typeDefsById_[tableId]);
            }
            catch (IndexOutOfRangeException)
            {
                return null; // TODO: Type not loaded.
            }
        }

        private static TypeBinding BuildTypeBindingFromTypeDef(string name)
        {
            TypeDef typeDef;
            typeDefsByName_.TryGetValue(name, out typeDef);
            return BuildTypeBindingFromTypeDef(typeDef);
        }

        private static TypeBinding BuildTypeBindingFromTypeDef(TypeDef typeDef)
        {
            if (typeDef == null) return null; // TODO: Type not loaded. Detect by way of exception.

            BindingBuilder builder = new BindingBuilder(typeDef);
            TypeBinding tb = builder.CreateTypeBinding();
#if false
            builder.WriteAssemblyToDisk();
#endif

            AddTypeBinding(tb);
            return tb;
        }

        private static void AddTypeBinding(TypeBinding typeBinding)
        {
            typeBindingsByName_.Add(typeBinding.Name, typeBinding);

            TableDef tableDef = typeBinding.TypeDef.TableDef;
            while (typeBindingsById_.Count <= tableDef.TableId) typeBindingsById_.Add(null); // TODO:
            typeBindingsById_.Insert(tableDef.TableId, typeBinding);
        }
    }
}
