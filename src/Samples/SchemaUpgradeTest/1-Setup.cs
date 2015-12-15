using Starcounter;

namespace SchemaUpgradeTest {
    class Program {
        static int Main() {
            bool b = Db.SQL("SELECT a FROM A a").First == null;
            ScAssertion.Assert(b, "TODO: Should be empty!");

            Db.Transact(() => {
                new A() {
                    IntInA = 1,
                    LongInC = 10001,
                    StringInD = "Ett"
                };

                new A() {
                    IntInA = 2,
                    LongInC = 10002,
                    StringInD = "Tva"
                };

                new B() {
                    DoubleInB = 39.45d,
                    LongInC = 20001,
                    StringInD = "Tre"
                };

                new StandAlone() {
                    Value = "Value"
                };
            });

            return 0;
        }
    }

    [Database]
    public class A : C {
        public int IntInA;
    }

    [Database]
    public class B : C {
        public double DoubleInB;
    }

    [Database]
    public class C : D {
        public long LongInC;
    }

    [Database]
    public class D {
        public string StringInD;
    }

    [Database]
    public class StandAlone {
        public string Value;
    }
}