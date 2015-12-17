@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_DROPTABLE%"=="False" GOTO :EOF

ECHO Running DropTable regression test.

REM Some predefined constants.
SET DB_NAME=DropTableDb

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run Step 1 to create initial schema and index
COPY /y DropTableV1.cs DropTable.cs
star --database=%DB_NAME% DropTable.cs
IF ERRORLEVEL 1 GOTO err

ECHO Run Step 2 to update initial schema without indexed column
COPY /y DropTableV2.cs DropTable.cs
star --database=%DB_NAME% DropTable.cs
IF ERRORLEVEL 1 GOTO err

REM Clean update
DEL DropTable.cs
staradmin --database=%DB_NAME% stop db

ECHO DropTable regression test succeeded.
EXIT /b 0


:err
DEL DropTable.cs
ECHO Error: DropTable failed!
EXIT /b 1
