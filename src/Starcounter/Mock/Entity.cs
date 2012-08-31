
using Sc.Server.Internal;

namespace Starcounter
{
    
    public abstract class Entity : DbObject
    {

        public Entity(Sc.Server.Internal.Uninitialized u) { }

        public Entity(ulong typeAddr, Sc.Server.Binding.TypeBinding typeBinding, Sc.Server.Internal.Uninitialized u)
        {
            DbState.Insert(this, typeAddr, typeBinding);
        }
    }
}
