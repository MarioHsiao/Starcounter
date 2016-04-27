@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_CountQueryInheritedOpt3447%"=="False" GOTO :EOF

ECHO Running CountQueryInheritedOpt3447 regression test.

REM Some predefined constants.
SET DB_NAME=CountQueryInheritedOpt3447Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% CountQueryInheritedOpt3447.cs
IF ERRORLEVEL 1 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO CountQueryInheritedOpt3447 regression test succeeded.
EXIT /b 0


:err
ECHO Error: CountQueryInheritedOpt3447 failed!
EXIT /b 1
