using System;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class BindingTestDirect {
        private static uint ErrorTypeName(String name, uint errorCode) {
            uint receivedErrorCode = 0;
            try {
                Starcounter.Binding.Bindings.GetTypeBindingInsensitive(name);
            } catch (Starcounter.DbException dbExc) {
                receivedErrorCode = dbExc.ErrorCode;
            }
            Trace.Assert(receivedErrorCode == errorCode, "Exception SCERR" + errorCode + " is expected during get type binding");
            return receivedErrorCode;
        }

        public static void DirectBindingTest() {
            HelpMethods.LogEvent("Starting direct binding test.");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("QueryProcessingTest.CamelNameSpace.CommonClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("QueryProcessingTest.CamelNameSpace.AName.CommonClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("CommonClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("commonclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("CommonClass");
            ErrorTypeName("lowercasecommonclass", 4177);
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("lowercasenamespace.lowercasecommonclass");
            ErrorTypeName("lowercasecommonclass", 4177);
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("nonamespaceclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("QueryProcessingTest.CamelNameSpace.lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("lowercasenamespace.commonclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("CamelClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("QueryProcessingTest.CamelNameSpace.AName.lowercasecommonclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("QueryProcessingTest.CamelNameSpace.CamelClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("CamelClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("lowercasenamespace.lowercaseclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("LongNameCamelClass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("lowercaseclass");
            Starcounter.Binding.Bindings.GetTypeBindingInsensitive("QueryProcessingTest.CamelNameSpace.AName.LongNameCamelClass");
            HelpMethods.LogEvent("Finished direct binding test.");
        }
    }
}
