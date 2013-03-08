﻿using System;
using Starcounter.TestFramework;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            HelpMethods.logger = new TestLogger("QueryProcessingTest", false);
            HelpMethods.LogEvent("Query processing tests are started");
            BindingTestDirect.DirectBindingTest();
            HelpMethods.LogEvent("Test query preparation performance, first round. Query parser was not yet accessed.");
            QueryProcessingPerformance.MeasurePrepareQuery();
            HelpMethods.LogEvent("Test query preparation performance, second round.");
            QueryProcessingPerformance.MeasurePrepareQuery();
            TestErrorMessages.RunTestErrorMessages();
            PopulateData();
            SqlBugsTest.QueryTests();
            FetchTest.RunFetchTest();
            AggregationTest.RunAggregationTest();
            CodePropertiesTesting.TestCodeProperties();
            SelectClauseExpressionsTests.TestSelectClauseExpressions();
            HelpMethods.LogEvent("All tests completed");
            Environment.Exit(0);
        }

        static void PopulateData() {
            HelpMethods.LogEvent("Data population");
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
            HelpMethods.LogEvent("Finished data population");
        }
    }
}
