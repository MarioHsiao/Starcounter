
using Sc.Server.Binding;

namespace Starcounter
{
    
    public abstract class DbObject
    {

        internal ObjectRef ThisRef;

        internal void Attach(ulong addr, ulong oid, TypeBinding typeBinding)
        {
            ThisRef.ETI = addr;
            ThisRef.ObjectID = oid;
        }
    }
}
