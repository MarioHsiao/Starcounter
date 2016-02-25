@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_UnloadTwoApps3399%"=="False" GOTO :EOF

ECHO Running UnloadTwoApps3399 regression test.

REM Some predefined constants.
SET DB_NAME=UnloadTwoApps3399Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Start the first App
star --database=%DB_NAME% UnloadTwoApps3399App1.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Start the second App
star --database=%DB_NAME% UnloadTwoApps3399App2.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run unload
staradmin --database=%DB_NAME% unload --file=unload.sql
IF %ERRORLEVEL% NEQ 0 GOTO err

REM Clean update
DEL unload.sql
staradmin --database=%DB_NAME% stop db

ECHO UnloadTwoApps3399 regression test succeeded.
EXIT /b 0


:err
DEL unload.sql
ECHO Error: UnloadTwoApps3399 failed!
EXIT /b 1
