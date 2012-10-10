
using Starcounter.Internal;

namespace Starcounter.Metadata
{
    
    public sealed class SysIndex : Entity
    {

        public SysIndex(Uninitialized u) : base(u) { }

        public string Name
        {
            get { return DbState.ReadString(this, 0); }
        }

        public string TableName
        {
            get { return DbState.ReadString(this, 1); }
        }
    }
}
