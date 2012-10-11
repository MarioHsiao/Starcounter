
using Starcounter.Internal;

namespace Starcounter.Metadata
{
    
    public sealed class SysTable : Entity
    {

        public SysTable(Uninitialized u) : base(u) { }

        public string Name
        {
            get { return DbState.ReadString(this, 0); }
        }
    }
}
