@echo off

:: Tests for doing multiple upgrades of the schema and verifying that data is not removed.
:: Each subtest needs to stop both codehost and datamanager to properly unload and run cleanup 
:: of old layouts.

set DBNAME=schemaupgradetest
set APPNAME=schemaupgradetest
set CURRENTTEST=""

:: Start server and clean up old database (if any)
staradmin start server
staradmin --database=%DBNAME% delete --force db

:: Run first "app" to create the initial state of the schema
:: and add some default records, used later to check layouts used.

set CURRENTTEST=Setup
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% Setup.cs
if ERRORLEVEL 1 GOTO FAILURE

:: First test.
:: Add a column to base, middle and leaf class.
:: Remove column from middle class.
:: Rename standalone class
set CURRENTTEST=AddAndRemove
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% AddAndRemove.cs
if ERRORLEVEL 1 GOTO FAILURE

:: Second test. 
:: Readd column from middle class.  
:: Revert the namechange. 
:: Make sure data is intact.
set CURRENTTEST=RevertRemoveAndNameChange
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% RevertRemoveAndNameChange.cs
if ERRORLEVEL 1 GOTO FAILURE

:: Third test.
:: Change type of existing column. SHOULD FAIL.
set CURRENTTEST=IncorrectTypeColumn
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% IncorrectType.cs
if ERRORLEVEL 0 GOTO FAILURE

:: Fourth test. 
:: Change inheritance of a class. SHOULD FAIL.
set CURRENTTEST=IncorrectInheritance
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% IncorrectInheritance.cs
if ERRORLEVEL 0 GOTO FAILURE

set STATUS=All schema upgrade tests succeeded.
GOTO CLEANUP

:FAILURE
set STATUS=Schema upgrade test '%CURRENTTEST%' failed.
GOTO CLEANUP

:CLEANUP
staradmin --database=%DBNAME% stop db
staradmin --database=%DBNAME% delete --force db
echo %STATUS%

