@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTCLASSSCHEMACHANGE%"=="False" GOTO :EOF

ECHO Running TestClassSchemaChange regression test.

REM Some predefined constants.
SET DBNAME=ClassSchemaChangeDb
SET APPNAME=ClassSchemaChange

staradmin start server
staradmin --database=%DBNAME% delete --force db

ECHO Run Step 1 to create initial schema
star --database=%DBNAME% --name=%APPNAME% TestClassSchemaChangeV1.cs
IF ERRORLEVEL 1 GOTO failure

staradmin --database=%DBNAME% stop db
ECHO Run Step 2 to update initial schema with more columns
star --database=%DBNAME% --name=%APPNAME% TestClassSchemaChangeV2.cs
IF ERRORLEVEL 1 GOTO failure

SET STATUS=ClassSchemaChange regression test succeeded.
GOTO cleanup

:failure
SET STATUS=Error: TestClassSchemaChange failed!

:cleanup
staradmin --database=%DBNAME% stop db
staradmin --database=%DBNAME% delete --force db

ECHO %STATUS%