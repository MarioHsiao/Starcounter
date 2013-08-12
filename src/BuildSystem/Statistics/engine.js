var TestValue = (function () {
    function TestValue(buildNumber, testValue, buildServerName, testDate, personalBuild) {
        this.buildNumber = buildNumber;
        this.testValue = testValue;
        this.buildServerName = buildServerName;
        this.testDate = testDate;
        this.personalBuild = personalBuild;
    }
    return TestValue;
})();
;

var Test = (function () {
    function Test(name) {
        this.name = name;
        this.values = new Array();
    }
    return Test;
})();
;

// Filter to remove empty strings from the array.
var FilterEmptyEntries = function (s) {
    return s.length != 0;
};

var Tests = (function () {
    function Tests() {
    }
    Tests.prototype.ParseTests = function (drawPersonal, maxNumFreshEntries) {
        // Splitting the test data, removing empty entries and reversing the final array.
        var testsStrings = GlobalFileContents.split("\n").filter(FilterEmptyEntries).reverse();

        var allTests = new Array();

        for (var i = 0; i < testsStrings.length; i++) {
            var testString = testsStrings[i];
            var testDetails = testString.split(" ").filter(FilterEmptyEntries);

            if (testDetails.length != 6)
                continue;

            // Getting the name of the test.
            var testName = testDetails[0].toLowerCase();

            if (!(testName in allTests))
                allTests[testName] = new Test(testName);

            if (allTests[testName].values.length >= maxNumFreshEntries)
                continue;

            // Creating new test.
            var testValue = new TestValue(testDetails[1], parseFloat(testDetails[2]), testDetails[5], testDetails[4], testDetails[3].toLowerCase() == "personal");

            if (drawPersonal || !testValue.personalBuild)
                allTests[testName].values.push(testValue);
        }

        // Collecting the tests by name.
        var testNames = new Array();
        for (var testName in allTests)
            testNames.push(testName);

        // Sorting the tests by name.
        testNames.sort();

        // Creating final sorted tests array.
        this.allTests = new Array();
        for (var i = 0; i < testNames.length; i++) {
            // Reversing the values.
            allTests[testNames[i]].values.reverse();

            // Adding to global tests.
            this.allTests.push(allTests[testNames[i]]);
        }
    };
    return Tests;
})();
;
//@ sourceMappingURL=engine.js.map
