@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_INDEXEDCOLUMNDROP%"=="False" GOTO :EOF

ECHO Running IndexedColumnDrop regression test.

REM Some predefined constants.
SET DB_NAME=IndexedColumnDropDb

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
star --database=%DB_NAME% ..\EmptyScApp.cs
staradmin --database=%DB_NAME% stop db
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run Step 1 to create initial schema and index
COPY /y IndexedColumnDropV1.cs IndexedColumnDrop.cs
star --database=%DB_NAME% IndexedColumnDrop.cs
IF ERRORLEVEL 1 GOTO err

ECHO Run Step 2 to update initial schema without indexed column
COPY /y IndexedColumnDropV2.cs IndexedColumnDrop.cs
star --database=%DB_NAME% IndexedColumnDrop.cs
IF ERRORLEVEL 1 GOTO err

REM Clean update
DEL IndexedColumnDrop.cs

ECHO IndexedColumnDrop regression test succeeded.
EXIT /b 0


:err
DEL IndexedColumnDrop.cs
ECHO Error: IndexedColumnDrop failed!
EXIT /b 1
