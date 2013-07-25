declare var GlobalFileContents: string;

class TestValue {
    buildNumber: string;
    testValue: number;
    buildServerName: string;
    testDate: string;
    personalBuild: boolean;

    constructor(
        buildNumber: string,
        testValue: number,
        buildServerName: string,
        testDate: string,
        personalBuild: boolean) {
            this.buildNumber = buildNumber;
            this.testValue = testValue;
            this.buildServerName = buildServerName;
            this.testDate = testDate;
            this.personalBuild = personalBuild;
    }
};

class Test {
    name: string;
    values: TestValue[];

    constructor(name: string) {
        this.name = name;
        this.values = new Array<TestValue>();
    }
};

// Filter to remove empty strings from the array.
var FilterEmptyEntries = function (s: string) {
    return s.length != 0;
};

class Tests {
    allTests: Test[];

    public ParseTests(drawPersonal: boolean, maxNumFreshEntries: number) {

        // Splitting the test data, removing empty entries and reversing the final array.
        var testsStrings: string[] = GlobalFileContents.split("\n").filter(FilterEmptyEntries).reverse();

        var allTests = new Array<Test>();

        for (var i = 0; i < testsStrings.length; i++) {
            var testString = testsStrings[i];
            var testDetails: string[] = testString.split(" ").filter(FilterEmptyEntries);

            // Checking that we have correct number of parts in the string.
            if (testDetails.length != 6)
                continue;

            // Getting the name of the test.
            var testName = testDetails[0].toLowerCase();

            // Checking if we already have an entry.
            if (!(testName in allTests))
                allTests[testName] = new Test(testName);

            // Checking if we have maximum number of entries.
            if (allTests[testName].values.length >= maxNumFreshEntries)
                continue;

            // Creating new test.
            var testValue = new TestValue(
                testDetails[1],
                parseFloat(testDetails[2]),
                testDetails[5],
                testDetails[4],
                testDetails[3].toLowerCase() == "personal");

            // Checking if personal builds should be included.
            if (drawPersonal || !testValue.personalBuild)
                allTests[testName].values.push(testValue);
        }

        // Collecting the tests by name.
        var testNames = new Array<string>();
        for (var testName in allTests)
            testNames.push(testName);

        // Sorting the tests by name.
        testNames.sort();

        // Creating final sorted tests array.
        this.allTests = new Array<Test>();
        for (var i = 0; i < testNames.length; i++) {
            // Reversing the values.
            allTests[testNames[i]].values.reverse();

            // Adding to global tests.
            this.allTests.push(allTests[testNames[i]]);
        }
    }
};

/*
window.onload = () => {
    var tests = new Tests();
    tests.ParseTests(false, 10);
};
*/
