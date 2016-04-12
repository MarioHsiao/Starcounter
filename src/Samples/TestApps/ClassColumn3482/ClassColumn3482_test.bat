@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_ClassColumn3482%"=="False" GOTO :EOF

ECHO Running ClassColumn3482 regression test.

REM Some predefined constants.
SET DB_NAME=ClassColumn3482Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% ClassColumn3482.cs
IF ERRORLEVEL 1 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO ClassColumn3482 regression test succeeded.
EXIT /b 0


:err
ECHO Error: ClassColumn3482 failed!
EXIT /b 1
