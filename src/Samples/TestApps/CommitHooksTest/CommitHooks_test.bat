@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_COMMITHOOKSTEST%"=="False" GOTO :EOF

REM Some predefined constants.
SET DB_NAME=CommitHooksTest
SET TEST_NAME=CommitHooksTest

ECHO Running %TEST_NAME% regression test.

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

star --database=%DB_NAME% CommitHooksTest.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO %TEST_NAME% regression test succeeded.
EXIT /b 0


:err
ECHO Error: %TEST_NAME% failed!
EXIT /b 1
