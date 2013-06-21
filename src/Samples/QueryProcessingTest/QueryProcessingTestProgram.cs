using System;
using System.Diagnostics;
using Starcounter.TestFramework;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            try {
                HelpMethods.LogEvent("Query processing tests are started");
                Starcounter.Internal.ErrorHandling.TestTraceListener.ReplaceDefault("QueryProcessingListener");
                BindingTestDirect.DirectBindingTest();
                HelpMethods.LogEvent("Test query preparation performance.");
                QueryProcessingPerformance.MeasurePrepareQuery();
                TestErrorMessages.RunTestErrorMessages();
                PopulateData();
                SqlBugsTest.QueryTests();
                FetchTest.RunFetchTest();
                AggregationTest.RunAggregationTest();
                CodePropertiesTesting.TestCodeProperties();
                SelectClauseExpressionsTests.TestSelectClauseExpressions();
                OffsetkeyTest.Master();
                ObjectIdentityTest.TestObjectIdentityInSQL();
                HelpMethods.LogEvent("All tests completed");
            } catch (Exception e) {
                HelpMethods.LogEvent(e.ToString());
                throw e;
            }
            Environment.Exit(0);
        }

        static void PopulateData() {
            HelpMethods.LogEvent("Data population");
            CreateIndexes();
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
            HelpMethods.LogEvent("Finished data population");
        }

        internal static void CreateIndexes() {
            if (Starcounter.Db.SQL("select i from sysindex i where name = ?", "accountidindx").First == null)
                Starcounter.Db.SlowSQL("create index accountidindx on Account(accountid)");
            if (Starcounter.Db.SQL("select i from sysindex i where name = ?", "nicknameindx").First == null) {
                Starcounter.Db.SlowSQL("create index nicknameindx on User(NickName)");
                Starcounter.Db.SlowSQL("create index anothernicknameindx on User(AnotherNickName)");
            }
        }
    }
}
