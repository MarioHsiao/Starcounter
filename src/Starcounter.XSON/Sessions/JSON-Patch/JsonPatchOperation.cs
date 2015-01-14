using System;

namespace Starcounter {
    public enum JsonPatchOperation : int {
        Undefined = 0,
        Remove = 1,
        Replace = 2,
        Add = 3,
        Test = 4
    }
}
