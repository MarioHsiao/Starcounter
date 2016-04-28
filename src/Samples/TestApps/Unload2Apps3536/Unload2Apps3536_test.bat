@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_Unload2Apps3536%"=="False" GOTO :EOF

ECHO Running Unload2Apps3536 regression test.

REM Some predefined constants.
SET DB_NAME=Unload2Apps3536Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% App1toUnload.cs
IF ERRORLEVEL 1 GOTO err
star --database=%DB_NAME% App2toUnload.cs
IF ERRORLEVEL 1 GOTO err
staradmin --database=%DB_NAME% unload --file=unload3536.sql
IF ERRORLEVEL 1 GOTO err

staradmin --database=%DB_NAME% stop db

if EXIST unload3536.sql DEL unload3536.sql
ECHO Unload2Apps3536 regression test succeeded.
EXIT /b 0


:err
if EXIST unload3536.sql DEL unload3536.sql
ECHO Error: Unload2Apps3536 failed!
EXIT /b 1
