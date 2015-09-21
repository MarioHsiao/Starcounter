@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_APPHOSTTEST%"=="False" GOTO :EOF

REM Some predefined constants.
SET DB_NAME=AppHostTest
SET TEST_NAME=IApplicationHostTest

ECHO Running %TEST_NAME% regression test.

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
star --database=%DB_NAME% ..\EmptyScApp.cs
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

star --database=%DB_NAME% AppHostTest.cs
IF ERRORLEVEL 1 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO %TEST_NAME% regression test succeeded.
EXIT /b 0


:err
ECHO Error: %TEST_NAME% failed!
EXIT /b 1