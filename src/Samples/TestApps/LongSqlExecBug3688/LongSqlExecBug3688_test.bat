@ECHO OFF

ECHO Running LongSqlExecBug3688 regression test.

REM Some predefined constants.
SET DB_NAME=LongSqlExecBug3688Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% LongSqlExecBug3688.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO LongSqlExecBug3688 regression test succeeded.
EXIT /b 0


:err
ECHO Error: LongSqlExecBug3688 failed!
EXIT /b 1
