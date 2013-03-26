using System;
using Starcounter;

namespace QueryProcessingTest.CamelNameSpace {
    [Database]
    public class CommonClass {
        public int CamelIntProperty;
    }

    [Database]
    public class lowercasecommonclass {
        public string StringProperty;
    }

    [Database]
    public class CamelClass {
        public decimal DecimalProperty;
    }
}

namespace lowercasenamespace {
    [Database]
    public class commonclass {
        public int lowercaseintproperty;
    }

    [Database]
    public class lowercasecommonclass {
        public string StringProperty;
    }

    [Database]
    public class lowercaseclass {
        public decimal DecimalProperty;
    }
}

// No namespace
[Database]
public class commonclass {
    public int NoNamespaceProperty;
}

[Database]
public class nonamespaceclass {
    public decimal DecimalProperty;
}

// Add another namespace with common class inside

namespace QueryProcessingTest.CamelNameSpace.AName {
    [Database]
    public class CommonClass {
        public int CamelIntProperty;
    }

    [Database]
    public class lowercasecommonclass {
        public string StringProperty;
    }

    [Database]
    public class LongNameCamelClass {
        public decimal DecimalProperty;
    }
}
