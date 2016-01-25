@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_3LEVELSCHEMACHANGE%"=="False" GOTO :EOF

ECHO Running 3LevelSchemaChange regression test.

REM Some predefined constants.
SET DBNAME=3LevelSchemaChangeDb
SET APPNAME=3LevelSchemaChange

staradmin start server
staradmin --database=%DBNAME% delete --force db

ECHO Run Step 1 to create initial schema
star --database=%DBNAME% --name=%APPNAME% 3LevelSchemaChange.Step1.cs
IF ERRORLEVEL 1 GOTO FAILURE

staradmin --database=%DBNAME% stop db
ECHO Run Step 2 to update the initial schema with more columns
star --database=%DBNAME% --name=%APPNAME% 3LevelSchemaChange.Step2.cs
IF ERRORLEVEL 1 GOTO FAILURE

staradmin --database=%DBNAME% stop db
ECHO Run Step 1 again to update the schema with less columns
star --database=%DBNAME% --name=%APPNAME% 3LevelSchemaChange.Step1.cs
IF ERRORLEVEL 1 GOTO FAILURE

staradmin --database=%DBNAME% stop db
staradmin --database=%DBNAME% delete --force db
ECHO 3LevelSchemaChange regression test succeeded.
EXIT /b 0

:FAILURE
staradmin --database=%DBNAME% stop db
echo Error: 3LevelSchemaChange failed!
EXIT /b 1
