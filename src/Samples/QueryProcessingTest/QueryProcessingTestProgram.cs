using System;
using System.Diagnostics;
using Starcounter.TestFramework;
using System.IO;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            try {
                HelpMethods.LogEvent("Query processing tests are started");
                Starcounter.Internal.ErrorHandling.TestTraceListener.ReplaceDefault("QueryProcessingListener");
                var secondRun = File.Exists(@"s\QueryProcessingTest\dumpQueryProcessingDB.sql");
                if (secondRun) {
                    HelpMethods.LogEvent("Start loading query processing database.");
                    int nrLoaded = Starcounter.Db.Reload(@"s\QueryProcessingTest\dumpQueryProcessingDB.sql");
                    HelpMethods.LogEvent("Finish loading query processing database. Loaded " +
                        nrLoaded + " objects.");

                    UnloadReloadTest.Check();
                }
                else
                    HelpMethods.LogEvent("No reload.");
                KernelBugsTest.RunKernelBugsTest(secondRun);
                BindingTestDirect.DirectBindingTest();
                HelpMethods.LogEvent("Test query preparation performance.");
                QueryProcessingPerformance.MeasurePrepareQuery();
                HelpMethods.LogEvent("Finished test query preparation performance.");
                TestErrorMessages.RunTestErrorMessages();
                NamespacesTest.TestClassesNamespaces();
                WebVisitTests.TestVisits();
                InsertIntoTests.TestValuesInsertIntoWebVisits();
                PopulateData();
                SqlBugsTest.QueryTests();
                FetchTest.RunFetchTest();
                AggregationTest.RunAggregationTest();
                CodePropertiesTesting.TestCodeProperties();
                SelectClauseExpressionsTests.TestSelectClauseExpressions();
                OffsetkeyTest.Master();
                ObjectIdentityTest.TestObjectIdentityInSQL();
                MetadataTest.TestPopulatedMetadata();
                TestKinds.RunKindsTest();
                if (Environment.GetEnvironmentVariable("SC_NIGHTLY_BUILD") == "True")
                    BenchmarkQueryCache.BenchQueryCache();
                else
                    HelpMethods.LogEvent("Benchmark of query cache is skipped");
                HelpMethods.LogEvent("Start unloading query processing database.");
                UnloadReloadTest.Populate();
                int nrUnloaded = Starcounter.Db.Unload(@"s\QueryProcessingTest\dumpQueryProcessingDB.sql");
                HelpMethods.LogEvent("Finish unloading query processing database. Unloaded " + 
                    nrUnloaded + " objects.");
                HelpMethods.LogEvent("Start delete the database data.");
                Starcounter.Reload.DeleteAll();
                HelpMethods.LogEvent("Finish delete the database data.");
                HelpMethods.LogEvent("All tests completed");
            } catch (Exception e) {
                HelpMethods.LogEvent(e.ToString());
                throw;
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
            if (Starcounter.Db.SQL("select i from \"Index\" i where name = ?", "accountidindx").First == null)
                Starcounter.Db.SQL("create index AccountTypeActiveIndx on Account (notactive, AccountType)");
            if (Starcounter.Db.SQL("select i from \"Index\" i where name = ?", "AccountTypeIndx").First == null) {
                Starcounter.Db.SQL("create index AccountTypeIndx on Account (AccountType)");
                Starcounter.Db.SQL("create index accountidindx on Account(accountid)");
            }
            if (Starcounter.Db.SQL("select i from \"Index\" i where name = ?", "nicknameindx").First == null) {
                Starcounter.Db.SlowSQL("create index nicknameindx on User(NickName)");
                Starcounter.Db.SlowSQL("create index anothernicknameindx on User(AnotherNickName)");
            }
            if (Starcounter.Db.SQL("select i from \"Index\" i where name = ?", "UserCompoundIndx").First == null)
                Starcounter.Db.SlowSQL("create index UserCompoundIndx on user(NickName, LastName)");
            if (Starcounter.Db.SQL("SELECT i FROM \"Index\" i WHERE Name=?", "VersionSourceBuildErrorChannelIndex").First == null) {
                Starcounter.Db.SQL("CREATE INDEX VersionSourceBuildErrorChannelIndex ON VersionSource (BuildError,Channel)");
            }

            if (Starcounter.Db.SQL("SELECT i FROM \"Index\" i WHERE Name=?", "VersionSourceBuildErrorIndex").First == null) {
                Starcounter.Db.SQL("CREATE INDEX VersionSourceBuildErrorIndex ON VersionSource (BuildError)");
            }

            if (Starcounter.Db.SQL("SELECT i FROM \"Index\" i WHERE Name=?", "VersionSourceVersionIndex").First == null) {
                Starcounter.Db.SQL("CREATE INDEX VersionSourceVersionIndex ON VersionSource (Version)");
            }
        }
    }
}
