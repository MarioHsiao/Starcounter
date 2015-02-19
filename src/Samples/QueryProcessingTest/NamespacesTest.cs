using System;
using Starcounter;
using System.Diagnostics;

namespace QueryProcessingTest {
    public class NamespacesTest {
        public static void TestClassesNamespaces() {
            HelpMethods.LogEvent("Test queries with classes in different namespaces");
            Db.Transact(delegate {
                commonclass c = new commonclass { NoNamespaceProperty = 1 };
                CamelNameSpace.CommonClass Cc = new CamelNameSpace.CommonClass { CamelIntProperty = 2 };
                lowercasenamespace.commonclass cc = new lowercasenamespace.commonclass { lowercaseintproperty = 3 };
                CommonCamelClass cCc = new CommonCamelClass { DecimalProperty = 10m };
                int nrs = 0;
                foreach (commonclass nc in Db.SQL("select c from commonclass c")) {
                    nrs++;
                    Trace.Assert(c.Equals(nc));
                }
                Trace.Assert(nrs == 1);
                nrs = 0;
                foreach (CommonCamelClass nc in Db.SQL<CommonCamelClass>("select c from commoncamelclass c")) {
                    nrs++;
                    Trace.Assert(cCc.Equals(nc));
                }
                Trace.Assert(nrs == 1);
                nrs = 0;
                foreach (lowercasenamespace.commonclass nc in Db.SQL("select c from lowercasenamespace.commonclass c")) {
                    nrs++;
                    Trace.Assert(cc.Equals(nc));
                }
                Trace.Assert(nrs == 1);
                nrs = 0;
                foreach (commonclass nc in Db.SQL<commonclass>("select c from commonclass c")) {
                    nrs++;
                    Trace.Assert(c.Equals(nc));
                }
                Trace.Assert(nrs == 1);
                nrs = 0;
                foreach (lowercasenamespace.commonclass nc in
                    Db.SQL<lowercasenamespace.commonclass>("select c from lowercasenamespace.commonclass c")) {
                    nrs++;
                    Trace.Assert(cc.Equals(nc));
                }
                Trace.Assert(nrs == 1);
                c.Delete();
                Cc.Delete();
                cc.Delete();
                cCc.Delete();
            });
            Db.SQL("create index commonclassindex on commonclass (nonamespaceproperty)");
            Db.SQL("create index commonclassindex on lowercasenamespace.commonclass(lowercaseintproperty)");
            Db.SQL("drop index commonclassindex on commonclass");
            Db.SQL("drop index commonclassindex on lowercasenamespace.commonclass");
            HelpMethods.LogEvent("Finished testing queries with classes in different namespaces");
        }
    }
}
