@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_SELECTONNULL2362%"=="False" GOTO :EOF

ECHO Running SelectOnNull2362 regression test.

REM Some predefined constants.
SET DB_NAME=SelectOnNull2362Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

star --database=%DB_NAME% SelectOnNull2362.cs
IF %ERRORLEVEL% NEQ 0 GOTO err


ECHO SelectOnNull2362 regression test succeeded.
EXIT /b 0


:err
ECHO Error: SelectOnNull2362 failed!
EXIT /b 1
