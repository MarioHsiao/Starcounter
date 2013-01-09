using System;

namespace QueryProcessingTest {
    public static class BindingTestDirect {
        public static void DirectBindingTest() {
            Starcounter.Binding.Bindings.GetTypeBinding("QueryProcessingTest.CamelNameSpace.CommonClass");
        }
    }
}
