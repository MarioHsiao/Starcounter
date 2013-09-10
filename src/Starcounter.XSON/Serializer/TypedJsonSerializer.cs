using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {
    public abstract class TypedJsonSerializer {
        public abstract int Serialize(Json json, out byte[] buffer);
        public abstract int Populate(Json json, IntPtr source, int sourceSize);
    }
}
