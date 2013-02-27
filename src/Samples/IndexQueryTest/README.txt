This project contains sets of tests related to SQL query processing with indexes. It contains two data models: one account data model with two table and another is hierarchy of inherited tables. The database is populated with tiny amount of data.

To run the test three scripts are created:
cleanAccountTestDb.cmd - removes database files and kills all processes
prepareAccountTestDb.cmd - creates database if it doesn't exist and starts necessary prcesses
runIndexQueryTest.cmd - starts sccode.exe for the database

The application should be called manually in the started sccode.exe, by given the following command:
exec s\IndexQueryTest\.starcounter\IndexQueryTest.exe
