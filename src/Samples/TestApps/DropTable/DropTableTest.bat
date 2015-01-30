@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_DROPTABLETEST%"=="False" GOTO :EOF

ECHO Running DropTableTest regression test.

REM Some predefined constants.
SET DB_NAME=DropTableTestDb

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
star --database=%DB_NAME% ..\EmptyScApp.cs
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run Step 1 to create initial schema and index
COPY /y DropTableTestV1.cs DropTableTest.cs
star --database=%DB_NAME% DropTableTest.cs
IF ERRORLEVEL 1 GOTO err

ECHO Run Step 2 to update initial schema without indexed column
COPY /y DropTableTestV2.cs DropTableTest.cs
star --database=%DB_NAME% DropTableTest.cs
IF ERRORLEVEL 1 GOTO err

REM Clean update
DEL DropTableTest.cs

ECHO DropTableTest regression test succeeded.
EXIT /b 0


:err
DEL DropTableTest.cs
ECHO Error: DropTableTest failed!
EXIT /b 1
