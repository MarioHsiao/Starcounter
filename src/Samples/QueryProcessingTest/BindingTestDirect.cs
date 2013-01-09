using System;

namespace QueryProcessingTest {
    public static class BindingTestDirect {
        public static void DirectBindingTest() {
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.CommonClass");
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.LongNameSpace.CommonClass");
            Starcounter.Binding.Bindings.GetTypeBinding("CommonClass");
            Starcounter.Binding.Bindings.GetTypeBinding("commonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("CommonClass");
            Starcounter.Binding.Bindings.GetTypeBinding("lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("lowercasenamespace.lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("nonamespaceclass");
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("lowercasenamespace.commonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("CamelClass");
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.LongNameSpace.lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.CamelClass");
            Starcounter.Binding.Bindings.GetTypeBinding("CamelClass");
            Starcounter.Binding.Bindings.GetTypeBinding("lowercasenamespace.lowercaseclass");
            Starcounter.Binding.Bindings.GetTypeBinding("LongNameCamelClass");
            Starcounter.Binding.Bindings.GetTypeBinding("lowercaseclass");
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.LongNameSpace.LongNameCamelClass");
        }
    }
}
