@ECHO OFF

ECHO Running OffsetkeyEmptyBug3502 regression test.

REM Some predefined constants.
SET DB_NAME=OffsetkeyEmptyBug3502Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run the test
star --database=%DB_NAME% OffsetkeyEmptyBug3502.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

staradmin --database=%DB_NAME% stop db

ECHO OffsetkeyEmptyBug3502 regression test succeeded.
EXIT /b 0


:err
ECHO Error: OffsetkeyEmptyBug3502 failed!
EXIT /b 1
