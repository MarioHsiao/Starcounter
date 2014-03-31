using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {
    public abstract class TypedJsonSerializer {
        public abstract int EstimateSizeBytes(Json obj);
        public abstract int Serialize(Json json, byte[] buffer, int offset);
        public abstract int Populate(Json json, IntPtr source, int sourceSize);
    }
}
