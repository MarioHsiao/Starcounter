
using Sc.Server.Binding;
using System.Collections.Generic;

namespace Starcounter
{
    
    public static class BindingRegistry
    {

        // TODO: Access to dictionary needs to be thread-safe.
        private static Dictionary<string, TypeBinding> typeBindingsByName_ = new Dictionary<string, TypeBinding>();

        public static void BuildAndAddTypeBinding(TableDef tableDef)
        {
            TypeBinding typeBinding = new TypeBinding(tableDef);
            typeBindingsByName_.Add(tableDef.Name, typeBinding);
        }

        internal static TypeBinding GetTypeBinding(string name)
        {
            TypeBinding typeBinding;
            typeBindingsByName_.TryGetValue(name, out typeBinding);
            return typeBinding;
        }
    }
}
