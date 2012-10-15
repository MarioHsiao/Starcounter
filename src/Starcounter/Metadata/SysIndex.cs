
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

        public string Description
        {
            get { return DbState.ReadString(this, 2); }
        }

        public bool Unique
        {
            get { return DbState.ReadBoolean(this, 3); }
        }
    }
}
