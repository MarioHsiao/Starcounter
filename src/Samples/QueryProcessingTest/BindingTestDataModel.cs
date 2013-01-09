﻿using System;
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

public class nonameclass : Entity {
    public decimal DecimalProperty;
}
