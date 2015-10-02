@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_OffsetkeyBug2915%"=="False" GOTO :EOF

ECHO Running OffsetkeyBug2915 regression test.

REM Some predefined constants.
SET DB_NAME=OffsetkeyBug2915Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
star --database=%DB_NAME% ..\EmptyScApp.cs
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% OffsetkeyBug2915.cs
IF ERRORLEVEL 1 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO OffsetkeyBug2915 regression test succeeded.
EXIT /b 0


:err
ECHO Error: OffsetkeyBug2915 failed!
EXIT /b 1
