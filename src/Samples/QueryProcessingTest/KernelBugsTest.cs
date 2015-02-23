using Starcounter;
using System;
using System.Diagnostics;
using System.Text;

namespace QueryProcessingTest {
    class KernelBugsTest {
        public static void RunKernelBugsTest(bool secondRun) {
#if false
            // We have to run this first, and only on first run, in order for
            // the conditions for reproducing the bug to be met (not that this
            // might change if additional tables are added or something like
            // that).

            if (!secondRun) TestIteratorRecreationBug1676();
#endif
        }

        private static void TestIteratorRecreationBug1676() {
            HelpMethods.LogEvent("Test iterator recreation bug (1676)");

            try {
                Db.SQL("CREATE INDEX Label_Used ON Label (Used)");
            }
            catch (DbException ex) {
                Trace.Assert(ex.ErrorCode == Starcounter.Error.SCERRNAMEDINDEXALREADYEXISTS);
            }

            Db.Transact(delegate {
                Db.SlowSQL("DELETE FROM Label");
            });

            using (Transaction tr1 = new Transaction()) {
                tr1.Scope(() => {
                    var expectedResult = new ulong[4];
                    expectedResult[3] = new Label() { Text = "A", Used = 1 }.GetObjectNo();
                    expectedResult[2] = new Label() { Text = "B", Used = 1 }.GetObjectNo();
                    expectedResult[1] = new Label() { Text = "C", Used = 1 }.GetObjectNo();
                    expectedResult[0] = new Label() { Text = "D", Used = 1 }.GetObjectNo();

                    tr1.Commit();

                    // Query on index where duplicate keys.

                    var labels = Db.SQL<Label>("SELECT l FROM Label l WHERE l.Used > ?", 0);
                    var labelsEnum = labels.GetEnumerator();
                    int count = 0;

                    while (labelsEnum.MoveNext()) {
                        Trace.Assert(count < 4);
                        Trace.Assert(labelsEnum.Current.GetObjectNo() == expectedResult[count++]);
                    }
                    labelsEnum.Dispose();

                    labelsEnum = labels.GetEnumerator();
                    count = 0;

                    Trace.Assert(labelsEnum.MoveNext());
                    Trace.Assert(labelsEnum.Current.GetObjectNo() == expectedResult[count++]);

                    Trace.Assert(labelsEnum.MoveNext());
                    Trace.Assert(labelsEnum.Current.GetObjectNo() == expectedResult[count++]);

                    // New transaction will force the scheduler to release the snapshot and recreate the
                    // iterator once we restore the current transaction and continue iterating the
                    // result set.

                    using (Transaction tr2 = new Transaction()) {
                        tr2.Scope(() => {
                            // Change size of first record in result set so that it is moved.
                            //
                            // Hopefully the record is moved and moved to a position after the the second
                            // record in the result set. Otherwise the bug will not be reproduced.

                            var label = (Label)DbHelper.FromID(expectedResult[0]);
                            label.Notes = "Sick BIG exploding hamsters.";

                            tr2.Commit();
                        });
                    }

                    // Now the record should have been moved and moved to a position after the second
                    // record is everything works correctly. If this is not so the correct conditions
                    // for reproducing the bug will not have been met.
                    //
                    // Since we can not access the record address from this interface we can not confirm
                    // that this is so and will just have to assume that this is the case.

                    var label2 = (Label)DbHelper.FromID(expectedResult[0]);

                    // This should not fail. The result set count will be 5 instead of 4 as expected
                    // since the record initially first in the stream will not be last in the stream.

                    while (labelsEnum.MoveNext()) {
                        Trace.Assert(count < 4);
                        Trace.Assert(labelsEnum.Current.GetObjectNo() == expectedResult[count++]);
                    }
                    labelsEnum.Dispose();
                });
            }

            Db.Transact(delegate {
                Db.SlowSQL("DELETE FROM Label");
            });
        }
    }
}
