using System;
using Starcounter;

namespace QueryProcessingTest.CamelNameSpace {
    public class CommonClass : Entity {
        public int CamelIntProperty;
    }

    public class lowercasecommonclass : Entity {
        public string StringProperty;
    }

    public class CamelClass : Entity {
        public decimal DecimalProperty;
    }
}

namespace lowercasenamespace {
    public class commonclass : Entity {
        public int lowercaseintproperty;
    }

    public class lowercasecommonclass : Entity {
        public string StringProperty;
    }

    public class lowercaseclass : Entity {
        public decimal DecimalProperty;
    }
}

// No namespace
public class commonclass : Entity {
    public int NoNamespaceProperty;
}

public class nonamespaceclass : Entity {
    public decimal DecimalProperty;
}

// Add another namespace with common class inside

namespace QueryProcessingTest.CamelNameSpace.AName {
    public class CommonClass : Entity {
        public int CamelIntProperty;
    }

    public class lowercasecommonclass : Entity {
        public string StringProperty;
    }

    public class LongNameCamelClass : Entity {
        public decimal DecimalProperty;
    }
}
