using Starcounter;
using Starcounter.Binding;

namespace SchemaUpgradeTest {
    class Program {
        static int Main() {
            // Assert that we still have two A:s and one B with correct values and correct ( = expected) layout.
            ScAssertion.Assert(Db.SQL<long>("SELECT Count(a) FROM A a").First == 2);

            ushort expectedLayoutHandle = GetExpectedLayoutHandle<A>();
            var a1 = Db.SQL<A>("SELECT a FROM A a WHERE a.IntInA=?", 1).First;
            ScAssertion.Assert(GetLayoutHandle(a1) == expectedLayoutHandle);
            ScAssertion.Assert(a1.BoolInD == true);
            ScAssertion.Assert(a1.ByteInC == 64);
            ScAssertion.Assert(a1.LongInA == 1111111);
            ScAssertion.Assert(a1.LongInC == 10001); // Readded column

            var a2 = Db.SQL<A>("SELECT a FROM A a WHERE a.IntInA=?", 2).First;
            ScAssertion.Assert(GetLayoutHandle(a2) == expectedLayoutHandle);
            ScAssertion.Assert(a2.BoolInD == true);
            ScAssertion.Assert(a2.ByteInC == 128);
            ScAssertion.Assert(a2.LongInA == 2222222);
            ScAssertion.Assert(a2.LongInC == 10002); // Readded column

            expectedLayoutHandle = GetExpectedLayoutHandle<B>();
            ScAssertion.Assert(Db.SQL<long>("SELECT Count(b) FROM B b").First == 1);

            B b = null;
            foreach (var tmp in Db.SQL<B>("SELECT b FROM B b")) {
                b = DbHelper.FromID(tmp.GetObjectNo()) as B;
            }
            ScAssertion.Assert(GetLayoutHandle(b) == expectedLayoutHandle);
            ScAssertion.Assert(b.LongInC == 20001); // Readded column

            // One instance should still exist in StandAlone
            expectedLayoutHandle = GetExpectedLayoutHandle<StandAlone>();
            var sa = Db.SQL<StandAlone>("SELECT s FROM StandAlone s").First;
            ScAssertion.Assert(sa != null);
            ScAssertion.Assert(GetLayoutHandle(sa) == expectedLayoutHandle);
            ScAssertion.Assert(sa.Value == "Value");

            return 0;
        }

        private static ushort GetExpectedLayoutHandle<T>() {
            return Db.SQL<ushort>("SELECT r.LayoutHandle FROM Starcounter.Metadata.RawView r WHERE r.FullName=?", typeof(T).FullName).First;
        }

        private static ushort GetLayoutHandle(object obj) {
            var proxy = obj as IObjectProxy;
            if (proxy == null)
                return 0;

            return (ushort)(proxy.ThisHandle & 0xFFFF);
        }
    }

    // Things modified in this test: 
    // C.LongInC readded.
    // Change of name reverted, StandAlone renamed to RenamedClass.

    [Database]
    public class A : C {
        public int IntInA;
        public long LongInA;
    }

    [Database]
    public class B : C {
        public double DoubleInB;
    }

    [Database]
    public class C : D {
        public long LongInC;
        public byte ByteInC;
    }

    [Database]
    public class D {
        public string StringInD;
        public bool BoolInD;
    }

    [Database]
    public class StandAlone {
        public string Value;
    }
}