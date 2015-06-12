@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_CODEPROPINDEX2533%"=="False" GOTO :EOF

ECHO Running CodePropIndex2533 regression test.

REM Some predefined constants.
SET DB_NAME=CodePropIndex2533Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
star --database=%DB_NAME% ..\EmptyScApp.cs
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% CodePropIndex2533.cs
IF ERRORLEVEL 1 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO CodePropIndex2533 regression test succeeded.
EXIT /b 0


:err
ECHO Error: CodePropIndex2533 failed!
EXIT /b 1
