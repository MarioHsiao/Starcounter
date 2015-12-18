using Starcounter;

namespace SchemaUpgradeTest {
    class Program {
        static int Main() {
            return 0;
        }
    }

    // Things modified in this test: 
    // B changed inheritance from C to D

    [Database]
    public class A : C {
        public int IntInA;
        public long LongInA;
    }

    [Database]
    public class B : D {
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