
using System;
using Starcounter;

[Database]
public class SharedClassWithoutNamespace {
    public string SharedClassWithoutNamespaceField;
}

namespace SharedDll {

    [Database]
    public class SharedDllClass {
        public string SharedDllClassField;
    }
}