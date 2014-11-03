@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTCLASSSCHEMACHANGE%"=="False" GOTO :EOF

ECHO Running TestClassSchemaChange regression test.

REM Some predefined constants.
SET DB_NAME=TestClassSchemaChangeDb
REM SET DB_NAME=TestClassSchemaChange

REM Delete database after server is started
REM staradmin --database=%DB_NAME% delete

ECHO Run Step 1 to create initial schema
COPY /y TestClassSchemaChangeV1.cs TestClassSchemaChange.cs
star --database=%DB_NAME% TestClassSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 2 to update initial schema with more columns
COPY /y TestClassSchemaChangeV2.cs TestClassSchemaChange.cs
star --database=%DB_NAME% TestClassSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

REM Clean update
DEL TestClassSchemaChange.cs

ECHO TestClassSchemaChange regression test succeeded.
EXIT /b 0


:err
DEL TestClassSchemaChange.cs
ECHO Error: TestClassSchemaChange failed!
EXIT /b 1
