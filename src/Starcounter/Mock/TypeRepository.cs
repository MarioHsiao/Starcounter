
using Starcounter;

namespace Sc.Server.Binding
{
    
    internal static class TypeRepository
    {

        internal static TypeBinding GetTypeBinding(ushort tableId)
        {
            return Bindings.GetTypeBinding(tableId);
        }

        internal static TypeBinding GetTypeBinding(string name)
        {
            return Bindings.GetTypeBinding(name);
        }

        internal static System.Collections.Generic.IEnumerator<TypeBinding> GetAllTypeBindings()
        {
            return Bindings.GetAllTypeBindings();
        }

        internal static int TryGetTypeBindingByShortName(string name, out TypeBinding typeBind)
        {
            throw new System.NotImplementedException();
        }
    }
}
