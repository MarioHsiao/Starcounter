@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_BASICDYNAMICTYPESMODELLING%"=="False" GOTO :EOF

REM Some predefined constants.
SET DB_NAME=BasicDynamicTypesModelling
SET TEST_NAME=BasicDynamicTypesModelling

ECHO Running %TEST_NAME% regression test.

REM Some predefined constants.
SET DB_NAME=BasicDynamicTypesModelling

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
star --database=%DB_NAME% ..\EmptyScApp.cs
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

star --database=%DB_NAME% BasicDynamicTypesModelling.cs
IF ERRORLEVEL 1 GOTO err


ECHO %TEST_NAME% regression test succeeded.
EXIT /b 0


:err
ECHO Error: %TEST_NAME% failed!
EXIT /b 1
