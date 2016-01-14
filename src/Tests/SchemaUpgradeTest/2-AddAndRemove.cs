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
            ScAssertion.Assert(a1.StringInD == "Ett");
            ScAssertion.Assert(a1.BoolInD == false); // new column, not set.
            ScAssertion.Assert(a1.ByteInC == 0); // new column, not set.
            ScAssertion.Assert(a1.LongInA == 0); // new column, not set.


            var a2 = Db.SQL<A>("SELECT a FROM A a WHERE a.IntInA=?", 2).First;
            ScAssertion.Assert(GetLayoutHandle(a2) == expectedLayoutHandle);
            ScAssertion.Assert(a2.StringInD == "Tva");
            ScAssertion.Assert(a1.BoolInD == false); // new column, not set.
            ScAssertion.Assert(a1.ByteInC == 0); // new column, not set.
            ScAssertion.Assert(a2.LongInA == 0); // new column, not set.

            expectedLayoutHandle = GetExpectedLayoutHandle<B>();
            ScAssertion.Assert(Db.SQL<long>("SELECT Count(b) FROM B b").First == 1);
            var b = Db.SQL<B>("SELECT b FROM B b").First;
            ScAssertion.Assert(GetLayoutHandle(b) == expectedLayoutHandle);
            ScAssertion.Assert(b.StringInD == "Tre");
            ScAssertion.Assert(b.BoolInD == false); // new column, not set.
            ScAssertion.Assert(b.ByteInC == 0); // new column, not set.
            ScAssertion.Assert(b.DoubleInB == 39.45d);

            // No instances should exist (renamed from StandAlone)
            ScAssertion.Assert(Db.SQL("SELECT r FROM RenamedClass r").First == null);

            Db.Transact(() => {
                a1.LongInA = 1111111;
                a1.ByteInC = 64;
                a1.BoolInD = true;

                a2.LongInA = 2222222;
                a2.ByteInC = 128;
                a2.BoolInD = true;
            });

            return 0;
        }

        private static ushort GetExpectedLayoutHandle<T>() {
            return Db.SQL<ushort>("SELECT r.LayoutHandle FROM Starcounter.Metadata.RawView r WHERE r.FullName=?", typeof(T).FullName).First;
        }

        private static ushort GetLayoutHandle(D d) {
            var proxy = d as IObjectProxy;
            if (proxy == null)
                return 0;

            return (ushort)(proxy.ThisHandle & 0xFFFF);
        }
    }

    // Things modified in this test: 
    // A.LongInA, C.ByteInC and D.BoolInD added.
    // C.LongInC removed.
    // StandAlone renamed to RenamedClass.

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
        public byte ByteInC;
    }

    [Database]
    public class D {
        public string StringInD;
        public bool BoolInD;
    }

    [Database]
    public class RenamedClass {
        public string Value;
    }
}