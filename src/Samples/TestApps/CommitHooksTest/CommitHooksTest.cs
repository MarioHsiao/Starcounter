
namespace TestCommitHooks {
    using System;
    using Starcounter;

    [Database] public class Foo {
        public Foo() {
            Bar = DateTime.Now;
        }

        public DateTime Bar;
    }

    class Program {
        static void Main() {
            new Basics().Test();
        }
    }

    class Basics {
        Application app;
        int insertHookCount = 0;
        int insertHookExpectedCount = 2;
        int updateHookCount = 0;
        int updateHookExpectedCount = 1;
        int deleteHookCount = 0;
        int deleteHookExpectedCount = 3;

        public Basics() {
            app = Application.Current;
        }

        void AssureAppContext() {
            var a = Application.Current;
            if (a.Name != app.Name) throw new Exception("Unexpected application context in BASIC commit hook test.");
        }

        void AssureKey(ulong key, ulong expected) {
            if (key != expected) {
                throw new Exception("Unexpected object key in BASIC commit hook test.");
            }
        }

        void AssureCount(int count, int expected, string context = "") {
            if (count != expected) {
                throw new Exception(string.Format("Unexpected count in BASIC commit hook test: {0}", context));
            }
        }

        public void Test() {
            ulong key = 0;

            // Insert hooks

            Hook<Foo>.OnInsert(f => {
                AssureAppContext();
                AssureKey(f.GetObjectNo(), key);
                insertHookCount++;
            });
            Hook<Foo>.OnInsert(f => {
                AssureAppContext();
                AssureKey(f.GetObjectNo(), key);
                insertHookCount++;
            });

            // Update hooks

            Hook<Foo>.OnUpdate(f => {
                AssureAppContext();
                AssureKey(f.GetObjectNo(), key);
                updateHookCount++;
            });

            // Delete hooks

            Hook<Foo>.OnDelete(oid => {
                AssureAppContext();
                AssureKey(oid, key);
                deleteHookCount++;
            });
            Hook<Foo>.OnDelete(oid => {
                AssureAppContext();
                AssureKey(oid, key);
                deleteHookCount++;
            });
            Hook<Foo>.OnDelete(oid => {
                AssureAppContext();
                AssureKey(oid, key);
                deleteHookCount++;
            });

            // Transactions to trigger them

            Db.Transact(() => { 
                var f = new Foo();
                key = f.GetObjectNo();
            });

            Db.Transact(() => {
                var f = DbHelper.FromID(key) as Foo;
                f.Bar = DateTime.Now;
            });

            Db.Transact(() => {
                var f = DbHelper.FromID(key) as Foo;
                f.Delete();
            });

            // Assure results

            AssureCount(insertHookCount, insertHookExpectedCount, "Insert count");
            AssureCount(updateHookCount, updateHookExpectedCount, "Update count");
            AssureCount(deleteHookCount, deleteHookExpectedCount, "Delete count");
        }
    }
}

