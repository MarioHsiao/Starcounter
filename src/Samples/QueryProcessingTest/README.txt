This project contains sets of tests related to SQL query processing. The query model is account model with two tables: users and accounts. The database is populated with approximately 40000 objects.

To run the test three scripts are created:
cleanTestDb.cmd - removes database files and kills all processes
prepareTestDb.cmd - creates database if it doesn't exist and starts necessary prcesses
runQueryTest.cmd - starts sccode.exe for the database

The application should be called manually in the started sccode.exe, by given the following command:
exec s\QueryProcessingTest\.starcounter\QueryProcessingTest.exe
