
This file includes information on how to use the SqlTest.

NOTE! THIS INFORMATION IS NOT UP TO DATE.

=== RUNNING SqlTest ===

When running the application SqlTest in Starcounter the collation should be English. Before running the test the database should be recreated,
since the tests compare objects by their ids.


=== ADDING MORE QUERIES TO SqlTest ===

To add a new test query to SqlTest do the following steps:

1. In the file SqlTestInput.txt add the new test query in the form:

<NEXT>
Description:[A description of the query, which describes what is tested.]
QueryString:[The query string (in one single line).]
VariableValues:[Values for the variables in the query string in the form [type1]:[value1]; [type2]:[value2];]
CompositeResultObjects:[The value True if the objects in the result set are of type CompositeObject, otherwise the value False.]
IncludesLiteral:[The value True if the query includes one or more literals, otherwise the value False.]
ShouldBeReordered:[The value False if the query has an "order by" clause, otherwise the value True.]
UseBisonParser:[The value True if the query should be optimized on the parsed tree produced by bison-based parser, otherwise the value False.]
ExpectedExceptionMessage: 
ExpectedExecutionPlan: 
[An empty line.]
ExpectedResult: 
[An empty line.]

The command <NEXT> creates a new test query and can not be omitted.
If an attribute is omitted it will be given a default value. The default values are as follows:

Description:[An empty string.]
QueryString:[An empty string.]
VariableValues:[An empty string.]
CompositeResultObjects:True
IncludesLiteral:False
ShouldBeReordered:True
UseBisonParser:False
ExpectedExceptionMessage:[An empty string.]
ExpectedExecutionPlan: 
[An empty line.]
ExpectedResult: 
[An empty line.]

2. Run SqlTest. The file SqlTestOutput.txt should include errors for the newly added query that 
the ActualExceptionMessage, the ActualExecutionPlan and the ActualResult did not correspond to 
the ExpectedExceptionMessage, the ExpectedExecutionPlan and the ExpectedResult (since you have 
not added this information yet).

If the ActualExceptionMessage, the ActualExecutionPlan and the ActualResult in SqlTestOutput.txt 
are correct with respect to the newly added query then copy the values from them to the places for 
the ExpectedExceptionMessage, the ExpectedExecutionPlan and the ExpectedResult in SqlTestInput.txt. 
Make sure that both the ExpectedExecutionPlan and the ExpectedResult ends with an empty line, and 
all other input parameters are written on separate single lines.


=== COMMENTS IN SqlTestInput.txt ===

You can write comments and/or comment out test queries that should not be executed in two ways.

1. Lines starting with "//" in SqlTestInput.txt will be disregarded.

2. Blocks of lines starting with a line starting with "/*" and 
ending with a line ending with "*/" will also be disregarded.

