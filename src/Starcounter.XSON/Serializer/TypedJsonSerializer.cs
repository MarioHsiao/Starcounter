using System;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {
    public abstract class TypedJsonSerializer {
        public abstract int EstimateSizeBytes(Json obj);
        public abstract int EstimateSizeBytes(Json obj, Template property);
        public abstract int Serialize(Json json, IntPtr dest, int destSize);
        public abstract int Serialize(Json json, Template property, IntPtr dest, int destSize);
        public abstract int Populate(Json json, IntPtr src, int srcSize);
    }
}
