
using Sc.Server.Binding;
using System;
using System.Collections.Generic;

namespace Starcounter
{
    
    public static class Bindings
    {

        // TODO: Access to collections needs to be thread-safe.
        private static List<TypeBinding> typeBindingsById_ = new List<TypeBinding>();
        private static Dictionary<string, TypeBinding> typeBindingsByName_ = new Dictionary<string, TypeBinding>();

        public static void BuildAndAddTypeBinding(TableDef tableDef)
        {
            TypeBinding typeBinding = new TypeBinding(tableDef);

            while (typeBindingsById_.Count <= tableDef.TableId) typeBindingsById_.Add(null); // TODO:

            typeBindingsById_.Insert(tableDef.TableId, typeBinding);
            typeBindingsByName_.Add(tableDef.Name, typeBinding);

        }

        internal static TypeBinding GetTypeBinding(ushort tableId)
        {
            return typeBindingsById_[tableId];
        }

        internal static TypeBinding GetTypeBinding(string name)
        {
            TypeBinding typeBinding;
            typeBindingsByName_.TryGetValue(name, out typeBinding);
            return typeBinding;
        }

        internal static IEnumerator<TypeBinding> GetAllTypeBindings()
        {
            return typeBindingsByName_.Values.GetEnumerator(); // TODO:
        }
    }
}
