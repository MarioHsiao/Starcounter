
namespace Starcounter.Binding
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

        internal static int TryGetTypeBindingByShortName(string name, out TypeBinding typeBind)
        {
            throw new System.NotImplementedException();
        }
    }
}
