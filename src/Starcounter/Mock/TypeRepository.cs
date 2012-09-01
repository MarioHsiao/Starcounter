
namespace Sc.Server.Binding
{
    
    public static class TypeRepository
    {

        public static TypeBinding[] TypeBindingsByIndex;

        public static TypeBinding GetTypeBinding(string name)
        {
            throw new System.NotImplementedException();
        }

        public static System.Collections.Generic.IEnumerator<TypeBinding> GetAllTypeBindings()
        {
            throw new System.NotImplementedException();
        }

        public static int TryGetTypeBindingByShortName(string name, out TypeBinding typeBind)
        {
            throw new System.NotImplementedException();
        }
    }
}
