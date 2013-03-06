﻿using System;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            BindingTestDirect.DirectBindingTest();
            RunQueryProcessingTest();
            SqlBugsTest.QueryTests();
            FetchTest.RunFetchTest();
            AggregationTest.RunAggregationTest();
            CodePropertiesTesting.TestCodeProperties();
            SelectClauseExpressionsTests.TestSelectClauseExpressions();
            QueryProcessingPerformance.MeasurePrepareQuery();
        }

        static void RunQueryProcessingTest() {
            TestErrorMessages.RunTestErrorMessages();
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
        }
    }
}
