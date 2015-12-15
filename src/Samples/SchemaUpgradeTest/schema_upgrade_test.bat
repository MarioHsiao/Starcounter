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
set CURRENTTEST=1-Setup
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% 1-Setup.cs
if %ERRORLEVEL% NEQ 0 GOTO FAILURE

:: Add a column to base, middle and leaf class.
:: Remove column from middle class.
:: Rename standalone class
set CURRENTTEST=2-AddAndRemove
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% 2-AddAndRemove.cs
if %ERRORLEVEL% NEQ 0 GOTO FAILURE

:: Readd column from middle class.  
:: Revert the namechange. 
:: Make sure data is intact.
set CURRENTTEST=3-RevertRemoveAndNameChange
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% 3-RevertRemoveAndNameChange.cs
if %ERRORLEVEL% NEQ 0 GOTO FAILURE

:: Change type of existing column. SHOULD FAIL.
set CURRENTTEST=4-ChangeColumnType
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% 4-ChangeColumnType.cs
if %ERRORLEVEL% EQU 0 GOTO FAILURE

:: Change inheritance of a class. SHOULD FAIL.
set CURRENTTEST=5-ChangeInheritance
staradmin --database=%DBNAME% stop db
echo Running test: %CURRENTTEST%
star --database=%DBNAME% --name=%APPNAME% 5-ChangeInheritance.cs
if %ERRORLEVEL% EQU 0 GOTO FAILURE

set STATUS=All schema upgrade tests succeeded.
GOTO CLEANUP

:FAILURE
set STATUS=Schema upgrade test '%CURRENTTEST%' failed.
GOTO CLEANUP

:CLEANUP
staradmin --database=%DBNAME% stop db
staradmin --database=%DBNAME% delete --force db
echo %STATUS%

