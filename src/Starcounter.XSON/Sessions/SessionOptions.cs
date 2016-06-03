using System;

namespace Starcounter {
    [Flags]
    public enum SessionOptions : int {
        Default = 0,
        IncludeSchema = 1,
        PatchVersioning = 2,
        StrictPatchRejection = 4,
//        DisableProtocolOT = 8,
        IncludeNamespaces = 16
    }
}
