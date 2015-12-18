using Starcounter;

namespace SchemaUpgradeTest {
    class Program {
        static int Main() {
            return 0;
        }
    }

    // Things modified in this test: 
    // B.DoubleInB changed type from double to string.

    [Database]
    public class A : C {
        public int IntInA;
        public long LongInA;
    }

    [Database]
    public class B : C {
        public string DoubleInB;
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