
using System;

namespace Starcounter.Internal.Weaver {
    
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    internal class AssemblyWeavedForIPCAttribute : Attribute {
        public AssemblyWeavedForIPCAttribute()
            : base() {
        }
    }
}
