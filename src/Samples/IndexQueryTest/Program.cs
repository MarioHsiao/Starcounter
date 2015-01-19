﻿using Starcounter;
using Starcounter.Binding;
using System;
using System.Collections;
using System.Diagnostics;

namespace IndexQueryTest
{
    static partial class IndexQueryTests
    {

        static void Main(string[] args)
        {
            HelpMethods.LogEvent("Starting tests with inherited indexes");
            HelpMethods.LogEvent("Test of CREATE/DROP INDEX and DROP TABLE.");
            Starcounter.Internal.ErrorHandling.TestTraceListener.ReplaceDefault("QueryProcessingListener");
#if ACCOUNTTEST_MODEL
            HelpMethods.LogEvent("Test with loading model");
            TestCreateIndexWithoutQuery();
            TestDelete();
            Populate();
            CountAllObjects();
            //PrintAllObjects();
            // See a query plan
            Db.Transaction(delegate
            {
                IEnumerator sqlEnum = (IEnumerator)Db.SQL("select u from accounttest.user u").GetEnumerator();
                Trace.Assert(sqlEnum.ToString() != "");
            });
            TestCreateDropIndex();
            TestOrderBy();
            TestHint();
            TestJoinWIndex();
            TestAggregate();
            //int unloaded = Db.Unload(@"s\IndexQueryTest\IndexQueryDbDump.sql", 100);
            //Console.WriteLine("Unloaded {0} objects.", unloaded);
            //int reloaded = Db.Reload(@"s\IndexQueryTest\IndexQueryDbDump.sql");
            //Console.WriteLine("Reloaded {0} obejcts.", reloaded);
            //CreateDropIndexParallelTest();
#endif

#if ACCOUNTTEST_MODEL_NO
            HelpMethods.LogEvent("Test without loading model.");
            Db.SlowSQL("DROP TABLE AccountTest.Account");
            Db.SlowSQL("DROP TABLE AccountTest.User");
#endif

            using (Transaction t = new Transaction()) {
                t.Add(() => {
                    HelpMethods.LogEvent("Test of CREATE/DROP INDEX and DROP TABLE completed.");
                    HelpMethods.LogEvent("Test inherited indexes");
                    InheritedIndex.InheritedIndexTest.RunInheritedIndexTest();
                    HelpMethods.LogEvent("Finished testing inherited indexes");
                    HelpMethods.LogEvent("Test IS type predicate");
                    IsTypePredicateTest.RunIsTypePredicateTest();
                    HelpMethods.LogEvent("Finished testing IS type predicate");
                    HelpMethods.LogEvent("All tests are completed!");
                    Environment.Exit(0);
                });
            }
        }
    }
}
