@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTCLASSSCHEMACHANGE%"=="False" GOTO :EOF

ECHO Running TestClassSchemaChange regression test.

REM Some predefined constants.
SET DB_NAME=TestClassSchemaChangeDb

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run Step 1 to create initial schema
COPY /y TestClassSchemaChangeV1.cs TestClassSchemaChange.cs
star --database=%DB_NAME% TestClassSchemaChange.cs
IF ERRORLEVEL 1 GOTO err

ECHO Run Step 2 to update initial schema with more columns
COPY /y TestClassSchemaChangeV2.cs TestClassSchemaChange.cs
star --database=%DB_NAME% TestClassSchemaChange.cs
IF ERRORLEVEL 1 GOTO err

REM Clean update
DEL TestClassSchemaChange.cs
staradmin --database=%DB_NAME% stop db

ECHO TestClassSchemaChange regression test succeeded.
EXIT /b 0


:err
DEL TestClassSchemaChange.cs
ECHO Error: TestClassSchemaChange failed!
EXIT /b 1
