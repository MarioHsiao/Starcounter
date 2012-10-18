
using System;
using System.Collections.Generic;

namespace Starcounter.Binding
{
    
    public static class Bindings
    {

        private static TypeBinding[] typeBindingsById_ = new TypeBinding[0];
        private static Dictionary<string, TypeBinding> typeBindingsByName_ = new Dictionary<string, TypeBinding>();

        private static TypeDef[] typeDefsById_ = new TypeDef[0];
        private static Dictionary<string, TypeDef> typeDefsByName_ = new Dictionary<string, TypeDef>();

        private static object syncRoot_ = new object();

        //
        // Note that a type definition must not be registered until the table
        // definition has been synchronized with the database.
        //
        // Only one thread at a time must be allowed to add type definitions.
        //
        internal static void RegisterTypeDefs(TypeDef[] typeDefs)
        {
            // We don't have to lock here since only one thread at a time will
            // be adding type definitions.

            Dictionary<string, TypeDef> typeDefsByName = new Dictionary<string, TypeDef>(typeDefsByName_);
            TypeDef typeDef;
            for (int i = 0; i < typeDefs.Length; i++)
            {
                typeDef = typeDefs[i];
                typeDefsByName.Add(typeDef.Name, typeDef);
                typeDefsByName.Add(typeDef.Name.ToUpper(), typeDef);
            }

            List<TypeDef> typeDefsById = new List<TypeDef>(typeDefsById_);
            for (int i = 0; i < typeDefs.Length; i++)
            {
                typeDef = typeDefs[i];
                var tableId = typeDef.TableDef.TableId;
                while (typeDefsById.Count <= tableId) typeDefsById.Add(null);
                typeDefsById[tableId] = typeDef;
            }

            // No one will be requesting a type not previously registered so we
            // do not have to worry that the different maps are not in sync.

            typeDefsById_ = typeDefsById.ToArray();
            typeDefsByName_ = typeDefsByName;
        }

        public static TypeDef GetTypeDef(int tableId)
        {
            return typeDefsById_[tableId];
        }

        public static TypeDef GetTypeDef(string name)
        {
            TypeDef typeDef;
            typeDefsByName_.TryGetValue(name, out typeDef);
            return typeDef;
        }

        internal static IEnumerable<TypeDef> GetAllTypeDefs()
        {
            return typeDefsByName_.Values;
        }

        internal static TypeBinding GetTypeBinding(int tableId)
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
            catch (KeyNotFoundException) { }

            return BuildTypeBindingFromTypeDef(name);
        }

        private static TypeBinding BuildTypeBindingFromTypeDef(int tableId)
        {
            try
            {
                return BuildTypeBindingFromTypeDef(typeDefsById_[tableId]);
            }
            catch (IndexOutOfRangeException)
            {
                throw CreateExceptionOnTypeDefNotFound();
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
            if (typeDef == null) throw CreateExceptionOnTypeDefNotFound();

            lock (syncRoot_)
            {
                // Check if some other thread has added a type binding for the
                // specific type while we where waiting to acquire the lock.

                TypeBinding tb = null;
                var tableId = typeDef.TableDef.TableId;
                if (typeBindingsById_.Length > tableId)
                {
                    tb = typeBindingsById_[tableId];
                }

                if (tb == null)
                {
                    BindingBuilder builder = new BindingBuilder(typeDef);
                    tb = builder.CreateTypeBinding();
#if false
                    builder.WriteAssemblyToDisk();
#endif
                    AddTypeBinding(tb);
                }

                return tb;
            }
        }

        private static void AddTypeBinding(TypeBinding typeBinding)
        {
            Dictionary<string, TypeBinding> typeBindingsByName = new Dictionary<string, TypeBinding>(typeBindingsByName_);
            typeBindingsByName.Add(typeBinding.Name, typeBinding);
            typeBindingsByName.Add(typeBinding.UpperName, typeBinding);

            List<TypeBinding> typeBindingsById = new List<TypeBinding>(typeBindingsById_);
            var tableId = typeBinding.TypeDef.TableDef.TableId;
            while (typeBindingsById.Count <= tableId) typeBindingsById.Add(null);
            typeBindingsById[tableId] = typeBinding;

            typeBindingsById_ = typeBindingsById.ToArray();
            typeBindingsByName_ = typeBindingsByName;
        }

        private static Exception CreateExceptionOnTypeDefNotFound()
        {
            // This should not happen. No one should be requesting type binding
            // for a type that hasn't be registered. Schema must not have been
            // provided properly.

            throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH);
        }
    }
}
